using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public class InventoryController : NetworkBehaviour
    {
        public List<ItemDefinition> startingItems;
        public NetworkVariable<int> ActiveSlot = new(0);

        private readonly Dictionary<ItemType, float> _cooldowns = new();
        private DrunkController _drunk;

        void Awake()
        {
            _drunk = GetComponent<DrunkController>();
        }

        [ServerRpc]
        public void UseActiveItemServerRpc()
        {
            if (!IsServer) return;
            if (startingItems == null || startingItems.Count == 0) return;
            int idx = Mathf.Clamp(ActiveSlot.Value, 0, startingItems.Count - 1);

            var def = startingItems[idx];
            if (_drunk == null) return;
            if (_drunk.Tier < def.requiredTier) return;

            if (_cooldowns.TryGetValue(def.itemType, out float until) && Time.time < until) return;
            _cooldowns[def.itemType] = Time.time + def.cooldown;

            // spawn item effect
            var go = Instantiate(def.networkedPrefab, transform.position + transform.forward * 1.2f + Vector3.up * 1.0f, Quaternion.identity);
            var net = go.GetComponent<NetworkObject>();
            net.Spawn(true);

            var effect = go.GetComponent<ItemEffect>();
            if (effect != null)
                effect.Activate(OwnerClientId, transform.forward, _drunk.Tier);
        }

        [ServerRpc]
        public void SetActiveSlotServerRpc(int slot)
        {
            ActiveSlot.Value = slot;
        }
    }
}
