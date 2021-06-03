//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
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
using DIPOL_UF.Converters;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serializers;
using Serilog.Core;
using Serilog.Events;

namespace DIPOL_UF
{
    public static class Helper
    {

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
        public static void WriteLog(string entry) => Injector.GetLogger().Write(LogEventLevel.Information, entry);

        public static void WriteLog(ANDOR_CS.Exceptions.AndorSdkException entry) => WriteLog($"{entry.Message} [{entry.ErrorCode}]");

        public static void WriteLog(object entry) => WriteLog(entry.ToString());

        public static void WriteLog(LogEventLevel level, string template, params object[] args) => Injector.GetLogger().Write(level, template, args);
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
                        host = @"net.tcp://" + host;
                    if (Uri.TryCreate(host, UriKind.RelativeOrAbsolute, out var uri)
                        && uri.Host.ToLowerInvariant() is var invariantHost)
                        return invariantHost == "localhost" || invariantHost == "127.0.0.1"
                            ? Properties.Localization.General_LocalHostName
                            : uri.Host;

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

        /// <summary>
        /// Gets value of <see cref="DescriptionAttribute"/> for an item from a given enum.
        /// </summary>
        /// <param name="enumValue">Value from the enum.</param>
        /// <param name="enumType">Enum type. If types mismatch, returns name of the field or null.</param>
        /// <returns></returns>
        public static string GetEnumDescription(object enumValue, Type enumType)
        {
            // Retrieves string representation of the enum value
            var fieldName = Enum.GetName(enumType, enumValue);

            if (fieldName is null)
                return enumValue.ToString();
            // Which corresponds to field name of Enum-derived class
            var descriptionAttr = enumType
                                  .GetField(fieldName)
                                  // It is possible that such field is not defined (type error), from this point return null
                                  ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                  .DefaultIfEmpty(null)
                                  .FirstOrDefault();

            // Casts result to DescriptionAttribute. 
            // If attribute is not found or value is of wrong type, cast gives null and method returns fieldName
            return (descriptionAttr as DescriptionAttribute)?.Description ?? fieldName;

        }

        /// <summary>
        /// Gets value of <see cref="Enum"/> for a given value of <see cref="DescriptionAttribute"/>.
        /// </summary>
        /// <param name="description">Description string as defined in attribute constructor.</param>
        /// <param name="enumType">Enum type.</param>
        /// <returns></returns>
        public static object GetEnumFromDescription(string description, Type enumType)
        {
            // Gets all declared enum values
            var values = Enum.GetValues(enumType);

            // If any present
            if (values.Length > 0)
            {
                // Iterates through values, retrieves description for each
                for (var i = 0; i < values.Length; i++)
                {
                    var local = values.GetValue(i);
                    // If retrieved description equals to passed argument, returns this value.
                    if (GetEnumDescription(local, enumType) == description)
                        return local;
                }
            }

            // If Enum is empty or description is not found/defined, returns null
            return null;
        }

        /// <summary>
        /// Converts <see cref="Enum"/> flags to an array of <see cref="Enum"/> values.
        /// Useful for combobox-like representations.
        /// </summary>
        /// <typeparam name="T"><see cref="Enum"/> type. If not, throws <see cref="ArgumentException"/></typeparam>
        /// <param name="enum">Flags to convert to array.</param>
        /// <exception cref="ArgumentException"/>
        /// <returns>An array of flags found in input parameter, one flag per each array item.</returns>
        public static List<T> EnumFlagsToArray<T>(Enum @enum) where T :Enum
        {
            return Enum
                   .GetValues(@enum.GetType())
                   .Cast<Enum>()
                   .Where(@enum.HasFlag)
                   .Cast<T>()
                   .ToList();
        }

        public static List<string> GetEnumStringEx(this Enum @enum)
        {
            var type = @enum.GetType();

            if (type.GetCustomAttribute(typeof(FlagsAttribute)) is null)
                return new List<string>
                {
                    Enum.IsDefined(type, @enum) ? GetEnumString(type.GetEnumName(@enum), type) : @enum.ToString()
                };

            var enumDesc = GetEnumNames(type);

            var @default = enumDesc
                           .Where(x => type.GetField(x.Value).GetCustomAttribute<IgnoreDefaultAttribute>() != null)
                           .ToList();
            if (@default.Count == 1 && Equals(@enum, @default[0].Key))
                return new List<string>
                {
                    GetEnumString(@default[0].Value, type)
                };


            return enumDesc.Where(x => type.GetField(x.Value).GetCustomAttribute<IgnoreDefaultAttribute>() is null
                                       && @enum.HasFlag(x.Key))
                           .Select(x => x.Key.ToString())
                           .Select(x => GetEnumString(x, type))
                           .ToList();

        }

        public static Dictionary<Enum, string> GetEnumNames(Type type)
            => Enum.GetValues(type).Cast<Enum>().Zip(Enum.GetNames(type),
                       (x, y) => (x, y))
                   .ToDictionary(x => x.x, x => x.y);

        public static string GetValueTupleString(this ITuple tuple)
        {
            var list = new List<string>(tuple.Length);
            for (var i = 0; i < tuple.Length; i++)
            {
                list.Add(tuple[i].ToStringEx());
            }

            return list.EnumerableToString();
        }

