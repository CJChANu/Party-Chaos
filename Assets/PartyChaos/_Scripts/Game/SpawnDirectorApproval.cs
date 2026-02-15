using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnDirectorApproval : MonoBehaviour
{
    [Header("Assign spawn point transforms here")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private NetworkManager _nm;
    private int _nextSpawnIndex = 0;

    private void Awake()
    {
        _nm = GetComponent<NetworkManager>();
        if (_nm == null)
        {
            Debug.LogError("SpawnDirectorApproval must be on the SAME GameObject as NetworkManager.");
            return;
        }

        // Register approval callback (fixes the timeout warning)
        _nm.ConnectionApprovalCallback += ApprovalCheck;
    }

    private void OnDestroy()
    {
        if (_nm != null)
            _nm.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                               NetworkManager.ConnectionApprovalResponse response)
    {
        // Always approve for now (you can add rules later)
        response.Approved = true;
        response.CreatePlayerObject = true;

        // IMPORTANT: must set Pending = false
        response.Pending = false;

        // Choose spawn point
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            // Round-robin spawn selection
            if (_nextSpawnIndex >= spawnPoints.Count) _nextSpawnIndex = 0;
            Transform sp = spawnPoints[_nextSpawnIndex++];
            if (sp != null)
            {
                pos = sp.position;
                rot = sp.rotation;
            }
        }
        else
        {
            Debug.LogWarning("No spawn points assigned. Using (0,0,0).");
        }

        // THIS is what makes the player spawn at your spawn point (not prefab position)
        response.Position = pos;
        response.Rotation = rot;

        Debug.Log($"[SpawnDirectorApproval] Approved client. Spawn at {pos}");
    }
}
