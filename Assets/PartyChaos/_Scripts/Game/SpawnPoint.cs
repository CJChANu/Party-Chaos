using UnityEngine;

namespace PartyChaos.Game
{
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        public Color gizmoColor = Color.green;
        public float gizmoRadius = 0.5f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
