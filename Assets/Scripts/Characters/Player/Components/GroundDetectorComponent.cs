using UnityEngine;

namespace TeamZ.Characters.Player.Components
{
    /// <summary>
    /// Reusable grounded, incline and ceiling check component for any character.
    /// Mirrors the legacy PlayerAnimationController grounded/incline/ceiling checks.
    /// </summary>
    public class GroundDetectorComponent : MonoBehaviour
    {
        [Tooltip("Radius of the ground check sphere (should match CharacterController radius)")]
        [SerializeField]
        private float _groundCheckRadius = 0.25f;

        [Tooltip("Vertical offset from character pivot to ground test position")]
        [SerializeField]
        private float _groundCheckOffset = 0.2f;

        [SerializeField]
        private LayerMask _groundMask;

        [Header("Incline / Ceiling")]
        [Tooltip("Rear ray origin for incline calculation")]
        [SerializeField]
        private Transform _rearRayPos;

        [Tooltip("Front ray origin for incline and ceiling detection")]
        [SerializeField]
        private Transform _frontRayPos;

        [Tooltip("Standing capsule height for ceiling headroom checks")]
        [SerializeField]
        private float _capsuleStandingHeight = 1.8f;

        /// <summary>True when the character is currently grounded.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Smoothed incline angle under the character in degrees.</summary>
        public float InclineAngle { get; private set; }

        /// <summary>True when there is not enough headroom to stand up from crouch.</summary>
        public bool CannotStandUp { get; private set; }

        // Expose configuration so PlayerContext can mirror legacy controller settings.
        public LayerMask GroundMask
        {
            get => _groundMask;
            set => _groundMask = value;
        }

        public Transform RearRayPos
        {
            get => _rearRayPos;
            set => _rearRayPos = value;
        }

        public Transform FrontRayPos
        {
            get => _frontRayPos;
            set => _frontRayPos = value;
        }

        public float CapsuleStandingHeight
        {
            get => _capsuleStandingHeight;
            set => _capsuleStandingHeight = value;
        }

        private void Update()
        {
            Vector3 origin = transform.position;
            origin.y -= _groundCheckOffset;

            IsGrounded = Physics.CheckSphere(origin, _groundCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                UpdateIncline();
            }

            UpdateCeiling();
        }

        private void UpdateIncline()
        {
            if (_rearRayPos == null || _frontRayPos == null)
            {
                InclineAngle = 0f;
                return;
            }

            float rayDistance = Mathf.Infinity;

            _rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0f, 0f);
            _frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0f, 0f);

            if (!Physics.Raycast(_rearRayPos.position, _rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, _groundMask))
            {
                return;
            }

            if (!Physics.Raycast(_frontRayPos.position, _frontRayPos.TransformDirection(-Vector3.up), out RaycastHit frontHit, rayDistance, _groundMask))
            {
                return;
            }

            Vector3 hitDifference = frontHit.point - rearHit.point;
            
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            float targetAngle = Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg;
            
            InclineAngle = Mathf.Lerp(InclineAngle, targetAngle, 20f * Time.deltaTime);
        }

        private void UpdateCeiling()
        {
            if (_frontRayPos == null)
            {
                CannotStandUp = false;
                return;
            }

            float rayDistance = Mathf.Infinity;
            
            float minimumStandingHeight = _capsuleStandingHeight - _frontRayPos.localPosition.y;

            Vector3 midpoint = new Vector3(transform.position.x, transform.position.y + _frontRayPos.localPosition.y, transform.position.z);
            
            if (Physics.Raycast(midpoint, transform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, _groundMask))
            {
                CannotStandUp = ceilingHit.distance < minimumStandingHeight;
            }
            else
            {
                CannotStandUp = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            
            Vector3 origin = transform.position;
            
            origin.y -= _groundCheckOffset;
            
            Gizmos.DrawWireSphere(origin, _groundCheckRadius);
        }
    }
}
