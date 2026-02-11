using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    public class GrabThrowController : NetworkBehaviour
    {
        public Transform carrySocket;
        public float throwForce = 14f;

        private NetworkObject _carried;
        private DrunkController _drunk;

        void Awake()
        {
            _drunk = GetComponent<DrunkController>();
        }

        public bool IsCarrying => _carried != null;

        [ServerRpc]
        public void TryGrabServerRpc(ulong targetNetId)
        {
            if (!IsServer) return;
            if (_drunk == null || !_drunk.CanGrabThrow()) return;
            if (_carried != null) return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var target))
                return;

            if (!target.IsPlayerObject) return;
            if (target.OwnerClientId == OwnerClientId) return;

            // distance check
            if (Vector3.Distance(transform.position, target.transform.position) > 2.0f) return;

            _carried = target;

            // disable target movement quick (your movement script should respect this)
            var targetRb = target.GetComponentInParent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.isKinematic = true;
                target.transform.position = carrySocket.position;
                target.transform.SetParent(carrySocket, true);
            }

            // attacker credit stamp
            var credit = target.GetComponentInParent<LastAttackerCredit>();
            if (credit != null) credit.SetAttacker(OwnerClientId);
        }

        [ServerRpc]
        public void ThrowServerRpc()
        {
            if (!IsServer) return;
            if (_carried == null) return;

            var target = _carried;
            _carried = null;

            target.transform.SetParent(null, true);

            var rb = target.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;

                // variance if wasted
                float variance = (_drunk != null && _drunk.Tier == DrunkTier.Wasted) ? Random.Range(0.6f, 1.4f) : 1f;
                Vector3 dir = (transform.forward + Vector3.up * 0.35f).normalized;
                rb.AddForce(dir * throwForce * variance, ForceMode.VelocityChange);
            }
        }
    }
}
