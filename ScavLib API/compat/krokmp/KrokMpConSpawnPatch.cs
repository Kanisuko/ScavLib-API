using System.Linq;
using HarmonyLib;
using UnityEngine;
using KrokoshaCasualtiesMP;
using ScavLib.item;

namespace ScavLib.compat.krokmp
{
    [HarmonyPatch(typeof(Con), nameof(Con.SpawnThingOnPlayer))]
    internal static class KrokMpConSpawnPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string resourceid, Body body, bool give_it_to_em,
                                   GameObject container, ref GameObject __result)
        {
            if (!CustomItemRegistry.Contains(resourceid)) return true;

            if (KrokoshaScavMultiplayer.is_dedicated_server && body == null && NetPlayer.BodyToPlayerDict.Count > 0)
                body = NetPlayer.BodyToPlayerDict.First().Key;

            if (body == null) body = PlayerCamera.main.body;

            GameObject go = Utils.Create(resourceid, (Vector2)body.transform.position + Random.insideUnitCircle, 0f);

            if (go.TryGetComponent<AmmoScript>(out var amm)) amm.rounds = amm.maxRounds;
            if (go.TryGetComponent<GunScript>(out var gun))
            {
                gun.roundsInMag = gun.magCapacity;
                if (gun.feedType == GunScript.FeedType.Mag) gun.hasMag = true;
                gun.roundInChamber = GunScript.RoundInChamber.Round;
            }

            if (give_it_to_em && go.TryGetComponent<Item>(out var it))
            {
                if (container != null && container.TryGetComponent<Container>(out var c))
                    c.LoadItem(it);
                else
                    body.AutoPickUpItem(it);
            }

            __result = go;
            return false;
        }
    }
}
