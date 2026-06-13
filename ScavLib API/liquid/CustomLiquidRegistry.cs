
using System;
using System.Collections.Generic;

namespace ScavLib.liquid
{

    public static class CustomLiquidRegistry
    {
        private static readonly Dictionary<string, (LiquidType type, string owner)> _pending
            = new Dictionary<string, (LiquidType, string)>(StringComparer.OrdinalIgnoreCase);

        internal static bool LiquidsInitialized { get; set; } = false;

        internal static bool TryRegister(string id, LiquidType type, string owner, out string error)
        {
            error = null;
            bool flag = string.IsNullOrEmpty(id);
            bool flag2;
            if (flag)
            {
                error = "Liquid id is null/empty.";
                flag2 = CustomLiquidRegistry.Fail(error);
            }
            else
            {
                bool flag3 = type == null;
                if (flag3)
                {
                    error = "Liquid '" + id + "' has null LiquidType.";
                    flag2 = CustomLiquidRegistry.Fail(error);
                }
                else
                {
                    ValueTuple<LiquidType, string> existing;
                    bool flag4 = CustomLiquidRegistry._pending.TryGetValue(id, out existing) && !string.Equals(existing.Item2, owner, StringComparison.OrdinalIgnoreCase);
                    if (flag4)
                    {
                        error = string.Concat(new string[] { "Liquid id '", id, "' already registered by '", existing.Item2 ?? "<unknown>", "'." });
                        flag2 = CustomLiquidRegistry.Fail(error);
                    }
                    else
                    {
                        CustomLiquidRegistry._pending[id] = new ValueTuple<LiquidType, string>(type, owner);
                        bool flag5 = CustomLiquidRegistry.LiquidsInitialized && Liquids.Registry != null;
                        if (flag5)
                        {
                            Liquids.Registry[id] = type;

                            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("KrokoshaCasualtiesMP"))
                            {
                                InvokeKrokMpLiquidSync(id);
                            }

                            ScavLibPlugin.Log.LogInfo($"[CustomLiquidRegistry] Late-registered liquid '{id}' (owner: {owner ?? "<none>"}).");
                        }
                        flag2 = true;
                    }
                }
            }
            return flag2;
        }

        private static void InvokeKrokMpLiquidSync(string id)
        {
            try
            {

                var listenerType = Type.GetType("KrokoshaCasualtiesMP.Item_SetupItems_Listener, KrokoshaCasualtiesMP");
                if (listenerType == null) return;

                var registryField = listenerType.GetField("LiquidIdRegistry", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var netIdToIdField = listenerType.GetField("LiquidNetIdToId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (registryField != null && netIdToIdField != null)
                {
                    var registry = (System.Collections.IDictionary)registryField.GetValue(null);
                    if (registry != null && !registry.Contains(id))
                    {
                        byte newNetId = (byte)registry.Count;
                        registry[id] = newNetId;

                        string[] oldArr = (string[])netIdToIdField.GetValue(null);
                        string[] newArr = new string[oldArr.Length + 1];
                        Array.Copy(oldArr, newArr, oldArr.Length);
                        newArr[newNetId] = id;
                        netIdToIdField.SetValue(null, newArr);
                    }
                }
            }
            catch (Exception ex)
            {
                ScavLibPlugin.Log.LogDebug("[ScavLib] KrokMP liquid sync bridge failed: " + ex.Message);
            }
        }

        internal static void FlushIntoRegistry()
        {
            LiquidsInitialized = true;

            if (Liquids.Registry == null) return;
            foreach (var kv in _pending)
            {
                Liquids.Registry[kv.Key] = kv.Value.type;
                ScavLibPlugin.Log.LogInfo(
                    $"[CustomLiquidRegistry] Registered liquid '{kv.Key}' " +
                    $"(owner: {kv.Value.owner ?? "<none>"}).");
            }
        }

        public static bool Contains(string id)
            => !string.IsNullOrEmpty(id) && _pending.ContainsKey(id);

        private static bool Fail(string msg)
        {
            ScavLibPlugin.Log.LogError($"[CustomLiquidRegistry] {msg}");
            return false;
        }
    }
}
