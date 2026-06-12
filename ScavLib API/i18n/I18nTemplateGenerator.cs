using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ScavLib.i18n
{

    public static class TranslationTemplateGenerator
    {

        public static void ExportTemplate(string outputPath)
        {

            var root = new JObject();
            var itemsObj = new JObject();
            var otherObj = new JObject();

            foreach (var id in LocaleManager.RegisteredItemIds)
            {
                itemsObj[id] = $"TODO: {id} Name";
            }

            root["items"] = itemsObj;
            root["other"] = otherObj;

            if (File.Exists(outputPath))
            {
                try
                {
                    var existing = JObject.Parse(File.ReadAllText(outputPath));

                    root.Merge(existing, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Union,
                        MergeNullValueHandling = MergeNullValueHandling.Ignore
                    });
                }
                catch (System.Exception e)
                {
                    ScavLibPlugin.Log.LogError($"[ScavLib] i18n Generator Merge Error: {e.Message}");
                }
            }

            string json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(outputPath, json);
            ScavLibPlugin.Log.LogInfo($"[ScavLib] i18n Template updated at: {outputPath}");
        }
    }
}
