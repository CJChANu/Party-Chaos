using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    public class PartyLevelController : NetworkBehaviour
    {
        public NetworkVariable<int> PartyXP = new(0);
        public NetworkVariable<int> PartyLevel = new(1);

        [Header("Thresholds")]
        public int level2XP = 30;
        public int level3XP = 70;

        [ServerRpc]
        public void AddPartyXpServerRpc(int amount)
        {
            PartyXP.Value = Mathf.Max(0, PartyXP.Value + amount);

            int newLevel = 1;
            if (PartyXP.Value >= level3XP) newLevel = 3;
            else if (PartyXP.Value >= level2XP) newLevel = 2;

            PartyLevel.Value = newLevel;
        }
    }
}
