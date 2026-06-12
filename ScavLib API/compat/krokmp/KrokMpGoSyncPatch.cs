using HarmonyLib;
using UnityEngine;
using KrokoshaCasualtiesMP;
using ScavLib.item;

namespace ScavLib.compat.krokmp
{
    [HarmonyPatch(typeof(GOSyncPacket), nameof(GOSyncPacket.Apply))]
    internal static class KrokMpGoSyncPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref GOSyncPacket __instance, string resource_stringid,
                                   uint request_response, ref SyncInfo __result)
        {

            if (!CustomItemRegistry.Contains(resource_stringid)) return true;

            GameObject go = Utils.Create(resource_stringid, __instance.pos, 0f);
            if (go == null)
            {
                __result = null;
                return false;
            }

            SyncInfo si = NetObjectRegistry._RegisterGO(go, __instance.net_syncid);

            __instance.ApplyDirect(si);

            __result = si;
            return false;
        }
    }
}
