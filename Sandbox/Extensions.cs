using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable
namespace Sandbox
{

    internal static class Extensions
    {
        public static IEnumerable<T> SetAll<T>(
            this IEnumerable<T> items, string methodName, params object?[] @params)
        {
            var type = typeof(T);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
                throw new ArgumentException(nameof(methodName));
            if(method.GetParameters().Length != @params?.Length)
                throw new ArgumentException(nameof(@params));

            var list = new List<T>();
            foreach (var item in items)
            {
                method.Invoke(item, @params);
                list.Add(item);
            }

            return list;
        }


        public static IEnumerable<TResult> InvokeAll<TSource, TResult>(
            this IEnumerable<TSource> items, 
            string methodName, params object?[] @params)
        {
            var type = typeof(TSource);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
                throw new ArgumentException(nameof(methodName));
            if (method.GetParameters().Length != @params?.Length)
                throw new ArgumentException(nameof(@params));

            return items.Select(item => (TResult) method.Invoke(item, @params)).ToList();
        }


        public static IEnumerable<TResult> GetAll<TInput, TResult>(
            this IEnumerable<TInput> items, 
            string propertyName)
        {
            var type = typeof(TResult);
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null)
                throw new ArgumentException(nameof(propertyName));

            return items.Select(x => (TResult) prop.GetValue(x)).ToList();
        }
    }
}
