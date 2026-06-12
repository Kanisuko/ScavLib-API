using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using KrokoshaCasualtiesMP;

namespace ScavLib.compat.krokmp
{
    internal static class KrokMpBridge
    {
        internal const string Guid = "KrokoshaCasualtiesMP";
        internal static bool Enabled { get; private set; }
        public static bool IsEnabled => Enabled;
        internal static void Init(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(Guid)) return;
            Enabled = true;

            try
            {
                harmony.PatchAll(typeof(KrokMpGoSyncPatch));
                harmony.PatchAll(typeof(KrokMpConSpawnPatch));

                harmony.PatchAll(typeof(KrokMpValidationPatch));

                save.SaveCompanionFile.SaveDirResolver = ResolveMpSaveDir;
                ScavLibPlugin.Log.LogInfo("[KrokMpBridge] Krokosha MP bridge initialized with ValidationFix.");
            }
            catch (Exception ex)
            {
                Enabled = false;
                ScavLibPlugin.Log.LogError($"[KrokMpBridge] Init failed: {ex}");
            }
        }

        private static string ResolveMpSaveDir()
        {

            if (!KrokoshaScavMultiplayer.network_system_is_running)
                return Application.persistentDataPath;

            if (!string.IsNullOrEmpty(SavesystemPatch.savedatapathreplacement))
                return SavesystemPatch.savedatapathreplacement;

            if (SavesystemPatch.HasMultiplayerSaveFile())
                return SavesystemPatch.mpsavefolder;

            return Application.persistentDataPath;
        }
    }
}
