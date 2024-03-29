﻿using System;
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ANDOR_CS;
using DIPOL_UF.Annotations;
using DIPOL_UF.Converters;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog.Events;

namespace DIPOL_UF
{
    public static class Helper
    {
        private static readonly double DEpsilon = Math.Pow(2, -53);
        public static Dispatcher UiDispatcher =>
            Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("Dispatcher is unavailable.");

        public static bool IsDialogWindow(Window window)
        {
            var showingAsDialogField = typeof(Window).GetField(
                @"_showingAsDialog",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return (bool) (showingAsDialogField?.GetValue(window) ?? false);
        }
        public static void WriteLog(string entry) => Injector.GetLogger()?.Write(LogEventLevel.Information, entry);

        public static void WriteLog(ANDOR_CS.Exceptions.AndorSdkException entry) => WriteLog($"{entry.Message} [{entry.ErrorCode}]");

        public static void WriteLog(object entry) => WriteLog(entry.ToString());

        public static void WriteLog(LogEventLevel level, string template, params object[] args) =>
            Injector.GetLogger()?.Write(level, template, args);
        public static void WriteLog(LogEventLevel level, Exception exception, string template, params object[] args) => 
            Injector.GetLogger()?.Write(level, exception, template, args);

        public static string GetCameraHostName(string input)
        {
            var splits = input.Split(';');
            if (splits.Length > 0)
            {
                var host = splits[0];
                if (!string.IsNullOrWhiteSpace(host))
                {
                    if (!host.Contains("://"))
                    {
                        host = $@"net.tcp://{host}";
                    }

                    if (
                        Uri.TryCreate(host, UriKind.RelativeOrAbsolute, out var uri)
                        && uri.Host.ToLowerInvariant() is var invariantHost
                    )
                    {
                        return invariantHost is "localhost" or "127.0.0.1"
                            ? Properties.Localization.General_LocalHostName
                            : uri.Host;
                    }

                }
            }
            return string.Empty;
        }

        public static void ExecuteOnUi(Action action)
        {
            if (UiDispatcher.CheckAccess())
                action();
            else
                UiDispatcher.Invoke(action);
        }

        public static T ExecuteOnUi<T>(Func<T> action)
        {
            if (UiDispatcher.CheckAccess())
                return action();
            return Application.Current.Dispatcher.Invoke(action);
        }

        public static string EnumerableToString<T>(this IEnumerable<T> input, string separator = ", ")
            =>  input.ToList() is var list && list.Count != 0
                ? list.Select(x => x.ToString()).Aggregate((old, @new) => old + separator + @new)
                : "";

        public static string EnumerableToString(this IEnumerable input, string separator = ", ")
        {
            var enumer = input.GetEnumerator();

            var s = new StringBuilder();

            if (enumer.MoveNext())
                s.Append(enumer.Current.ToStringEx());

            while (enumer.MoveNext())
                s.Append(separator + enumer.Current.ToStringEx());

            return s.ToString();
        }

        public static string GetValueTupleString(this ITuple tuple)
        {
            var list = new List<string>(tuple.Length);
            for (var i = 0; i < tuple.Length; i++)
            {
                list.Add(tuple[i].ToStringEx());
            }

            return list.EnumerableToString();
        }

        public static string ToStringEx([CanBeNull] this object @this)
        {
            return @this switch
            {
                null => throw new ArgumentNullException(nameof(@this)),
                string s => s,
                Enum @enum => @enum.GetEnumNameRep().Full,
                ITuple tuple => $"({tuple.GetValueTupleString()})",
                IEnumerable enumerable => $"[{enumerable.EnumerableToString()}]",
                _ => @this.ToString()
            };
        }

        public static double Clamp(this double val, double min, double max)
        {
            var result = val >= min ? val : min;
            result = result <= max ? result : max;
            return result;
        }

        public static IObservable<T> ObserveOnUi<T>(this IObservable<T> input)
            => input.ObserveOn(UiDispatcher);

        public static IObservable<T> ModifyIf<T>(
            this IObservable<T> input,
            bool condition,
            Func<IObservable<T>, IObservable<T>> modifier) =>
            condition ? modifier(input) : input;

        public static IObservable<IChangeSet<TEntry, TKey>> DisposeManyEx<TEntry, TKey>(
            this IObservable<IChangeSet<TEntry, TKey>> @this,
            Action<TEntry> disposeAction)
            => @this.SubscribeMany(x => Disposable.Create(x, disposeAction));


        public static T WithDataContext<T>(this T control, ReactiveObjectEx dataContext)
            where T : FrameworkElement
        {
            control.DataContext = dataContext;
            return control;
        }

        public static ConfiguredTaskAwaitable RunNoMarshall(Action action)
            => RunNoMarshall(action, CancellationToken.None);

        public static ConfiguredTaskAwaitable<T> RunNoMarshall<T>(Func<T> action)
            => RunNoMarshall(action, CancellationToken.None);

        public static ConfiguredTaskAwaitable RunNoMarshall(Action action, CancellationToken token)
            => Task.Run(action, token).ConfigureAwait(false);

        public static ConfiguredTaskAwaitable<T> RunNoMarshall<T>(Func<T> action, CancellationToken token)
            => Task.Run(action, token).ConfigureAwait(false);

        public static ConfiguredTaskAwaitable ExpectCancellation(
            this Task input,
            bool marshal = false
        )
            => input
#if DEBUG
               .ContinueWith(
                   (task, _) =>
                   {
                       if (!(task.Exception is null))
                           WriteLog(task.Exception.Message);
                   }, null
               )
#endif
               .ConfigureAwait(marshal);

        public static ConfiguredTaskAwaitable<T> ExpectCancellation<T>(
            this Task<T> input, bool marshal = false)
            => input.ContinueWith((task, _) =>
                    {
#if DEBUG
                        if (task.Exception is not null)
                        {
                            WriteLog(task.Exception.Message);
                        }
#endif
                        return task.Status == TaskStatus.RanToCompletion ? task.Result : default;
                    }, null)
                    .ConfigureAwait(marshal);

        public static async Task<T> ExpectCancellationAsync<T>(this Task<T> input)
        {
            try
            {
                return await input;
            }
            catch (OperationCanceledException)
            {
                return await Task.FromResult(default(T));
            }
            catch (Exception e)
            {
                return await Task.FromException<T>(e);
            }
        }
        public static async Task ExpectCancellationAsync(this Task input)
        {
            try
            {
                await input;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                await Task.FromException(e);
            }
        }

        public static IObservable<Unit> NotifyWhenAnyPropertyChanged(this ReactiveObject @this, params string[] properties)
            => @this.WhenAnyPropertyChanged(properties).Select(_ => Unit.Default);

        public static void SubscribeDispose<T>(this IObservable<T> @this, CompositeDisposable disposedWith)
            => @this.Subscribe().DisposeWith(disposedWith);


        public static IObservable<T> StartWith<T>(this IObservable<T> @this, T value)
            => Observable.Return(value).Concat(@this);


       
       public static void GracefullyLoad<T>(this IObservableCollection<T> @this, 
            IEnumerable<T> newItems)
        {
            

            var countSuspender = @this.SuspendCount();
            var notificationSuspender = @this.SuspendNotifications();
            @this.Load(newItems);
            countSuspender.Dispose();
            notificationSuspender.Dispose();
        }

        public static (double Min, double Max) MinMax(this ReadOnlySpan<double> @this)
        {
            if (!@this.IsEmpty)
            {
                var result = (Min: @this[0], Max: @this[0]);
                for (var i = 0; i < @this.Length; i++)
                {
                    var temp = @this[i];
                    if (temp > result.Max)
                    {
                        result.Max = temp;
                    } 
                    else if (temp < result.Min)
                    {
                        result.Min = temp;
                    }
                }

                return result;
            }

            return (double.NaN, double.NaN);
        }

        public static string SanitizePath(this ReadOnlySpan<char> s)
        {
            const int maxStackSize = 256;
            var len = s.Length;
            char[] borrowedBuffer = null;
            Span<char> buffer = len > maxStackSize
                ? (borrowedBuffer = ArrayPool<char>.Shared.Rent(len)).AsSpan(0, len)
                : stackalloc char[len];

            try
            {
                s.CopyTo(buffer);
                Span<char> disallowedChars = System.IO.Path.GetInvalidPathChars().AsSpan();
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (disallowedChars.IndexOf(buffer[i]) < 0)
                    {
                        continue;
                    }

                    buffer[i] = '_';
                }

                return buffer.ToString();
            }
            catch (Exception e)
            {
                if (Injector.GetLogger() is { } logger)
                {
                    logger.Error(e, "Failed to sanitize path.");
                }

                return "00";
            }
            finally
            {
                if (borrowedBuffer is not null)
                {
                    ArrayPool<char>.Shared.Return(borrowedBuffer);
                }
            }
        }

