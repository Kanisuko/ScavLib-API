using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ScavLib.save
{

    internal static class SaveCompanionFile
    {
        private const string FileName = "save.scavlib.sv";

        internal static Func<string> SaveDirResolver = () => Application.persistentDataPath;

        private static string FilePath
            => Path.Combine(Application.persistentDataPath, FileName);

        internal static bool Exists() => File.Exists(FilePath);

        internal static void Write(SaveCompanionData data)
        {
            if (data == null) return;

            try
            {
                string json = JsonConvert.SerializeObject(
                    data,
                    Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                File.WriteAllBytes(FilePath, Zip(json));

                ScavLibPlugin.Log.LogInfo(
                    $"[SaveCompanionFile] Wrote {data.items.Count} item(s) and " +
                    $"{data.recipes.Count} recipe(s) to '{FileName}'.");
            }
            catch (Exception ex)
            {
                ScavLibPlugin.Log.LogError(
                    $"[SaveCompanionFile] Failed to write companion file: {ex}");
            }
        }

        internal static SaveCompanionData Read()
        {
            if (!Exists()) return null;

            try
            {
                byte[] compressed = File.ReadAllBytes(FilePath);
                string json = Unzip(compressed);
                var data = JsonConvert.DeserializeObject<SaveCompanionData>(json);

                if (data == null)
                {
                    ScavLibPlugin.Log.LogWarning(
                        "[SaveCompanionFile] Companion file deserialized to null.");
                    return null;
                }

                ScavLibPlugin.Log.LogInfo(
                    $"[SaveCompanionFile] Read {data.items?.Count ?? 0} item(s) and " +
                    $"{data.recipes?.Count ?? 0} recipe(s) from '{FileName}' " +
                    $"(schema v{data.version}).");

                return data;
            }
            catch (Exception ex)
            {
                ScavLibPlugin.Log.LogError(
                    $"[SaveCompanionFile] Failed to read companion file: {ex}. " +
                    $"Treating as 'no companion data'.");
                return null;
            }
        }

        internal static void DeleteIfExists()
        {
            try
            {
                if (Exists()) File.Delete(FilePath);
            }
            catch (Exception ex)
            {
                ScavLibPlugin.Log.LogWarning(
                    $"[SaveCompanionFile] Failed to delete companion file: {ex.Message}");
            }
        }

        private static byte[] Zip(string s)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(
                    ms, System.IO.Compression.CompressionLevel.Optimal))
                using (var sw = new StreamWriter(gz, Encoding.UTF8))
                {
                    sw.Write(s);
                }
                return ms.ToArray();
            }
        }

        private static string Unzip(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var gz = new GZipStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(gz, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
