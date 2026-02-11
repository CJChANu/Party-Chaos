using Unity.Netcode;
using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.World
{
    public class DrinkStation : NetworkBehaviour
    {
        public int requiredPartyLevel = 1;
        public int drunkGain = 25;
        public int partyXpGain = 5;
        public float cooldownSeconds = 3f;

        private float _cooldownUntil;

        [ServerRpc(RequireOwnership = false)]
        public void RequestDrinkServerRpc(ulong playerId)
        {
            if (!IsServer) return;
            if (Time.time < _cooldownUntil) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            // distance validation
            float dist = Vector3.Distance(playerObj.transform.position, transform.position);
            if (dist > 3.0f) return;

            var party = playerObj.GetComponent<PartyLevelController>();
            var drunk = playerObj.GetComponent<DrunkController>();
            if (party == null || drunk == null) return;

            if (party.PartyLevel.Value < requiredPartyLevel) return;

            _cooldownUntil = Time.time + cooldownSeconds;

            party.AddPartyXpServerRpc(partyXpGain);
            drunk.AddDrunkServerRpc(drunkGain);
        }
    }
}
