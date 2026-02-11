using Unity.Netcode;
using UnityEngine;
using PartyChaos.Game;

namespace PartyChaos.World
{
    [RequireComponent(typeof(Collider))]
    public class EliminationVolume : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsServer) enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;

            // eliminate only players
            if (!netObj.IsPlayerObject) return;

            // notify round manager
            var rm = FindAnyObjectByType<RoundManager>();
            if (rm != null)
                rm.NotifyEliminated(netObj.OwnerClientId);

            // switch eliminated player to spectator
            var spectator = netObj.GetComponentInParent<PartyChaos.Player.SpectatorController>();
            if (spectator != null)
                spectator.SetSpectatorServerRpc(true);
        }
    }
}
