#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ANDOR_CS.Classes;
using Newtonsoft.Json;
using Serializers;
using Serilog.Events;

namespace DIPOL_UF
{
    [AttributeUsage(AttributeTargets.Enum)]
    internal class DescriptionProviderAttribute : Attribute
    {
        public string DescriptionKey { get; }

        public DescriptionProviderAttribute(string key) =>
            DescriptionKey = key ?? throw new ArgumentNullException(nameof(key));
    }

    internal record EnumNameRep(
        string Full, 
        string Special, 
        [property: JsonIgnore] bool IgnoreDefault = false,
        [property: JsonIgnore] string? Name = null
    );
    
    internal static class EnumHelper
    {
        // Assuming concurrency level of 2 (threads), likely UI and some worker, and 16 initial elements
        private static readonly ConcurrentDictionary<Type, object> StringRepresentationMaps = new(2, 16);

        // ReSharper disable once InconsistentNaming
        private static readonly MethodInfo GetEnumNameRepresentations_GenericHandle =
            typeof(EnumHelper).GetMethod(nameof(GetEnumNameRepresentations), Type.EmptyTypes)!
                              .GetGenericMethodDefinition();
        
        // ReSharper disable once InconsistentNaming
        private static readonly MethodInfo GetEnumNameRep_GenericHandle =
            typeof(EnumHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
                              .First(x => x is {Name: nameof(GetEnumNameRep), IsGenericMethod: true})
                              .GetGenericMethodDefinition();

        // ReSharper disable once InconsistentNaming
        private static readonly MethodInfo FromDescription_GenericHandle =
            typeof(EnumHelper).GetMethod(nameof(FromDescription), new[] {typeof(string)})!
                              .GetGenericMethodDefinition();

        public static Enum? FromDescription(string description, Type type)
        {
            if (
                string.IsNullOrWhiteSpace(description) || 
                type is not {IsEnum: true}
            )
            {
                return null;
            }

            return (Enum)FromDescription_GenericHandle.MakeGenericMethod(type).Invoke(null, new object[] {description});
        }
        
        public static T? FromDescription<T>(string description) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            IImmutableDictionary<T, EnumNameRep> nameReps = GetEnumNameRepresentations<T>();
            foreach (var (value, (fullName, specialName, _, name)) in nameReps)
            {
                if (
                    string.Equals(description, fullName, StringComparison.Ordinal) ||
                    string.Equals(description, specialName, StringComparison.Ordinal) ||
                    string.Equals(description, name, StringComparison.Ordinal)
                )
                {
                    return value;
                }
            }

            return null;
        }
        
        
        public static System.Collections.IDictionary? GetEnumNameRepresentations(Type type)
        {
            return type.IsEnum
                ? GetEnumNameRepresentations_GenericHandle.MakeGenericMethod(type).Invoke(null, Array.Empty<object>())
                    as System.Collections.IDictionary
                : null;
        }
        
        public static IImmutableDictionary<T, EnumNameRep> GetEnumNameRepresentations<T>()  where T : Enum
        {
            if (StringRepresentationMaps.TryGetValue(typeof(T), out var value))
            {
                switch (value)
                {
                    case IImmutableDictionary<T, EnumNameRep> nameRep1:
                        return nameRep1;
                    
                    // This path should never be taken, but if it is, just re-generate value and log
                    default:
                        Helper.WriteLog(
                            LogEventLevel.Warning,
                            "Type mismatch when retrieving names of enum `{Enum}`. This should never happen.", typeof(T)
                        );

                        return GenerateEnumStrings<T>();
                }
            }

            IImmutableDictionary<T, EnumNameRep> nameRep2 = GenerateEnumStrings<T>();
            StringRepresentationMaps.TryAdd(typeof(T), nameRep2);
            return nameRep2;
        }

        public static EnumNameRep GetEnumNameRep<T>(this T value)  where T : Enum
        {
            IImmutableDictionary<T, EnumNameRep> nameReps = GetEnumNameRepresentations<T>();
            if (!IsFlag<T>())
            {
                // This never throws
                return nameReps[value];
            }

            var fullSb = new StringBuilder(16 * nameReps.Count);
            var specialSb = new StringBuilder(8 * nameReps.Count);
            var isFirst = true;
            foreach(var (enumVal, (full, special, ignoreDefault, _)) in nameReps)
            {
                if (ignoreDefault || !HasFlagTyped(value, enumVal))
                {
                    continue;
                }
                
                if (!isFirst)
                {
                    fullSb.Append(" | ");
                    specialSb.Append(" | ");
                }
                else
                {
                    isFirst = false;
                }

                fullSb.Append(full);
                specialSb.Append(special);
            }

            return new EnumNameRep(fullSb.ToString(), specialSb.ToString());
        }

        // Generic method is guaranteed to return EnumNameRep!
        public static EnumNameRep GetEnumNameRep(this Enum @enum) =>
            (GetEnumNameRep_GenericHandle
             .MakeGenericMethod(@enum.GetType()).Invoke(
                 null, new object[] {@enum}
             ) as EnumNameRep)!;

