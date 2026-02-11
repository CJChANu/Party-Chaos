using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public enum ItemType { FoamBlaster, GlowstickBomb, SlipSoda }

    [CreateAssetMenu(menuName = "PartyChaos/ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        public ItemType itemType;
        public DrunkTier requiredTier = DrunkTier.Buzzed;
        public float cooldown = 6f;
        public GameObject networkedPrefab; // must have NetworkObject
    }
}
