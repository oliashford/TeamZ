using TeamZ.Characters.Player;
using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Handles player aiming state, feeds camera and animator and exposes an aim ray
    /// based on the current <see cref="CameraController"/>.
    /// This is the first step toward a Division-style aiming stack.
    /// </summary>
    [DisallowMultipleComponent]
    public class AimComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerContext _playerContext;

        [Tooltip("Optional explicit animator reference. If null, uses PlayerContext.Animator.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Maximum distance of the aim ray used for hit tests.")]
        [SerializeField] private float _maxAimDistance = 100f;

        private bool _isAiming;

        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int AimYawHash = Animator.StringToHash("AimYaw");
        private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");

        public bool isAiming => _isAiming;

        private void Awake()
        {
            if (_playerContext == null)
            {
                _playerContext = GetComponent<PlayerContext>();
            }

            if (_animator == null && _playerContext != null)
            {
                _animator = _playerContext.Animator;
            }
        }

        public void SetAiming(bool aiming)
        {
            _isAiming = aiming;

            if (_animator != null)
            {
                _animator.SetBool(IsAimingHash, _isAiming);
            }
        }

        /// <summary>
        /// Returns a world-space ray starting at the camera position and pointing
        /// along the camera forward vector.
        /// </summary>
        public Ray GetAimRay()
        {
            if (_playerContext == null || _playerContext.CameraController == null)
            {
                return new Ray(transform.position + transform.forward, transform.forward);
            }

            Vector3 origin = _playerContext.CameraController.GetCameraPosition();
            Vector3 dir = _playerContext.CameraController.GetCameraForward();
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
            }

            return new Ray(origin, dir.normalized);
        }

        public Vector3 GetAimPoint(float maxDistance)
        {
            Ray ray = GetAimRay();

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * maxDistance;
        }

        private void Update()
        {
            if (_animator == null || _playerContext == null || _playerContext.CameraController == null)
            {
                return;
            }

            Vector3 aimDir = _playerContext.CameraController.GetCameraForward();
            Vector3 flatAim = Vector3.ProjectOnPlane(aimDir, Vector3.up);
            if (flatAim.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 characterForward = transform.forward;
            Vector3 characterRight = transform.right;

            float yaw = Vector3.SignedAngle(characterForward, flatAim, Vector3.up);
            float pitch = Vector3.SignedAngle(flatAim, aimDir, characterRight);

            _animator.SetFloat(AimYawHash, yaw);
            _animator.SetFloat(AimPitchHash, pitch);
        }   
    }
}
