using System;
using System.Collections;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Json = System.Collections.Generic.Dictionary<string, object>;

namespace SettingsManager
{
    public class JsonSettings
    {
        private readonly Json _rawSettings;

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

        public bool TryRemove(in string name) =>
            _rawObject.Remove(name);


        public bool HasKey(in string name) =>
            _rawObject.ContainsKey(name);

        public T Get<T>(in string name) =>
            !HasKey(name) ? default : _rawObject.GetValue(name).ToObject<T>();

        public T[] GetArray<T>(in string name) =>
            !HasKey(name) ? new T[] { } : (_rawObject.GetValue(name) as JArray)?.ToObject<T[]>();

        public JsonSettings GetJson(in string name) =>
            !HasKey(name) ? null : new JsonSettings(_rawObject.GetValue(name) as JObject);

        public object Get(in string name) =>
            _rawObject.GetValue(name).ToObject(typeof(object));

        public bool TryGet<T>(in string name, out T value)
        {

            if (HasKey(name) && 
                Get(name) is T result)
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }

        public string WriteString()
        {

            var str = new JavaScriptSerializer().Serialize(_rawSettings);
            return "";
        }


        public JsonSettings(in string input)
        {
            var temp = JsonConvert.DeserializeObject(input) as JObject;
            _rawObject = temp;

        }

        private JsonSettings(in JObject json)
        {
            _rawObject = json;
        }
    }
}
