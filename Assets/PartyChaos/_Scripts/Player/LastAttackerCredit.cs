using UnityEngine;

namespace PartyChaos.Player
{
    public class LastAttackerCredit : MonoBehaviour
    {
        public ulong LastAttackerId { get; private set; }
        public float ValidUntil { get; private set; }

        public void SetAttacker(ulong attackerId, float seconds = 5f)
        {
            LastAttackerId = attackerId;
            ValidUntil = Time.time + seconds;
        }

        public bool TryGetValidAttacker(out ulong attackerId)
        {
            if (Time.time <= ValidUntil && LastAttackerId != 0)
            {
                attackerId = LastAttackerId;
                return true;
            }
            attackerId = 0;
            return false;
        }
    }
}
