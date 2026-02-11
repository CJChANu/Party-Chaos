using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    public enum DrunkTier { Sober, Buzzed, Drunk, Wasted }

    public class DrunkController : NetworkBehaviour
    {
        public NetworkVariable<int> DrunkValue = new(0); // 0-100

        [Header("Tier thresholds")]
        public int buzzed = 20;
        public int drunk = 50;
        public int wasted = 80;

        public DrunkTier Tier
        {
            get
            {
                int v = DrunkValue.Value;
                if (v < buzzed) return DrunkTier.Sober;
                if (v < drunk) return DrunkTier.Buzzed;
                if (v < wasted) return DrunkTier.Drunk;
                return DrunkTier.Wasted;
            }
        }

        [ServerRpc]
        public void AddDrunkServerRpc(int amount)
        {
            DrunkValue.Value = Mathf.Clamp(DrunkValue.Value + amount, 0, 100);
        }

        // called by server-side controllers (combat/inventory) for validation
        public bool CanDisturb() => Tier >= DrunkTier.Buzzed;
        public bool CanGrabThrow() => Tier >= DrunkTier.Drunk;
    }
}
