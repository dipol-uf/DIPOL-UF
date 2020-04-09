using System;
using System.Linq;
using System.Reflection;

#nullable enable
namespace Sandbox
{
    internal static class Extensions
    {
        public static T[] SetAll<T>(this T[] items, string methodName, params object?[] @params)
        {
            var type = typeof(T);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
                throw new ArgumentException(nameof(methodName));
            if(method.GetParameters().Length != @params?.Length)
                throw new ArgumentException(nameof(@params));

            foreach (var item in items)
                method.Invoke(item, @params);

            return items;
        }


        public static TResult[] InvokeAll<TSource, TResult>(this TSource[] items, 
            string methodName, params object?[] @params)
        {
            var type = typeof(TSource);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
                throw new ArgumentException(nameof(methodName));
            if (method.GetParameters().Length != @params?.Length)
                throw new ArgumentException(nameof(@params));

            return items.Select(x => method.Invoke(x, @params)).Cast<TResult>().ToArray();
        }


        public static TResult[] GetAll<TInput, TResult>(this TInput[] items, string propertyName)
        {
            var type = typeof(TResult);
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null)
                throw new ArgumentException(nameof(propertyName));

            return items.Select(x => prop.GetValue(x)).Cast<TResult>().ToArray();
        }
    }
}
