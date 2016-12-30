using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ghost.Core
{
    internal class SettingsManager : ISettingsManager
    {
        private const string SettingsExt = ".json";
        private const string SettingsPrfx = ".Settings.CFG_";
        private const string SettingsFile = "GhostSettings" + SettingsExt;

        private IDictionary<string, object> m_settings;

        public SettingsManager()
        {
            m_settings = new ConcurrentDictionary<string, object>();
            Reload();
        }

        public void Save()
        {
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(m_settings));
        }

        public void Reload()
        {
            if (File.Exists(SettingsFile))
                JsonConvert.PopulateObject(File.ReadAllText(SettingsFile), m_settings);
            CreateDefaults();
        }

        public T Get<T>(string name)
        {
            object @object;
            if (m_settings.TryGetValue(name, out @object))
            {
                if (!(@object is T))
                {
                    @object = JsonConvert.DeserializeObject<T>(@object.ToString());
                    m_settings[name] = @object;
                }
                return (T)@object;
            }
            return default(T);
        }

        public void Set<T>(string name, T value)
        {
            m_settings[name] = value;
        }

        private void CreateDefaults()
        {
            AppDomain.CurrentDomain.GetAssemblies().Where(x=> !x.IsDynamic).SelectMany(x =>
            {
                var name = x.GetName().Name + SettingsPrfx;
                return x.GetManifestResourceNames()
                 .Where(y => y.StartsWith(name) && y.EndsWith(SettingsExt))
                 .Select(y =>
                 {
                     using (var stream = x.GetManifestResourceStream(y))
                     {
                         using (var reader = new StreamReader(stream))
                         {
                             return new KeyValuePair<string, object>(y.Substring(name.Length, y.Length - (name.Length + SettingsExt.Length)), 
                                 JsonConvert.DeserializeObject(reader.ReadToEnd()));
                         }
                     }
                 });
            }).Aggregate(m_settings, (x, y) =>
            {
                if (!x.ContainsKey(y.Key)) x.Add(y);
                return x;
            });
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(m_settings));
        }
    }
}