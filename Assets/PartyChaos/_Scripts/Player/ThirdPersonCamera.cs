using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    public class ThirdPersonCamera : NetworkBehaviour
    {
        [Header("Target")]
        public Transform followTarget; // usually the player root
        public Transform yawPivot;      // optional pivot for movement yaw (assign to MovementController.cameraYawSource)

        [Header("Orbit")]
        public float yawSpeed = 220f;
        public float pitchSpeed = 170f;
        public float minPitch = -35f;
        public float maxPitch = 70f;

        [Header("Distance")]
        public float distance = 4.0f;
        public float minDistance = 2.2f;
        public float maxDistance = 6.5f;
        public float zoomSpeed = 2.0f;

        [Header("Offsets")]
        public Vector3 shoulderOffset = new Vector3(0.5f, 1.6f, 0f);
        public float followSmooth = 18f;

        [Header("Collision")]
        public LayerMask collisionMask;
        public float collisionRadius = 0.2f;

        private Camera _cam;
        private float _yaw;
        private float _pitch;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _cam = Camera.main;
            if (_cam == null)
            {
                Debug.LogError("No Main Camera found. Tag your camera as MainCamera.");
                enabled = false;
                return;
            }

            // Initialize angles from current camera
            Vector3 e = _cam.transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x;
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (_cam == null || followTarget == null) return;

            // Mouse input (legacy). Fast to ship.
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            _yaw += mx * yawSpeed * Time.deltaTime;
            _pitch -= my * pitchSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

            // pivot position (shoulder)
            Vector3 pivotPos = followTarget.position + shoulderOffset;

            // rotation
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

            // desired camera position behind pivot
            Vector3 desiredCamPos = pivotPos - (rot * Vector3.forward) * distance;

            // collision push-in
            Vector3 finalCamPos = desiredCamPos;
            if (collisionMask.value != 0)
            {
                Vector3 dir = (desiredCamPos - pivotPos).normalized;
                float dist = Vector3.Distance(pivotPos, desiredCamPos);

                if (Physics.SphereCast(pivotPos, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    finalCamPos = pivotPos + dir * Mathf.Max(0.3f, hit.distance - 0.05f);
                }
            }

            // Smooth follow
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, finalCamPos, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
            _cam.transform.rotation = rot;

            // Optional: update yaw pivot so movement uses camera yaw
            if (yawPivot != null)
            {
                yawPivot.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
        }
    }
}
