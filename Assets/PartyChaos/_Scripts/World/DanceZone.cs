using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PartyChaos.Player;
using PartyChaos.Game;

namespace PartyChaos.World
{
    [RequireComponent(typeof(Collider))]
    public class DanceZone : NetworkBehaviour
    {
        public int partyXpPerTick = 1;
        public int drunkPerTick = 1; // keep small
        public float tickInterval = 1f;

        private float _t;
        private readonly HashSet<DrunkController> _inside = new();

        private void OnTriggerEnter(Collider other)
        {
            var drunk = other.GetComponentInParent<DrunkController>();
            if (drunk != null) _inside.Add(drunk);
        }

        private void OnTriggerExit(Collider other)
        {
            var drunk = other.GetComponentInParent<DrunkController>();
            if (drunk != null) _inside.Remove(drunk);
        }

        private void Update()
        {
            if (!IsServer) return;
            if (GameSessionManager.I == null || GameSessionManager.I.State.Value != SessionState.Playing) return;

            _t += Time.deltaTime;
            if (_t < tickInterval) return;
            _t = 0f;

            foreach (var drunk in _inside)
            {
                if (drunk == null) continue;
                var party = drunk.GetComponent<PartyLevelController>();
                if (party == null) continue;

                party.AddPartyXpServerRpc(partyXpPerTick);
                drunk.AddDrunkServerRpc(drunkPerTick);

                // optional small points for dancing
                ScoreManager.I.AddPoints(drunk.OwnerClientId, 1);
            }
        }
    }
}
