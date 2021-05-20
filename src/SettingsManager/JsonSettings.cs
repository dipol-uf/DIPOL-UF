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

        public object? this[string name] => Get(name);

        public void Set(string name, object? val)
        {
            if (HasKey(name))
                _rawObject[name]?.Replace(new JValue(val));
            else
                _rawObject.Add(name, new JValue(val));
        }

        public void Clear(string name) => Set(name, null);

        public bool TryRemove(string name)
			=> _rawObject.Remove(name);


        public bool HasKey(string name)
			=> _rawObject.ContainsKey(name);

        [return:MaybeNull]
        public T Get<T>(string name)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            => _rawObject.TryGetValue(name, out var value) && value is not null ? value.ToObject<T>() : default;        

        public T Get<T>(string name, T fallback)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            => _rawObject.TryGetValue(name, out var value) && value is not null && value.ToObject<T>() is {} obj ? obj : fallback; 

        public T[] GetArray<T>(string name)
            => _rawObject.TryGetValue(name, out var value) && value is JArray jArray && jArray.ToObject<T[]>() is { } array ? array : Array.Empty<T>();

        public JsonSettings? GetJson(string name)
            => _rawObject.TryGetValue(name, out var value) && value is JObject obj ? new JsonSettings(obj) : null;

        public object? Get(string name)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            => _rawObject.TryGetValue(name, out var value) && value is not null ? value.ToObject(typeof(object)) : null;

        public bool TryGet<T>(
            string name,
            [MaybeNullWhen(false)]out T value
        )
        {
            value = default;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once PatternAlwaysOfType
            if (_rawObject.TryGetValue(name, out var token) && token is not null && token.Value<T>() is T result)
            {
                value = result;
                return true;
            }

            return false;
        }

        public string WriteString()
			=> JsonConvert.SerializeObject(_rawObject, Formatting.Indented);

        public JsonSettings()
			=> _rawObject = new JObject();

        public JsonSettings(string input)
			=> _rawObject = JsonConvert.DeserializeObject(input) as JObject ?? throw new InvalidOperationException();

        public JsonSettings(in JObject json)
			=> _rawObject = json;
    }
}
