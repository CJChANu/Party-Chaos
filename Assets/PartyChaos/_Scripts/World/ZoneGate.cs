using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.World
{
    [RequireComponent(typeof(Collider))]
    public class ZoneGate : MonoBehaviour
    {
        public int requiredPartyLevel = 2;

        private void OnTriggerStay(Collider other)
        {
            var party = other.GetComponentInParent<PartyLevelController>();
            if (party == null) return;

            if (party.PartyLevel.Value < requiredPartyLevel)
            {
                // push them back slightly (simple, fast)
                var rb = other.GetComponentInParent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (other.transform.position - transform.position).normalized;
                    rb.AddForce(dir * 10f, ForceMode.Acceleration);
                }
            }
        }
    }
}
