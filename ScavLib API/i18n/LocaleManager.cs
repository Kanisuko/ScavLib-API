using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace ScavLib.i18n
{
    public static class LocaleManager
    {

        private static readonly Dictionary<string, JObject> JsonLocaleCache = new();

        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> ManualItems = new();
        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> ManualOthers = new();

        public static readonly HashSet<string> RegisteredItemIds = new();

        public static void AutoRegister(string modId, string modFolderPath)
        {
            string langPath = Path.Combine(modFolderPath, "lang");
            if (!Directory.Exists(langPath)) return;
            if (!JsonLocaleCache.ContainsKey(modId)) JsonLocaleCache[modId] = new JObject();

            LoadJsonToCache(modId, Path.Combine(langPath, "EN.json"));
            string current = GetGameLanguageCode();
            if (current != "EN") LoadJsonToCache(modId, Path.Combine(langPath, $"{current}.json"));
        }

        public static void RegisterItem(string id, IReadOnlyDictionary<string, string> names, IReadOnlyDictionary<string, string> descs = null)
        {
            RegisteredItemIds.Add(id);
            if (names != null) ManualItems[id] = names;
            if (descs != null) ManualOthers[id] = descs;
        }

        public static void RegisterString(string key, IReadOnlyDictionary<string, string> translations)
        {
            if (translations != null) ManualOthers[key] = translations;
        }

        public static void PerformInjection()
        {
            if (Locale.currentLang == null) return;

            string langCode = GetGameLanguageCode();
            InjectManualToNative(ManualItems, Locale.currentLang.main, langCode);
            InjectManualToNative(ManualOthers, Locale.currentLang.other, langCode);

            foreach (var mod in JsonLocaleCache)
            {
                var root = mod.Value;
                InjectJsonToNative(root["items"], Locale.currentLang.main);
                InjectJsonToNative(root["buildings"], Locale.currentLang.buildings);
                InjectJsonToNative(root["moodles"], Locale.currentLang.moodles);
                InjectJsonToNative(root["other"], Locale.currentLang.other);
                InjectJsonToNative(root, Locale.currentLang.other, true);
            }
        }

        private static void InjectManualToNative(Dictionary<string, IReadOnlyDictionary<string, string>> source, Dictionary<string, string> target, string langCode)
        {
            foreach (var entry in source)
            {

                if (entry.Value.TryGetValue(langCode, out string text) ||
                    entry.Value.TryGetValue("EN", out text) ||
                    entry.Value.TryGetValue("English", out text))
                {
                    target[entry.Key] = text;
                }
                else if (entry.Value.Count > 0)
                {
                    target[entry.Key] = entry.Value.Values.First();
                }
            }
        }

        private static void InjectJsonToNative(JToken source, Dictionary<string, string> target, bool ignoreReserved = false)
        {
            if (source == null || !source.HasValues) return;
            var reserved = new[] { "items", "buildings", "moodles", "other" };
            foreach (var pair in Flatten(source as JObject))
            {
                if (ignoreReserved && reserved.Any(r => pair.Key.StartsWith(r))) continue;
                target[pair.Key] = pair.Value;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Flatten(JObject jObject, string prefix = "")
        {
            if (jObject == null) yield break;
            foreach (var property in jObject.Properties())
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                if (property.Value is JObject nested)
                    foreach (var pair in Flatten(nested, key)) yield return pair;
                else
                    yield return new KeyValuePair<string, string>(key, property.Value.ToString());
            }
        }

        public static string GetGameLanguageCode() => Locale.currentLangName ?? PlayerPrefs.GetString("locale", "EN");

        private static void LoadJsonToCache(string modId, string path)
        {
            if (!File.Exists(path)) return;
            JsonLocaleCache[modId].Merge(JObject.Parse(File.ReadAllText(path)));
        }

        public static void RefreshAllUI()
        {
            var localizers = UnityEngine.Object.FindObjectsOfType<UILocalizer>();
            foreach (var ui in localizers)
            {
                if (ui.TryGetComponent<TextMeshProUGUI>(out var textComp))
                {

                    string translated = Locale.GetOther(ui.key);
                    textComp.text = ui.upper ? translated.ToUpper() : translated;
                }
            }
        }
    }
}
