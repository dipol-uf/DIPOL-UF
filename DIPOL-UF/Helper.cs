using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DynamicData;

namespace DIPOL_UF
{
    public static class Helper
    {
        public static Dispatcher UiDispatcher =>
            Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("Dispatcher is unavailable.");


        public static Size MeasureString(string strToMeasure, TextBlock presenter)
        {
            var formattedText = new FormattedText(strToMeasure,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    presenter.FontFamily,
                    presenter.FontStyle,
                    presenter.FontWeight,
                    presenter.FontStretch),
                presenter.FontSize,
                presenter.Foreground,
                VisualTreeHelper.GetDpi(presenter).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public static T FindParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            if (element is T)
                return element as T;

            var parent = VisualTreeHelper.GetParent(element);

            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);

            return parent as T;
        }

        public static DependencyObject FindParentByName(DependencyObject element, string name)
        {
            var parent = VisualTreeHelper.GetParent(element);

            while (parent is FrameworkElement elem && elem.Name != name)
                parent = VisualTreeHelper.GetParent(elem);

            return (parent is FrameworkElement e && e.Name == name) ? parent : null;
        }

        public static bool IsDialogWindow(Window window)
        {
            var showingAsDialogField = typeof(Window).GetField(
                @"_showingAsDialog",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return (bool) (showingAsDialogField?.GetValue(window) ?? false);
        }

        public static void WriteLog(string entry)
        {
            System.Diagnostics.Debug.WriteLine(entry);
        }

        public static void WriteLog(ANDOR_CS.Exceptions.AndorSdkException entry)
        {
            System.Diagnostics.Debug.WriteLine($"{entry.Message} [{entry.ErrorCode}]");
        }

        public static void WriteLog(object entry)
            => WriteLog(entry.ToString());

        public static string GetCameraHostName(string input)
        {
            var splits = input.Split(':');
            if (splits.Length > 0)
            {
                var host = splits[0];
                if (!string.IsNullOrWhiteSpace(host))
                    return host.ToLowerInvariant().Trim() == "localhost"
                        ? Properties.Localization.General_LocalHostName
                        : host;
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if dispatcher has not been shut down yet.
        /// </summary>
        /// <param name="d">Dispatcher instance (usually <see cref="Dispatcher"/>)</param>
        /// <returns>True if Dispatcher.Invoke can still be called.</returns>
        public static bool IsAvailable(this Dispatcher d)
            => !d.HasShutdownStarted && !d.HasShutdownFinished;

        public static void ExecuteOnUI(Action action)
            => ExecuteOnUi(action);

        public static T ExecuteOnUI<T>(Func<T> action)
            => ExecuteOnUi(action);

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

        /// <summary>
        /// Retrieves an item from a dictionary if specified key is present; otherwise, returns null.
        /// </summary>
        /// <param name="settings">The dictionary.</param>
        /// <param name="key">Key.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Either item associated with key, or null, if not present.</returns>
        public static object GetValueOrNullSafe(this Dictionary<string, object> settings, string key)
            => (settings ?? throw new ArgumentNullException($"{nameof(settings)} argument cannot be null."))
                .TryGetValue(key, out object item)
                    ? item
                    : null;

        public static T GetValueOrNullSafe<T>(this Dictionary<string, object> settings, string key,
            T nullReplacement = default)
        {
            var tempValue = GetValueOrNullSafe(settings, key);

            object value;

            if (tempValue is ArrayList list)
                value = list.ToArray();
            else
                value = tempValue;

            if (value is T safe)
                return safe;
            else
                return nullReplacement;
        }

        public static string ArrayToString(this Array array, string separator = ", ")
            => array.EnumerableToString(separator);

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
                // Itereates through values, retrieves description for each
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
        /// <returns>An array of flags found in input parameter, one flag per each aray item.</returns>
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

            string GetString(string key)
                => Properties.Localization.ResourceManager
                              .GetString($"General_{type.Name}_{key}")
                    ?? (type.GetField(key).GetCustomAttribute(
                            typeof(DescriptionAttribute)) is
                        DescriptionAttribute attr
                        ? attr.Description
                        : key);

            return
                type.GetCustomAttribute(typeof(FlagsAttribute)) is null
                    ? new List<string>()
                        {Enum.IsDefined(type, @enum) ? GetString(type.GetEnumName(@enum)) : @enum.ToString()}
                    : type.GetEnumValues()
                               .Cast<Enum>()
                               .Where(@enum.HasFlag)
                               .Select(x => x.ToString())
                               .Select(GetString)
                               .ToList();
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

        public static string ToStringEx(this object @this)
        {
            switch (@this)
            {
                case null:
                    throw new ArgumentNullException(nameof(@this));
                case Enum @enum:
                    return "[" + @enum.GetEnumStringEx().EnumerableToString() + "]";
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
            => Task.Run(action).ConfigureAwait(false);

        public static ConfiguredTaskAwaitable<T> RunNoMarshall<T>(Func<T> action)
            => Task.Run(action).ConfigureAwait(false);

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

        public static void Defer<T>(this IObservable<T> @this, int by)
        {
            
        }
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
    }
#endif
}
