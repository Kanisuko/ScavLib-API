using HarmonyLib;
using KrokoshaCasualtiesMP;
using ScavLib.item;
using UnityEngine;

namespace ScavLib.compat.krokmp
{
    [HarmonyPatch(typeof(NetObjectRegistry), nameof(NetObjectRegistry.ObjectCanBeIgnoredForNetwork))]
    internal static class KrokMpValidationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameObject obj, ref bool __result)
        {

            if (obj != null && obj.TryGetComponent<CustomItemTag>(out _))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
