using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SettingsManager
{
    public class JsonSettings
    {
        private readonly JObject _rawObject;

        public int Count => _rawObject.Count;

        public object this[in string name] => Get(name);

        public void Set(in string name, object val)
        {
            if (HasKey(name))
                _rawObject[name].Replace(new JValue(val));
            else
                _rawObject.Add(name, new JValue(val));
        }

        public void Clear(in string name) => Set(name, null);

        public bool TryRemove(in string name)
			=> _rawObject.Remove(name);


        public bool HasKey(in string name)
			=> _rawObject.ContainsKey(name);

        public T Get<T>(in string name)
			=> !HasKey(name) ? default : _rawObject.GetValue(name).ToObject<T>();

        public T[] GetArray<T>(in string name)
			=> !HasKey(name) ? new T[] { } : (_rawObject.GetValue(name) as JArray)?.ToObject<T[]>();

        public JsonSettings GetJson(in string name)
			=> !HasKey(name) ? null : new JsonSettings(_rawObject.GetValue(name) as JObject);

        public object Get(in string name)
            => _rawObject.GetValue(name)?.ToObject(typeof(object));

        public bool TryGet<T>(in string name, out T value)
        {
            value = default;

            if (HasKey(name))
            {
                if (Get(name) is T result)
                    value = result;

                if (Get(name) == null &&
                    typeof(T) == typeof(object))
                    value = default;


                return true;
            }

            return false;
        }

        public string WriteString()
			=> JsonConvert.SerializeObject(_rawObject, Formatting.Indented);

        public JsonSettings()
			=> _rawObject = new JObject();

        public JsonSettings(in string input)
			=> _rawObject = JsonConvert.DeserializeObject(input) as JObject;

        public JsonSettings(in JObject json)
			=> _rawObject = json;
    }
}
