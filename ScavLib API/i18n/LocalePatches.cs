using HarmonyLib;
using UnityEngine;

namespace ScavLib.i18n
{
    [HarmonyPatch]
    public static class LocalePatches
    {
        [HarmonyPatch(typeof(Locale), nameof(Locale.LoadLanguage))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix_LoadLanguage()
        {
            LocaleManager.PerformInjection();
            SyncModItemInstances();
        }

        [HarmonyPatch(typeof(Item), nameof(Item.SetupItems))]
        [HarmonyPostfix]
        public static void Postfix_SetupItems() => SyncModItemInstances();

        [HarmonyPatch(typeof(Locale), nameof(Locale.ChangeLanguage))]
        [HarmonyPostfix]
        public static void Postfix_ChangeLanguage() => SyncModItemInstances();

        public static void SyncModItemInstances()
        {
            if (Item.GlobalItems == null) return;
            foreach (string id in LocaleManager.RegisteredItemIds)
            {
                if (Item.GlobalItems.TryGetValue(id, out ItemInfo info))
                {
                    info.fullName = Locale.GetItem(id);
                }
            }
        }

        public static void Flush() => LocaleManager.PerformInjection();
    }
}
