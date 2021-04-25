using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SettingsManager
{
    public class JsonSettings
    {
        private readonly JObject _rawObject;

        public int Count => _rawObject.Count;

        public object? this[in string name] => Get(name);

        public void Set(in string name, object? val)
        {
            if (HasKey(name))
                _rawObject[name]?.Replace(new JValue(val));
            else
                _rawObject.Add(name, new JValue(val));
        }

        public void Clear(in string name) => Set(name, null);

        public bool TryRemove(in string name)
			=> _rawObject.Remove(name);


        public bool HasKey(in string name)
			=> _rawObject.ContainsKey(name);

        [return:MaybeNull]
        public T Get<T>(in string name)
            => _rawObject.TryGetValue(name, out var value) && value is not null ? value.ToObject<T>() : default;        

        [return:MaybeNull] 
        public T Get<T>(in string name, T fallback)
            => _rawObject.TryGetValue(name, out var value) && value is not null ? value.ToObject<T>() : fallback; 

        public T[] GetArray<T>(in string name)
            => _rawObject.TryGetValue(name, out var value) && value is JArray jArray && jArray.ToObject<T[]>() is T[] array ? array : Array.Empty<T>();

        public JsonSettings? GetJson(in string name)
            => _rawObject.TryGetValue(name, out var value) && value is JObject obj ? new JsonSettings(obj) : null;
			// => !HasKey(name) ? null : new JsonSettings(_rawObject.GetValue(name) as JObject);

        public object? Get(in string name)
            => _rawObject.TryGetValue(name, out var value) && value is not null ? value.ToObject(typeof(object)) : null;

        public bool TryGet<T>(
            in string name,
            [MaybeNullWhen(false)]out T value
        )
        {
            value = default;

            if (_rawObject.TryGetValue(name, out var token) && token is not null && token.Value<T>() is T result)
            {
                value = result;
                return true;
            }

            // if (HasKey(name))
            // {
            //     //if (Get(name) is T result)
            //     //    value = result;
            //
            //     if (Get(name) == null &&
            //         typeof(T) == typeof(object))
            //         value = default;
            //
            //
            //     return true;
            // }

            return false;
        }

        public string WriteString()
			=> JsonConvert.SerializeObject(_rawObject, Formatting.Indented);

        public JsonSettings()
			=> _rawObject = new JObject();

        public JsonSettings(in string input)
			=> _rawObject = JsonConvert.DeserializeObject(input) as JObject ?? throw new InvalidOperationException();

        public JsonSettings(in JObject json)
			=> _rawObject = json;
    }
}