        public static string ToStringEx(this object @this)
        {
            switch (@this)
            {
                case null:
                    throw new ArgumentNullException(nameof(@this));
                case string s:
                    return s;
                case Enum @enum:
                {
                    var coll = @enum.GetEnumStringEx();
                    switch (coll.Count)
                    {
                        case 0:
                            return "";
                        case 1:
                            return coll[0];
                        default:
                            return  '[' + coll.EnumerableToString() + ']';
                    }
                }
                case ITuple tuple:
                    return "(" + tuple.GetValueTupleString() + ")";
                case IEnumerable enumerable:
                    return "[" + enumerable.EnumerableToString() + "]";
                default:
                    return @this.ToString();
            }
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
            => @this.SubscribeMany(x => Disposable.Create(() => disposeAction(x)));

        public static IObservable<IChangeSet<TEntry>> DisposeManyEx<TEntry>(
            this IObservable<IChangeSet<TEntry>> @this,
            Action<TEntry> disposeAction)
            => @this.SubscribeMany(x => Disposable.Create(() => disposeAction(x)));

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
            bool marshal = false)
            => input.ContinueWith((task, param) =>
                    {
#if DEBUG
                        if (!(task.Exception is null))
                            WriteLog(task.Exception.Message);
#endif
                    }, null)
                    .ConfigureAwait(marshal);

        public static ConfiguredTaskAwaitable<T> ExpectCancellation<T>(
            this Task<T> input, bool marshal = false)
            => input.ContinueWith((task, param) =>
                    {
#if DEBUG
                        if (!(task.Exception is null))
                            WriteLog(task.Exception.Message);
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


        public static IDisposable GracefullyBindTo<TKey, TValue>(
            this IObservable<IChangeSet<(TKey, TValue), TKey>> @this,
                IObservableCollection<(TKey, TValue)> target,
                IEqualityComparer<TValue> comparer = null)
        {

            comparer = comparer ?? EqualityComparer<TValue>.Default;

            void Updater(IChangeSet<(TKey, TValue), TKey> batch)
            {
                target.AddRange(batch.Where(x => x.Reason == ChangeReason.Add).Select(x => x.Current));

                foreach (
                    var update in
                    from upd in batch
                    where upd.Reason == ChangeReason.Update && !comparer.Equals(upd.Current.Item2, upd.Previous.Value.Item2)
                    select upd)
                {
                    var index = target.IndexOf(update.Previous.Value);
                    target.Add(update.Current);
                    if (index >= 0)
                        target.RemoveAt(index);
                }

                target.Remove(batch.Where(x => x.Reason == ChangeReason.Remove).Select(x => x.Current));

            }

            return @this.Subscribe(Updater);
        }

        public static IDisposable GracefullyBindTo<TKey, TValue, TTarget>(
            this IObservable<IChangeSet<TValue, TKey>> @this,
            IObservableCollection<TTarget> target,
            Func<TValue, TTarget> selector,
            IEqualityComparer<TValue> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TValue>.Default;

            void Updater(IChangeSet<TValue, TKey> batch)
            {
                target.AddRange(batch.Where(x => x.Reason == ChangeReason.Add).Select(x => selector(x.Current)));

                foreach (
                    var update in
                    from upd in batch
                    where upd.Reason == ChangeReason.Update && !comparer.Equals(upd.Current, upd.Previous.Value)
                    select upd)
                {
                    var index = target.IndexOf(selector(update.Previous.Value));
                    target.Add(selector(update.Current));
                    if (index >= 0)
                        target.RemoveAt(index);
                }

                target.Remove(batch.Where(x => x.Reason == ChangeReason.Remove).Select(x => selector(x.Current)));

            }

            return @this.Subscribe(Updater);
        }

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

        private static string GetEnumString(string key, Type type)
            => Properties.Localization.ResourceManager
                         .GetString($"General_{type.Name}_{key}")
               ?? (type.GetField(key).GetCustomAttribute(
                       typeof(DescriptionAttribute)) is
                   DescriptionAttribute attr
                   ? attr.Description
                   : key);


        
    }

    internal class CameraTupleOrderComparer : IComparer<(string Id, IDevice Camera)>
    {
        public static IComparer<(string Id, IDevice Camera)> Default { get; } = new CameraTupleOrderComparer();
        public int Compare((string Id, IDevice Camera) x, (string Id, IDevice Camera) y)
            => ConverterImplementations.CameraToIndexConversion(x.Camera)
                .CompareTo(ConverterImplementations.CameraToIndexConversion(y.Camera));
    }

#if DEBUG
    internal static class DebugHelper
    {
        public static IObservable<T> LogObservable<T>(
            this IObservable<T> source, 
            string name, 
            CompositeDisposable disposedWith)
        {
            source.Subscribe(x => Helper.WriteLog($"{name}: {x}"))
                  .DisposeWith(disposedWith);

            return source;
        }

        public static void LogTask([CallerMemberName] string name = null)
        {
            Helper.WriteLog($"{name,25} : task: {Task.CurrentId,7}; thread : {Thread.CurrentThread.ManagedThreadId,3}; dispatch : {Application.Current?.Dispatcher?.CheckAccess()}");
        }

        public static IObservable<T> LogTask<T>(
            this IObservable<T> @this,
            [CallerMemberName]
            string name = null)
        {
            @this.Subscribe(x => LogTask(name));
            return @this;
        }

    }
#endif
}