        public static ImmutableArray<T> GetFlagValues<T>(this T value, bool ignoreDefault = true)  where T : Enum
        {
            IImmutableDictionary<T, EnumNameRep> nameReps = GetEnumNameRepresentations<T>();
            var builder = ImmutableArray.CreateBuilder<T>(nameReps.Count);

            foreach (var (flag, (_, _, currentDefaultIgnored, _)) in nameReps)
            {
                if (HasFlagTyped(value, flag) && !(ignoreDefault && currentDefaultIgnored))
                {
                    builder.Add(flag);
                }
            }

            return builder.ToImmutable();
        }

        public static ImmutableArray<EnumNameRep> GetEnumNamesRep<T>(params T[] @this)  where T : Enum
        {
            IImmutableDictionary<T, EnumNameRep> enumReps = GetEnumNameRepresentations<T>();
            var builder = ImmutableArray.CreateBuilder<EnumNameRep>(@this.Length);
            foreach (var value in @this)
            {
                builder.Add(enumReps[value]);
            }

            return builder.ToImmutable();
        }
        
        public static ImmutableArray<EnumNameRep> GetEnumNamesRep<T>(this IReadOnlyList<T> @this)  where T : Enum
        {
            IImmutableDictionary<T, EnumNameRep> enumReps = GetEnumNameRepresentations<T>();
            var builder = ImmutableArray.CreateBuilder<EnumNameRep>(@this.Count);
            for (var i = 0; i < @this.Count; i++)
            {
                builder.Add(enumReps[@this[i]]);
            }
            return builder.ToImmutable();
        }
        
        public static ImmutableArray<EnumNameRep> GetEnumNamesRep<T>(this IEnumerable<T> @this)  where T : Enum
        {
            IImmutableDictionary<T, EnumNameRep> enumReps = GetEnumNameRepresentations<T>();
            return @this.Select(x => enumReps[x]).ToImmutableArray();
        }
        
        public static bool HasFlagTyped<T>(this T @enum, T flag)  where T : Enum => Equals(And(@enum, flag), flag);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable EntityNameCapturedOnly.Global
        public static T Or<T>(T left, T right)  where T : Enum
        {
            InlineIL.IL.Emit.Ldarg(nameof(left));
            InlineIL.IL.Emit.Ldarg(nameof(right));
            InlineIL.IL.Emit.Or();
            return InlineIL.IL.Return<T>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T And<T>(T left, T right)  where T : Enum
        {
            InlineIL.IL.Emit.Ldarg(nameof(left));
            InlineIL.IL.Emit.Ldarg(nameof(right));
            InlineIL.IL.Emit.And();
            return InlineIL.IL.Return<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals<T>(T left, T right)  where T : Enum
        {
            InlineIL.IL.Emit.Ldarg(nameof(left));
            InlineIL.IL.Emit.Ldarg(nameof(right));
            InlineIL.IL.Emit.Ceq();
            return InlineIL.IL.Return<bool>();
        }
        // ReSharper restore EntityNameCapturedOnly.Global
        
        private static IImmutableDictionary<T, EnumNameRep> GenerateEnumStrings<T>() where T : Enum
        {
            var type = typeof(T);
            var nameProviderJson =
                type.GetCustomAttribute<DescriptionProviderAttribute>() is {DescriptionKey: { } key} &&
                Properties.EnumStrings.ResourceManager.GetString(key) is { } jsonRep
                    ? jsonRep
                    : null;

            IReadOnlyDictionary<string, EnumNameRep>? nameProvider = null;
            try
            {
                nameProvider = nameProviderJson is not null
                    ? JsonConvert.DeserializeObject<Dictionary<string, EnumNameRep>>(nameProviderJson)
                    : null;
            }
            catch (Exception)
            {
                // Ignore
            }

            FieldInfo[] valueFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var builder = ImmutableDictionary.CreateBuilder<T, EnumNameRep>();
            for (var i = 0; i < valueFields.Length; i++)
            {
                var fifo = valueFields[i];
                var name = fifo.Name;
                var isDefaultIgnored = fifo.GetCustomAttribute<IgnoreDefaultAttribute>() is not null;
                // The value can always be cast to `Enum`
                var enumVal = (T)fifo.GetValue(null)!;
                EnumNameRep enumRep;
                if (nameProvider is not null && nameProvider.TryGetValue(name, out var retrievedRep))
                {
                    enumRep = retrievedRep with {IgnoreDefault = isDefaultIgnored, Name = name};
                }
                else if (fifo.GetCustomAttribute<DescriptionAttribute>() is {Description: { } enumDesc})
                {
                    enumRep = new EnumNameRep(enumDesc, enumDesc, isDefaultIgnored, name);
                }
                else
                {
                    enumRep = new EnumNameRep(name, name, isDefaultIgnored, name);
                }
                
                builder.Add(enumVal, enumRep);
            }

            return builder.ToImmutable();
        }
        private static bool IsFlag<T>()  where T : Enum => typeof(T).GetCustomAttribute<FlagsAttribute>() is not null;
      
    }
}