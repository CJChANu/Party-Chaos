using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class MovementController : NetworkBehaviour
    {
        [Header("Speeds")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 9f;
        public float airControlMultiplier = 0.55f;

        [Header("Jump")]
        public float jumpForce = 6.5f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Slide")]
        public float slideSpeed = 12f;
        public float slideDuration = 0.55f;
        public float slideCooldown = 0.6f;

        [Header("Dive")]
        public float diveImpulse = 9.5f;
        public float diveUpImpulse = 2.0f;
        public float diveCooldown = 0.8f;

        [Header("Rotation")]
        public float turnSpeed = 14f; // higher = snappier

        [Header("Grounding")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.22f;
        public LayerMask groundMask;

        [Header("References")]
        public Transform cameraYawSource; // typically a "YawPivot" (optional). If null, uses Camera.main yaw.

        // runtime
        private Rigidbody _rb;
        private CapsuleCollider _cap;
        private bool _enabled = true;

        private Vector2 _move;
        private bool _sprintHeld;
        private bool _jumpPressed;
        private bool _slidePressed;
        private bool _divePressed;

        private float _lastGroundedTime;
        private float _lastJumpPressedTime;

        private bool _isSliding;
        private float _slideEndTime;
        private float _nextSlideTime;

        private bool _isDiving;
        private float _nextDiveTime;

        private float _defaultCapHeight;
        private Vector3 _defaultCapCenter;

        public bool IsGrounded { get; private set; }

        public override void OnNetworkSpawn()
        {
            _rb = GetComponent<Rigidbody>();
            _cap = GetComponent<CapsuleCollider>();

            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.freezeRotation = true;

            _defaultCapHeight = _cap.height;
            _defaultCapCenter = _cap.center;

            // Only owner processes input & drives movement.
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        public void SetEnabled(bool value)
        {
            _enabled = value;
            if (!_enabled)
            {
                _move = Vector2.zero;
                _sprintHeld = false;
                _jumpPressed = false;
                _slidePressed = false;
                _divePressed = false;
            }
        }

        private void Update()
        {
            if (!IsOwner || !_enabled) return;

            // Input (legacy). Fast to ship.
            // If you use the NEW Input System, you can replace these with your InputActions.
            _move.x = Input.GetAxisRaw("Horizontal");
            _move.y = Input.GetAxisRaw("Vertical");
            _sprintHeld = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpPressed = true;
                _lastJumpPressedTime = Time.time;
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
                _slidePressed = true;

            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetMouseButtonDown(1))
                _divePressed = true;
        }

        private void FixedUpdate()
        {
            if (!IsOwner || !_enabled) return;

            UpdateGrounded();

            // Slide / dive state timers
            if (_isSliding && Time.time >= _slideEndTime)
                EndSlide();

            // Buffer jump
            bool jumpBuffered = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;
            bool canCoyoteJump = (Time.time - _lastGroundedTime) <= coyoteTime;

            // Handle slide request
            if (_slidePressed)
            {
                _slidePressed = false;
                if (!_isSliding && Time.time >= _nextSlideTime && IsGrounded)
                    StartSlide();
            }

            // Handle dive request
            if (_divePressed)
            {
                _divePressed = false;
                if (!_isDiving && Time.time >= _nextDiveTime)
                    DoDive();
            }

            // Jump
            if (jumpBuffered && (IsGrounded || canCoyoteJump) && !_isSliding)
            {
                _jumpPressed = false;
                _lastJumpPressedTime = -999f;

                Vector3 v = _rb.linearVelocity;
                v.y = 0f;
                _rb.linearVelocity = v;

                _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }

            // Movement
            Vector3 wishDir = GetWishDirectionWorld(_move);
            float targetSpeed = _sprintHeld ? sprintSpeed : walkSpeed;

            if (_isSliding)
            {
                // slide is mostly forward
                Vector3 fwd = GetYawForward();
                ApplyGroundMove(fwd, slideSpeed);
                RotateTowards(fwd);
                return;
            }

            if (wishDir.sqrMagnitude > 0.0001f)
            {
                if (IsGrounded)
                    ApplyGroundMove(wishDir, targetSpeed);
                else
                    ApplyAirMove(wishDir, targetSpeed);

                RotateTowards(wishDir);
            }
            else
            {
                // tiny damp on ground when no input (keeps it controllable)
                if (IsGrounded)
                {
                    Vector3 vel = _rb.linearVelocity;
                    vel.x *= 0.92f;
                    vel.z *= 0.92f;
                    _rb.linearVelocity = vel;
                }
            }
        }

        private void UpdateGrounded()
        {
            if (groundCheck == null)
            {
                // fallback: use capsule bottom
                Vector3 p = transform.position + Vector3.down * (_cap.height * 0.5f - 0.05f);
                IsGrounded = Physics.CheckSphere(p, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                IsGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            }

            if (IsGrounded)
                _lastGroundedTime = Time.time;
        }

        private Vector3 GetWishDirectionWorld(Vector2 move)
        {
            Vector3 local = new Vector3(move.x, 0f, move.y);
            if (local.sqrMagnitude > 1f) local.Normalize();

            Vector3 fwd = GetYawForward();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x); // perpendicular on XZ

            Vector3 world = (right * local.x + fwd * local.z);
            if (world.sqrMagnitude > 1f) world.Normalize();
            return world;
        }

        private Vector3 GetYawForward()
        {
            float yaw;
            if (cameraYawSource != null) yaw = cameraYawSource.eulerAngles.y;
            else if (Camera.main != null) yaw = Camera.main.transform.eulerAngles.y;
            else yaw = transform.eulerAngles.y;

            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            return rot * Vector3.forward;
        }

        private void ApplyGroundMove(Vector3 dir, float speed)
        {
            Vector3 vel = _rb.linearVelocity;
            Vector3 desired = dir * speed;

            // keep y velocity from gravity/jumps
            Vector3 change = desired - new Vector3(vel.x, 0f, vel.z);

            // clamp acceleration per physics step
            float maxChange = 40f * Time.fixedDeltaTime;
            if (change.magnitude > maxChange) change = change.normalized * maxChange;

            _rb.AddForce(new Vector3(change.x, 0f, change.z), ForceMode.VelocityChange);
        }

        private void ApplyAirMove(Vector3 dir, float speed)
        {
            Vector3 vel = _rb.linearVelocity;
            Vector3 desired = dir * speed;

            Vector3 change = desired - new Vector3(vel.x, 0f, vel.z);
            float maxChange = (22f * airControlMultiplier) * Time.fixedDeltaTime;
            if (change.magnitude > maxChange) change = change.normalized * maxChange;

            _rb.AddForce(new Vector3(change.x, 0f, change.z), ForceMode.VelocityChange);
        }

        private void RotateTowards(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
            Quaternion newRot = Quaternion.Slerp(_rb.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime));
            _rb.MoveRotation(newRot);
        }

        private void StartSlide()
        {
            _isSliding = true;
            _slideEndTime = Time.time + slideDuration;
            _nextSlideTime = Time.time + slideCooldown;

            // Optional: make capsule shorter during slide (helps feel)
            _cap.height = _defaultCapHeight * 0.65f;
            _cap.center = _defaultCapCenter * 0.65f;

            // Small forward kick
            Vector3 fwd = GetYawForward();
            _rb.AddForce(fwd * 2.5f, ForceMode.VelocityChange);
        }

        private void EndSlide()
        {
            _isSliding = false;
            _cap.height = _defaultCapHeight;
            _cap.center = _defaultCapCenter;
        }

        private void DoDive()
        {
            _isDiving = true;
            _nextDiveTime = Time.time + diveCooldown;

            Vector3 fwd = GetYawForward();
            _rb.AddForce(fwd * diveImpulse + Vector3.up * diveUpImpulse, ForceMode.VelocityChange);

            // End "dive state" quickly; it mainly prevents spam via cooldown
            Invoke(nameof(EndDive), 0.25f);
        }

        private void EndDive()
        {
            _isDiving = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (groundCheck != null)
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            else
            {
                // approximate
                Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.9f, groundCheckRadius);
            }
        }
    }
}
