using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ScavLib.item;

namespace ScavLib.item.patches
{
    [HarmonyPatch(typeof(PlayerCamera), "RefreshTraderInventories")]
    internal static class TraderUiSpritePatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {

            foreach (Transform child in __instance.traderInventory)
            {

                if (child.name.Contains("TraderItemPanel"))
                {

                    var iconImage = child.GetChild(1).GetComponent<Image>();
                    if (iconImage != null)
                    {

                    }
                }
            }

            if (__instance.currentTrader == null) return;

            int panelIdx = 0;
            for (int i = 0; i < __instance.currentTrader.items.Count; i++)
            {
                var item = __instance.currentTrader.items[i];

                if (__instance.currentTrader.collapsedCategories.Contains(item.preference)) continue;

                if (CustomItemRegistry.TryGet(item.id, out var custom))
                {

                    var panel = __instance.traderInventory.GetChild(panelIdx);
                    var img = panel.GetChild(1).GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = custom.Sprite;
                        img.rectTransform.sizeDelta = PlayerCamera.ImageSizeDelta(custom.Sprite.texture, 8f, 64f);
                    }
                }
                panelIdx++;
            }
        }
    }
}
