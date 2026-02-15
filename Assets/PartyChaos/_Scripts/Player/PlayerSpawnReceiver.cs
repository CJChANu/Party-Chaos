using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Game
{
    public class PlayerSpawnReceiver : NetworkBehaviour
    {
        [ClientRpc]
        public void TeleportOwnerClientRpc(Vector3 pos, Quaternion rot)
        {
            if (!IsOwner) return;

            var nt = GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(pos, rot, transform.localScale);
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"PlayerSpawnReceiver: OWNER teleported to {pos}");
        }
    }
}
