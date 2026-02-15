using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Game
{
    public class SpawnDirector : MonoBehaviour
    {
        public List<Transform> spawnPoints = new();
        private int _nextIndex = 0;

        private void Start()
        {
            if (NetworkManager.Singleton == null) return;

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

            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                StartCoroutine(PlaceWhenReady(id));
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            StartCoroutine(PlaceWhenReady(clientId));
        }

        private IEnumerator PlaceWhenReady(ulong clientId)
        {
            // wait until PlayerObject exists
            while (NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var c) &&
                   c.PlayerObject == null)
            {
                yield return null;
            }

            if (NetworkManager.Singleton == null) yield break;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) yield break;
            if (client.PlayerObject == null) yield break;

            Place(client.PlayerObject);
        }

        private void Place(NetworkObject playerObj)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("SpawnDirector: No spawn points assigned.");
                return;
            }

            var sp = spawnPoints[_nextIndex % spawnPoints.Count];
            _nextIndex++;

            // ✅ IMPORTANT: Teleport via NetworkTransform so it doesn't snap back
            var nt = playerObj.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(sp.position, sp.rotation, playerObj.transform.localScale);
            }
            else
            {
                playerObj.transform.SetPositionAndRotation(sp.position, sp.rotation);
            }

            // reset physics
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"SpawnDirector: Placed {playerObj.name} at {sp.name}");
        }
    }
}
