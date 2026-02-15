using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Game
{
    public class SpawnManagerManual : MonoBehaviour
    {
        [Header("Assign SAME prefab as NetworkManager 'Default Player Prefab'")]
        [SerializeField] private GameObject playerPrefab;

        [Header("Spawn Points in the scene (drag here)")]
        [SerializeField] private List<Transform> spawnPoints = new();

        private int _nextIndex;

        private NetworkManager _nm;

        private void Awake()
        {
            _nm = NetworkManager.Singleton;
            if (_nm == null)
            {
                Debug.LogError("SpawnManagerManual: NetworkManager.Singleton is null (is NetworkManager in the scene?)");
                return;
            }

            // Force-enable approval in case you forgot the checkbox.
            _nm.NetworkConfig.ConnectionApproval = true;

            // Optional safety: keep these in sync.
            if (playerPrefab != null)
                _nm.NetworkConfig.PlayerPrefab = playerPrefab;

            _nm.ConnectionApprovalCallback += ApprovalCheck;
            _nm.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDestroy()
        {
            if (_nm == null) return;
            _nm.ConnectionApprovalCallback -= ApprovalCheck;
            _nm.OnClientConnectedCallback -= OnClientConnected;
        }

        // SERVER: approve and PREVENT Netcode from auto-creating the player at prefab position
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                   NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;

            // This is the key line:
            response.CreatePlayerObject = false;

            response.Pending = false;
        }

        // SERVER: create the player ourselves at a spawn point
        private void OnClientConnected(ulong clientId)
        {
            if (_nm == null || !_nm.IsServer) return;

            if (playerPrefab == null)
            {
                Debug.LogError("SpawnManagerManual: playerPrefab not assigned!");
                return;
            }

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogError("SpawnManagerManual: No spawnPoints assigned!");
                return;
            }

            // If Netcode already created a PlayerObject (because approval wasn't enabled earlier),
            // remove it so we don't keep the prefab-default position.
            if (_nm.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var existing = client.PlayerObject;
                Debug.LogWarning($"SpawnManagerManual: Existing PlayerObject found for {clientId}. Despawning it and respawning correctly.");

                existing.Despawn(true);
                Destroy(existing.gameObject);
            }

            Transform sp = spawnPoints[_nextIndex % spawnPoints.Count];
            _nextIndex++;

            GameObject go = Instantiate(playerPrefab, sp.position, sp.rotation);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("SpawnManagerManual: Player prefab has NO NetworkObject component!");
                Destroy(go);
                return;
            }

            // This spawns at the correct position on the network.
            netObj.SpawnAsPlayerObject(clientId, true);

            Debug.Log($"SpawnManagerManual: Spawned client {clientId} at {sp.name} -> {sp.position}");
        }
    }
}
