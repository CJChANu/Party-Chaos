using UnityEngine;
using Unity.Netcode;

public class SpawnPositionLogger : NetworkBehaviour
{
    private Vector3 _last;

    public override void OnNetworkSpawn()
    {
        _last = transform.position;
        Debug.Log($"[SpawnLogger] Spawned at: {_last}");
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        // detect snap to near-zero (common reset)
        if (transform.position.sqrMagnitude < 0.05f && _last.sqrMagnitude > 1f)
        {
            Debug.LogError($"[SpawnLogger] SNAP TO ZERO detected! Current: {transform.position}, Prev: {_last}\n{System.Environment.StackTrace}");
        }

        if ((transform.position - _last).sqrMagnitude > 0.01f)
        {
            _last = transform.position;
            Debug.Log($"[SpawnLogger] Moved to: {_last}");
        }
    }
}
