using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ScavLib.command;
using ScavLib.gui;
using ScavLib.gui.imgui;
using ScavLib.gui.ugui;
using ScavLib.i18n;
using ScavLib.mods;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScavLib
{
    [BepInPlugin("com.kanisuko.scavlib", "ScavLib", Version)]
    [BepInDependency("KrokoshaCasualtiesMP", BepInDependency. DependencyFlags.SoftDependency)]
    public class ScavLibPlugin : BaseUnityPlugin
    {

        public const string Version = "0.7.1";

        public static ScavLibPlugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        internal static readonly Dictionary<string, bool> PatchStatus
            = new Dictionary<string, bool>();
        internal static readonly Dictionary<string, string> PatchErrors
            = new Dictionary<string, string>();

        internal const string SelfModName = "ScavLib";

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            _harmony = new Harmony("com.kanisuko.scavlib");

            ApplyPatchesIndividually();

            CommandRegistry.Init();
            ImguiMenuManager.Init();

            compat.RivalFrameworkDetector.CheckAndWarn();
            compat.krokmp.KrokMpBridge.Init(_harmony);

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnFirstSceneLoaded;

            try
            {
                string pluginDir = Path.GetDirectoryName(Info.Location);
                i18n.LocaleManager.AutoRegister("ScavLib", pluginDir);
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[ScavLib] Failed to initialize self-i18n: {ex.Message}");
            }

            ModRegistry.Register(new ModInfo(
                SelfModName,
                Version,
                "Base API library for Scav Prototype mods.",
                "Kanisuko / QinShenYu"
            ));

            CommandRegistry.TryRegister(new ScavLibCommand(), SelfModName, out _);

            Log.LogInfo($"ScavLib {Version} loaded successfully with i18n 2.0.");
        }

        private void OnFirstSceneLoaded(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnFirstSceneLoaded;
            Log.LogInfo($"[ScavLib] First scene '{scene.name}' loaded — spawning GUI hosts.");
            SpawnGuiHosts();
        }

        private void ApplyPatchesIndividually()
        {
            var patchTypes = new System.Type[]
            {
                typeof(command.CommandRegistryPatch),
                typeof(event_bus.patches.WorldLoadedPatch),
                typeof(event_bus.patches.LayerLoadedPatch),
                typeof(event_bus.patches.WorldDestroyedPatch),
                typeof(event_bus.patches.WorldUnloadingContinueRunPatch),
                typeof(event_bus.patches.WorldUnloadingSaveAndExitPatch),
                typeof(event_bus.patches.ItemDropPatch),
                typeof(event_bus.patches.ItemPickupPatch),
                typeof(gui.ugui.UguiInputBlockerPatch),

                typeof(item.patches.ItemSetupItemsPatch),
                typeof(item.patches.UtilsCreatePatch.PosRot),
                typeof(item.patches.UtilsCreatePatch.Parented),
                typeof(item.patches.ConsoleSpawnAutofillPatch),
                typeof(recipe.patches.RecipesSetUpRecipesPatch),
                typeof(recipe.patches.RecipeResultSpritePatch),
                typeof(i18n.LocalePatches),

                typeof(save.patches.SaveGamePatch),
                typeof(save.patches.TryLoadGamePatch),
            };

            foreach (var t in patchTypes)
            {
                string name = t.Name;
                try
                {
                    _harmony.PatchAll(t);
                    PatchStatus[name] = true;
                }
                catch (System.Exception ex)
                {
                    PatchStatus[name] = false;
                    PatchErrors[name] = ex.Message;
                    Log.LogError(
                        $"[ScavLib] Patch '{name}' failed to apply: {ex.Message}. " +
                        $"Dependent functionality will be disabled, but the rest of " +
                        $"ScavLib will continue to operate.");
                }
            }
        }

        private void SpawnGuiHosts()
        {
            try
            {
                ImguiMenuRenderer.EnsureSpawned();
                PatchStatus["ImguiHost"] = true;
            }
            catch (System.Exception ex)
            {
                PatchStatus["ImguiHost"] = false;
                PatchErrors["ImguiHost"] = ex.Message;
                Log.LogError($"[ScavLib] Failed to spawn IMGUI host: {ex.Message}.");
            }

            try
            {
                UguiHost.EnsureSpawned();
                PatchStatus["UguiHost"] = true;
            }
            catch (System.Exception ex)
            {
                PatchStatus["UguiHost"] = false;
                PatchErrors["UguiHost"] = ex.Message;
                Log.LogError($"[ScavLib] Failed to spawn uGUI host: {ex.Message}.");
            }
        }
    }
}
