using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public class SlipSodaEffect : ItemEffect
    {
        public float life = 8f;

        public override void Activate(ulong ownerId, Vector3 forward, DrunkTier tier)
        {
            if (!IsServer) return;
            Destroy(gameObject, life);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsServer) return;
            var rb = other.GetComponentInParent<Rigidbody>();
            if (rb == null) return;

            // simple slip: add sideways drift
            Vector3 drift = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            rb.AddForce(drift * 2.5f, ForceMode.Acceleration);
        }
    }
}