        internal static string GetEnumString(string key, Type type)
            => Properties.Localization.ResourceManager
                         .GetString($"General_{type.Name}_{key}")
               ?? (type.GetField(key).GetCustomAttribute(
                       typeof(DescriptionAttribute)) is
                   DescriptionAttribute attr
                   ? attr.Description
                   : key);



        internal static double Interpolate(double x1, double x2, double y1, double y2, double x0)
        {
            if (Math.Abs(x2 - x1) < 1e-15)
            {
                return 0.0;
            }
            return y1 + (y2 - y1) / (x2 - x1) * (x0 - x1);
        }

        internal static double Percentile(ReadOnlySpan<double> data, double p)
        {
            if (data.IsEmpty || p is < 0 or > 1)
            {
                return double.NaN;
            }

            if (Math.Abs(p) < DEpsilon)
            {
                return data[0];
            }

            if (Math.Abs(1 - p) < DEpsilon)
            {
                return data[data.Length - 1];
            }

            
            var buffer = ArrayPool<double>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(buffer);
                Array.Sort(buffer, 0, data.Length);

                // Strictly non-negative value between `0` and `n - 1`
                var x = (data.Length - 1) * p;

                // Integral part of `x`
                var i = (int)Math.Floor(x);
                // Non-integral part of `x`
                var g = x - i;

                if (i == data.Length - 1)
                {
                    return buffer[i];
                }

                return (1 - g) * buffer[i] + g * buffer[i + 1];
            }
            finally
            {
                ArrayPool<double>.Shared.Return(buffer);
            }
        }

    }

    internal class CameraTupleOrderComparer : IComparer<(string Id, IDevice Camera)>
    {
        public static IComparer<(string Id, IDevice Camera)> Default { get; } = new CameraTupleOrderComparer();
        public int Compare((string Id, IDevice Camera) x, (string Id, IDevice Camera) y)
            => ConverterImplementations.CameraToIndexConversion(x.Camera)
                .CompareTo(ConverterImplementations.CameraToIndexConversion(y.Camera));
    }


}
