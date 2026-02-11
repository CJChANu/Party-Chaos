using Unity.Netcode;
using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public abstract class ItemEffect : NetworkBehaviour
    {
        public abstract void Activate(ulong ownerId, Vector3 forward, DrunkTier tier);
    }
}
