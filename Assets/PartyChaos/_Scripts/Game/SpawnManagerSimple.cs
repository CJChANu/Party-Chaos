using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Networking
{
    public class SpawnManagerSimple : MonoBehaviour
    {
        [Header("Assign scene spawn point transforms here")]
        public List<Transform> spawnPoints = new List<Transform>();

        private int _nextIndex;

        private void Awake()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SpawnManagerSimple] No NetworkManager.Singleton found in scene.");
                enabled = false;
                return;
            }

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnServerStarted()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Host is already connected when server starts
            PlacePlayer(NetworkManager.Singleton.LocalClientId);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            PlacePlayer(clientId);
        }

        private void PlacePlayer(ulong clientId)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("[SpawnManagerSimple] No spawn points assigned.");
                return;
            }

            var nm = NetworkManager.Singleton;
            var playerObj = nm.ConnectedClients[clientId].PlayerObject;

            if (playerObj == null)
            {
                // Sometimes PlayerObject is not ready the same frame
                StartCoroutine(WaitAndPlace(clientId));
                return;
            }

            Transform sp = spawnPoints[_nextIndex % spawnPoints.Count];
            _nextIndex++;

            ApplySpawn(playerObj, sp.position, sp.rotation);

            Debug.Log($"[SpawnManagerSimple] Placed client {clientId} at {sp.name} pos={sp.position}");
        }

        private System.Collections.IEnumerator WaitAndPlace(ulong clientId)
        {
            var nm = NetworkManager.Singleton;

            float t = 0f;
            while (t < 2f)
            {
                if (!nm.IsServer) yield break;

                if (nm.ConnectedClients.TryGetValue(clientId, out var cc) && cc.PlayerObject != null)
                {
                    Transform sp = spawnPoints[_nextIndex % spawnPoints.Count];
                    _nextIndex++;

                    ApplySpawn(cc.PlayerObject, sp.position, sp.rotation);
                    Debug.Log($"[SpawnManagerSimple] (Delayed) Placed client {clientId} at {sp.name} pos={sp.position}");
                    yield break;
                }

                t += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"[SpawnManagerSimple] PlayerObject not ready for client {clientId} after waiting.");
        }

        private void ApplySpawn(NetworkObject playerObj, Vector3 pos, Quaternion rot)
        {
            // 1) Stop physics snapping back
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = pos;
                rb.rotation = rot;
                rb.Sleep();
            }

            // 2) Apply transform
            playerObj.transform.SetPositionAndRotation(pos, rot);

            // 3) NetworkTransform teleport (THIS is what replicates)
            var nt = playerObj.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(pos, rot, playerObj.transform.localScale);
            }
        }
    }
}
