using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PartyChaos.Game
{
    public class SpawnManager : NetworkBehaviour
    {
        [Tooltip("Assign spawn points in the scene")]
        public List<Transform> spawnPoints = new();

        private int _nextIndex;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            Debug.Log("SpawnManager: OnNetworkSpawn (SERVER) ✅");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Host already has a client id, so we also handle already-connected clients.
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                StartCoroutine(RespawnWhenReady(id));
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            StartCoroutine(RespawnWhenReady(clientId));
        }

        private IEnumerator RespawnWhenReady(ulong clientId)
        {
            while (NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var c) &&
                   c.PlayerObject == null)
            {
                yield return null;
            }

            if (NetworkManager.Singleton == null) yield break;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) yield break;
            if (client.PlayerObject == null) yield break;

            PlacePlayer(client.PlayerObject);
        }

        private void PlacePlayer(NetworkObject playerObj)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogError("SpawnManager: No spawn points assigned.");
                return;
            }

            var sp = spawnPoints[_nextIndex % spawnPoints.Count];
            _nextIndex++;

            var receiver = playerObj.GetComponent<PlayerSpawnReceiver>();
            if (receiver == null)
            {
                Debug.LogError("SpawnManager: PlayerSpawnReceiver missing on Player prefab!");
                return;
            }

            // Decide based on NetworkTransform authority
            var nt = playerObj.GetComponent<NetworkTransform>();

            if (nt != null && nt.IsServerAuthoritative())
            {
                // Server authoritative: server teleports directly
                nt.Teleport(sp.position, sp.rotation, playerObj.transform.localScale);
                Debug.Log($"SpawnManager: SERVER teleported {playerObj.name} to {sp.name} -> {sp.position}");
            }
            else
            {
                // Owner authoritative: ask owner to teleport itself
                receiver.TeleportOwnerClientRpc(sp.position, sp.rotation);
                Debug.Log($"SpawnManager: OWNER teleport request for {playerObj.name} to {sp.name} -> {sp.position}");
            }
        }
    }
}
