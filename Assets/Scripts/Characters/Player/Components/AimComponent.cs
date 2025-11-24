using System;
using TeamZ.Characters.Player;
using UnityEngine;

namespace TeamZ.Characters.Player.Components
{
    // Handles player aiming state, feeds camera and animator and exposes an aim ray
    // based on the current CameraController.
    // This is the first step toward a Division-style aiming stack.
    [RequireComponent(typeof(PlayerContext))]
    [DisallowMultipleComponent]
    public class AimComponent : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;

        [Header("Settings")]
        [SerializeField] private float _maxAimDistance = 100f;
        
        [SerializeField] private LayerMask _aimRaycastLayers = -1;

        private PlayerContext _playerContext;
        
        private bool _isAiming;
        private bool _hasValidDependencies;

        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int AimYawHash = Animator.StringToHash("AimYaw");
        private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");

        // Event triggered when aiming state changes.
        public event Action<bool> OnAimingStateChanged;

        // Returns true if the component is currently in aiming mode.
        public bool IsAiming => _isAiming;
        
        // Returns the maximum aim distance configured for this component.
        public float MaxAimDistance => _maxAimDistance;

        private void Awake()
        {
            InitializeDependencies();
        }

        private void InitializeDependencies()
        {
            _playerContext = GetComponent<PlayerContext>();
        }

        public void SetAiming(bool aiming)
        {
            if (_isAiming == aiming)
            {
                return;
            }

            _isAiming = aiming;

            if (_playerContext != null && _playerContext.animator != null)
            {
                _playerContext.animator.SetBool(IsAimingHash, _isAiming);
            }

            OnAimingStateChanged?.Invoke(_isAiming);
        }

        // Returns a world-space ray starting at the camera position and pointing along the camera forward vector.
        public Ray GetAimRay()
        {
            if (_playerContext == null)
            {
                Debug.LogError($"[AimComponent] Cannot get aim ray: PlayerContext is null on {gameObject.name}", this);
                return new Ray();
            }

            if (_playerContext.cameraController == null)
            {
                Debug.LogError($"[AimComponent] Cannot get aim ray: CameraController is null on {gameObject.name}", this);
                return new Ray();
            }

            Vector3 origin = _playerContext.cameraController.GetCameraPosition();
            Vector3 direction = _playerContext.cameraController.GetCameraForward();
            
            if (direction.sqrMagnitude < DirectionEpsilon)
            {
                Debug.LogError($"[AimComponent] Camera forward vector is invalid (near zero) on {gameObject.name}", this);
                return new Ray();
            }

            return new Ray(origin, direction.normalized);
        }

        // Returns the world position of the aim point, using the configured layer mask.
        // If no hit is detected, returns a point at max distance along the aim ray.
        public Vector3 GetAimPoint()
        {
            return GetAimPoint(_maxAimDistance);
        }

        // Returns the world position of the aim point at the specified distance.
        // If no hit is detected, returns a point at max distance along the aim ray.
        public Vector3 GetAimPoint(float maxDistance)
        {
            Ray ray = GetAimRay();

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, _aimRaycastLayers))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * maxDistance;
        }

        private void Update()
        {
            if (_playerContext == null || _playerContext.cameraController == null || _playerContext.animator == null)
            {
                return;
            }

            Vector3 aimDir = _playerContext.cameraController.GetCameraForward();
            
            Vector3 flatAim = Vector3.ProjectOnPlane(aimDir, Vector3.up);
            
            if (flatAim.sqrMagnitude < DirectionEpsilon)
            {
                return;
            }

            Vector3 characterForward = transform.forward;
            Vector3 characterRight = transform.right;

            float yaw = Vector3.SignedAngle(characterForward, flatAim, Vector3.up);
            
            float pitch = Vector3.SignedAngle(flatAim, aimDir, characterRight);

            _playerContext.animator.SetFloat(AimYawHash, yaw);
            _playerContext.animator.SetFloat(AimPitchHash, pitch);
        }
    }
}

