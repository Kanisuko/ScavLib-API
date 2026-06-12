using System;
using System.Collections.Generic;
using UnityEngine;
using ScavLib.i18n;

namespace ScavLib.item
{

    public static class CustomItemRegistry
    {

        private static readonly Dictionary<string, CustomItem> _items
            = new Dictionary<string, CustomItem>(StringComparer.OrdinalIgnoreCase);

        internal static bool ItemsInitialized { get; set; } = false;

        public static bool TryRegister(CustomItem item, out string error)
        {
            error = null;

            if (item == null)
            { error = "CustomItem is null."; Err(error); return false; }

            if (string.IsNullOrEmpty(item.Id))
            { error = "CustomItem id is null or empty."; Err(error); return false; }

            if (item.Info == null)
            { error = $"CustomItem '{item.Id}' has null ItemInfo."; Err(error); return false; }

            if (string.IsNullOrEmpty(item.TemplateId))
            { error = $"CustomItem '{item.Id}' has null/empty TemplateId."; Err(error); return false; }

            bool isIdempotentReRegister = false;

            if (_items.TryGetValue(item.Id, out var existing))
            {
                if (string.Equals(existing.Owner, item.Owner, StringComparison.OrdinalIgnoreCase))
                {
                    ScavLibPlugin.Log.LogWarning(
                        $"[CustomItemRegistry] '{item.Id}' re-registered by same owner " +
                        $"'{item.Owner}'. Overwriting previous definition (idempotent).");
                    isIdempotentReRegister = true;
                }
                else
                {
                    error = $"Item id '{item.Id}' is already registered by mod " +
                            $"'{existing.Owner ?? "<unknown>"}'. Mod '{item.Owner}' " +
                            $"cannot claim it. Use a mod-prefixed id.";
                    Err(error);
                    return false;
                }
            }

            if (!isIdempotentReRegister &&
                ItemsInitialized && Item.GlobalItems != null &&
                Item.GlobalItems.ContainsKey(item.Id))
            {
                error = $"Item id '{item.Id}' collides with a vanilla/registered " +
                        $"GlobalItems entry. Choose a unique id (prefix with your " +
                        $"mod name, e.g. '{item.Owner}_{item.Id}').";
                Err(error);
                return false;
            }

            if (!string.IsNullOrEmpty(item.Owner) &&
                !item.Id.StartsWith(item.Owner + "_", StringComparison.OrdinalIgnoreCase) &&
                !item.Id.Contains("_"))
            {
                ScavLibPlugin.Log.LogWarning(
                    $"[CustomItemRegistry] Item id '{item.Id}' has no mod prefix. " +
                    $"Convention is '<modname>_<itemid>' to avoid cross-mod collisions.");
            }

            _items[item.Id] = item;

            LocaleManager.RegisterItem(item.Id, item.DisplayNames, item.Descriptions);

            if (ItemsInitialized && Item.GlobalItems != null)
            {
                Item.GlobalItems[item.Id] = item.Info;
                try
                {
                    item.Info.SetTags();
                }
                catch (Exception ex)
                {
                    ScavLibPlugin.Log.LogError(
                        $"[CustomItemRegistry] SetTags() failed for late-registered " +
                        $"'{item.Id}': {ex}");
                }
                try
                {
                    ItemLootPool.InitializePool();
                }
                catch (Exception ex)
                {
                    ScavLibPlugin.Log.LogError(
                        $"[CustomItemRegistry] ItemLootPool rebuild failed after " +
                        $"late-registration of '{item.Id}': {ex}");
                }
            }

            ScavLibPlugin.Log.LogInfo(
                $"[CustomItemRegistry] {(isIdempotentReRegister ? "Re-registered" : "Registered")} " +
                $"item '{item.Id}' (owner: {item.Owner ?? "<none>"}, template: {item.TemplateId}).");
            return true;
        }

        public static bool Contains(string id)
            => !string.IsNullOrEmpty(id) && _items.ContainsKey(id);

        public static bool TryGet(string id, out CustomItem item)
            => _items.TryGetValue(id ?? "", out item);

        public static string GetOwner(string id)
            => _items.TryGetValue(id ?? "", out var i) ? i.Owner : null;

        public static IReadOnlyList<(string id, string owner)> GetAllRegistered()
        {
            var list = new List<(string, string)>(_items.Count);
            foreach (var kv in _items) list.Add((kv.Key, kv.Value.Owner));
            return list;
        }

        public static IEnumerable<string> GetAllIds() => new List<string>(_items.Keys);

        internal static void FlushIntoGlobalItems()
        {
            ItemsInitialized = true;

            if (Item.GlobalItems == null)
            {
                ScavLibPlugin.Log.LogError(
                    "[CustomItemRegistry] Item.GlobalItems is null; cannot inject.");
                return;
            }

            foreach (var kv in _items)
            {
                if (Item.GlobalItems.ContainsKey(kv.Key))
                {
                    Item.GlobalItems[kv.Key] = kv.Value.Info;
                    ScavLibPlugin.Log.LogInfo(
                        $"[CustomItemRegistry] Overwrote GlobalItems['{kv.Key}'].");
                }
                else
                {
                    Item.GlobalItems.Add(kv.Key, kv.Value.Info);
                }
            }

            int tagSetupOk = 0;
            int tagSetupFail = 0;
            foreach (var kv in _items)
            {
                try
                {
                    kv.Value.Info.SetTags();
                    tagSetupOk++;
                }
                catch (Exception ex)
                {
                    tagSetupFail++;
                    ScavLibPlugin.Log.LogError(
                        $"[CustomItemRegistry] SetTags() failed for '{kv.Key}': {ex}");
                }
            }

            ScavLibPlugin.Log.LogInfo(
                $"[CustomItemRegistry] Injected {_items.Count} custom item(s) " +
                $"into GlobalItems. SetTags: {tagSetupOk} ok, {tagSetupFail} failed.");
        }

        [Obsolete("Use CustomItemBuilder. This overload only registers a definition " +
                  "and cannot spawn an instance (no template/sprite).")]
        public static void RegisterItem(string id, ItemInfo info, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(id) || info == null)
            {
                ScavLibPlugin.Log.LogError(
                    "[CustomItemRegistry] Legacy RegisterItem called with null id/info.");
                return;
            }

            var legacy = new CustomItem(
                id, null, info, null,
                ItemTemplate.SimpleItem.ToResourceId(),
                null, null, null, null);

            if (overwrite && _items.ContainsKey(id)) _items[id] = legacy;
            else TryRegister(legacy, out _);
        }

        private static void Err(string msg) => ScavLibPlugin.Log.LogError($"[CustomItemRegistry] {msg}");
    }
}
