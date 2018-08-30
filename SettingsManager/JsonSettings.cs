using System.Collections;
using Json = System.Collections.Generic.Dictionary<string, object>;

namespace SettingsManager
{
    public class JsonSettings
    {
        private readonly Json _rawSettings;

        public int Count => _rawSettings.Count;

        public object this[in string name] => Get(name);


        public void Set(in string name, object val)
        {
            if (HasKey(name))
                _rawSettings[name] = val;
            else
                _rawSettings.Add(name, val);
        }

        public void Clear(in string name) => Set(name, null);

        public bool TryRemove(in string name) =>
            _rawSettings.Remove(name);
            

        public bool HasKey(in string name) =>
            _rawSettings?.ContainsKey(name) ?? false;

        public T Get<T>(in string name) =>
            !HasKey(name) ? default : (T) _rawSettings[name];

        public T[] GetArray<T>(in string name) =>
            !HasKey(name) ? new T[] { } : Get<ArrayList>(name).ToArray(typeof(T)) as T[];
        
        public JsonSettings GetJson(in string name) =>
            !HasKey(name) ? null : new JsonSettings(Get<Json>(name));

        public object Get(in string name) =>
            _rawSettings[name];

        public bool TryGet<T>(in string name, out T value)
        {

            if (_rawSettings.ContainsKey(name) && 
                _rawSettings[name] is T result)
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }
            

        public JsonSettings(in string input)
        {
            _rawSettings = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Json>(input);
        }

        private JsonSettings(in Json json)
        {
            _rawSettings = json;
        }
    }
}
