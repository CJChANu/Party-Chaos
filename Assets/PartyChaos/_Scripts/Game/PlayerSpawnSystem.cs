using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Game
{
    public class PlayerSpawnSystem : MonoBehaviour
    {
        [Header("Spawn Points")]
        public List<Transform> spawnPoints = new();

        [Header("Spawn Settings")]
        public bool useRandomSpawn = false;

        private int _nextIndex;

        private void Awake()
        {
            // Auto-find ALL SpawnPoint objects in scene
            spawnPoints.Clear();
            var allSpawnPoints = FindObjectsOfType<SpawnPoint>(true);
            foreach (var sp in allSpawnPoints)
                spawnPoints.Add(sp.transform);

            Debug.Log($"PlayerSpawnSystem: Found {spawnPoints.Count} spawn points at:");
            for (int i = 0; i < spawnPoints.Count; i++)
                Debug.Log($"  {i}: {spawnPoints[i].name} = {spawnPoints[i].position}");
        }

        private void OnEnable()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnServerStarted()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            Debug.Log("PlayerSpawnSystem: Server started - placing already connected players");

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                StartCoroutine(PlaceWhenReady(clientId));
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            Debug.Log($"PlayerSpawnSystem: Client connected {clientId} - placing player");
            StartCoroutine(PlaceWhenReady(clientId));
        }

        private IEnumerator PlaceWhenReady(ulong clientId)
        {
            // Wait until player object exists
            float timeout = 3f;
            float t = 0f;

            while (t < timeout)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                    client.PlayerObject != null)
                {
                    TeleportPlayer(client.PlayerObject);
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning($"PlayerSpawnSystem: Timed out waiting for PlayerObject for client {clientId}");
        }

        private void TeleportPlayer(NetworkObject playerObj)
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("PlayerSpawnSystem: NO SPAWN POINTS! Player stays at prefab position");
                return;
            }

            Transform sp;
            if (useRandomSpawn)
                sp = spawnPoints[Random.Range(0, spawnPoints.Count)];
            else
            {
                sp = spawnPoints[_nextIndex % spawnPoints.Count];
                _nextIndex++;
            }

            Vector3 targetPos = sp.position;
            Quaternion targetRot = sp.rotation;

            Debug.Log($"PlayerSpawnSystem: Moving {playerObj.name} to {sp.name} -> {targetPos}");

            // Reset physics first
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = targetPos;
                rb.rotation = targetRot;
                rb.Sleep();
            }

            playerObj.transform.SetPositionAndRotation(targetPos, targetRot);

            // Best option: server-side NetworkTransform teleport
            var nt = playerObj.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(targetPos, targetRot, playerObj.transform.localScale);
            }
            else
            {
                // fallback: owner client rpc if you keep PlayerSpawnReceiver
                var receiver = playerObj.GetComponent<PlayerSpawnReceiver>();
                if (receiver != null)
                    receiver.TeleportOwnerClientRpc(targetPos, targetRot);
                else
                    Debug.LogWarning("PlayerSpawnSystem: No NetworkTransform or PlayerSpawnReceiver found.");
            }
        }
    }
}
