using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Game
{
    public class PlayerSpawnSystem : NetworkBehaviour
    {
        [Header("Spawn Points")]
        public List<Transform> spawnPoints = new();

        [Header("Spawn Settings")]
        public bool useRandomSpawn = false;

        private int _nextIndex;

        private void Awake()
        {
            // Auto-find ALL SpawnPoint objects in scene
            var allSpawnPoints = FindObjectsOfType<SpawnPoint>();
            foreach (var sp in allSpawnPoints)
            {
                spawnPoints.Add(sp.transform);
            }

            Debug.Log($"PlayerSpawnSystem: Found {spawnPoints.Count} spawn points at:");
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Debug.Log($"  {i}: {spawnPoints[i].name} = {spawnPoints[i].position}");
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            Debug.Log("PlayerSpawnSystem: SERVER starting - will spawn all players");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Spawn all already-connected clients (including host)
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayerNow(clientId);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            Debug.Log($"PlayerSpawnSystem: New client {clientId} - spawning immediately");
            SpawnPlayerNow(clientId);
        }

        private void SpawnPlayerNow(ulong clientId)
        {
            StartCoroutine(SpawnPlayerCoroutine(clientId));
        }

        private IEnumerator SpawnPlayerCoroutine(ulong clientId)
        {
            // Wait 1 frame for PlayerObject to be created
            yield return null;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                Debug.LogWarning($"PlayerSpawnSystem: Client {clientId} not found");
                yield break;
            }

            if (client.PlayerObject == null)
            {
                Debug.LogWarning($"PlayerSpawnSystem: No PlayerObject for client {clientId}");
                yield break;
            }

            var playerObj = client.PlayerObject;
            Debug.Log($"PlayerSpawnSystem: Spawning {playerObj.name} (client {clientId})");

            TeleportPlayer(playerObj);
        }

        private void TeleportPlayer(NetworkObject playerObj)
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("PlayerSpawnSystem: NO SPAWN POINTS! Player stays at prefab position");
                return;
            }

            // Pick spawn point
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

            Debug.Log($"PlayerSpawnSystem: Moving {playerObj.name} from {playerObj.transform.position} to {targetPos}");

            // Reset physics FIRST
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = targetPos;
                rb.rotation = targetRot;
                rb.Sleep();
            }

            // Set transform
            playerObj.transform.SetPositionAndRotation(targetPos, targetRot);

            // Network sync
            var nt = playerObj.GetComponent<NetworkTransform>();
            var receiver = playerObj.GetComponent<PlayerSpawnReceiver>();

            if (nt != null)
            {
                nt.Teleport(targetPos, targetRot, playerObj.transform.localScale);
                Debug.Log($"PlayerSpawnSystem: NetworkTransform teleport to {targetPos}");
            }
            else if (receiver != null)
            {
                receiver.TeleportOwnerClientRpc(targetPos, targetRot);
                Debug.Log($"PlayerSpawnSystem: ClientRpc teleport to {targetPos}");
            }
            else
            {
                Debug.LogWarning("PlayerSpawnSystem: No NetworkTransform OR PlayerSpawnReceiver!");
            }
        }
    }
}
