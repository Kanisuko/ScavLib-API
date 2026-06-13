using System;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using ScavLib.item;
using UnityEngine;

namespace ScavLib.compat.krokmp
{
    [HarmonyPatch(typeof(GOSyncPacket), "Apply")]
    internal static class KrokMpGoSyncPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref GOSyncPacket __instance, string resource_stringid, uint request_response, ref SyncInfo __result)
        {

            CustomItem custom;
            if (!CustomItemRegistry.TryGet(resource_stringid, out custom))
            {
                return true;
            }

            if (request_response != 0U)
            {
                GameObject requested = NetObjectRegistry.Client_GetRequestedExistenceObjFromId(request_response);
                if (requested != null) return true;
            }
            if (__instance.GetSyncInfo() != null) return true;

            UnityEngine.Object template = PrefabTemplateCache.Resolve(custom.TemplateId);
            if (template == null)
            {
                ScavLibPlugin.Log.LogError($"[KrokMpGoSyncPatch] Failed to resolve template '{custom.TemplateId}' for item '{resource_stringid}'.");
                __result = null;
                return false;
            }

            GameObject go = UnityEngine.Object.Instantiate(template, __instance.pos, Quaternion.Euler(0f, 0f, __instance.angle)) as GameObject;
            go.transform.localScale = new Vector3(__instance.scale_x, __instance.scale_y, go.transform.localScale.z);

            Item itemComp = go.GetComponent<Item>();
            if (itemComp != null)
            {
                itemComp.id = resource_stringid;
            }

            CustomItemTag tag = go.AddComponent<CustomItemTag>();
            tag.CustomItemId = resource_stringid;
            tag.Owner = custom.Owner;
            if (custom.OnSpawn != null)
            {
                try { custom.OnSpawn(go); } catch (Exception e) { ScavLibPlugin.Log.LogError($"OnSpawn hook error: {e}"); }
            }

            SyncInfo si = NetObjectRegistry._RegisterGO(go, __instance.net_syncid);
            __instance.ApplyDirect(si);

            __result = si;
            return false;
        }
    }
}
