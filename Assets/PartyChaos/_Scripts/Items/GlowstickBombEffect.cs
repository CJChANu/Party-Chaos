using UnityEngine;
using PartyChaos.Player;

namespace PartyChaos.Items
{
    public class GlowstickBombEffect : ItemEffect
    {
        public float fuse = 1.2f;
        public float radius = 5f;
        public float force = 14f;

        public override void Activate(ulong ownerId, Vector3 forward, DrunkTier tier)
        {
            if (!IsServer) return;
            Invoke(nameof(Explode), fuse);
        }

        void Explode()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var h in hits)
            {
                var rb = h.GetComponentInParent<Rigidbody>();
                if (rb == null) continue;

                Vector3 dir = (rb.transform.position - transform.position).normalized;
                rb.AddForce((dir + Vector3.up * 0.25f) * force, ForceMode.VelocityChange);
            }
            Destroy(gameObject);
        }
    }
}
