using UnityEngine;
using UnityEditor;
using System.IO;

namespace VisualScriptingPrompt
{
    [InitializeOnLoad]
    public static class Config
    {
        public static ConfigData data;
        static string configPath = "Packages/com.passivestar.visualscriptingprompt/config.json";

        static Config()
        {
            data = ConfigData.ReadFromJson(configPath);
        }

        [System.Serializable]
        public class ConfigAlias
        {
            public string from;
            public string to;
            public ConfigAlias(string from, string to)
            {
                this.from = from;
                this.to = to;
            }
        }

        [System.Serializable]
        public class ConfigData
        {
            public string[] assemblies;
            public string[] excludeNamespaces;
            public string[] priorityNames;
            public ConfigAlias[] unitAliases;
            public ConfigAlias[] commandAliases;

            public static ConfigData ReadFromJson(string path)
            {
                var json = File.ReadAllText(path);
                var result = JsonUtility.FromJson<ConfigData>(json);
                return result;
            }
        }
    }
}