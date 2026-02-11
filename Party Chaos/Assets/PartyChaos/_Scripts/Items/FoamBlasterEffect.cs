using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public class FoamBlasterEffect : ItemEffect
    {
        public float radius = 4f;
        public float force = 10f;
        public float life = 0.2f;

        public override void Activate(ulong ownerId, Vector3 forward, DrunkTier tier)
        {
            if (!IsServer) return;

            float f = (tier == DrunkTier.Wasted) ? force * Random.Range(0.7f, 1.5f) : force;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var h in hits)
            {
                var rb = h.GetComponentInParent<Rigidbody>();
                if (rb == null) continue;

                Vector3 dir = (rb.transform.position - transform.position).normalized;
                // only roughly forward
                if (Vector3.Dot(dir, forward) < 0.2f) continue;

                rb.AddForce((dir + Vector3.up * 0.15f) * f, ForceMode.VelocityChange);
            }

            Destroy(gameObject, life);
        }
    }
}
