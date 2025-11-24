using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(InputReader))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ClimbDetectorComponent))]
    [RequireComponent(typeof(GroundDetectorComponent))]
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(AimComponent))]
    [RequireComponent(typeof(WeaponHandlerComponent))]
    [RequireComponent(typeof(PlayerAdditiveRotationsComponent))]
    public class PlayerContext : MonoBehaviour, ICharacterContext
    {
        [Header("Player Configuration")]
        [Tooltip("Reference to the PlayerConfig ScriptableObject. This must be assigned in the inspector.")]
        [SerializeField] private PlayerConfig _playerConfig;
        
        public PlayerConfig playerConfig => _playerConfig;
        
        [Header("Camera")]
        [Tooltip("Reference to the CameraController. This must be assigned in the inspector.")]
        [SerializeField] private CameraController _cameraController;

        public CameraController cameraController => _cameraController;

        // Components

        private Animator _animator;
        private InputReader _inputReader;
        private CharacterController _characterController;
        private ClimbDetectorComponent _climbDetector;
        private GroundDetectorComponent _groundDetector;
        private MovementComponent _movement;
        private AimComponent _aim;
        private WeaponHandlerComponent _weaponHandler;
        private PlayerAdditiveRotationsComponent _playerAdditiveRotations;

        public Animator animator => _animator;
        public InputReader inputReader => _inputReader;
        public CharacterController characterController => _characterController;
        public ClimbDetectorComponent climbDetector => _climbDetector;
        public GroundDetectorComponent groundDetector => _groundDetector;
        public MovementComponent movement => _movement;
        public AimComponent aim => _aim;
        public WeaponHandlerComponent weaponHandler => _weaponHandler;
        public PlayerAdditiveRotationsComponent playerAdditiveRotations => _playerAdditiveRotations;

        // Flags

        private bool _isAiming;
        private bool _isCrouching;
        private bool _isSliding;
        private bool _isWalking;
        private bool _isSprinting;

        public bool IsAiming
        {
            get => _isAiming;
            set => _isAiming = value;
        }

        public bool IsCrouching
        {
            get => _isCrouching;
            set => _isCrouching = value;
        }

        public bool IsSliding
        {
            get => _isSliding;
            set => _isSliding = value;
        }

        public bool IsWalking
        {
            get => _isWalking;
            set => _isWalking = value;
        }

        public bool IsSprinting
        {
            get => _isSprinting;
            set => _isSprinting = value;
        }

        public bool IsGrounded
        {
            get
            {
                return _groundDetector.IsGrounded;
            }
        }

        // State Machines
        
        public CharacterStateMachine CombatStateMachine { get; private set; }
        public CharacterStateMachine PostureStateMachine { get; private set; }
        public CharacterStateMachine CoverStateMachine { get; private set; }
        public CharacterStateMachine InteractionStateMachine { get; private set; }
        public CharacterStateMachine MovementStateMachine { get; private set; }
        
        // Methods

        private void Awake()
        {
            #region GetComponentDependencies

            if (_cameraController == null)
            {
                Debug.LogError("PlayerContext requires a CameraController component.");
            }
            
            if (!TryGetComponent(out _animator))
            {
                Debug.LogError("PlayerContext requires an Animator component.");
            }
            
            if (!TryGetComponent(out _inputReader))
            {
                Debug.LogError("PlayerContext requires an InputReader component.");
            }
            
            if (!TryGetComponent(out _characterController))
            {
                Debug.LogError("PlayerContext requires a CharacterController component.");
            }
            
            if (!TryGetComponent(out _climbDetector))
            {                
                Debug.LogError("PlayerContext requires a ClimbDetectorComponent component.");
            }
            
            if (!TryGetComponent(out _groundDetector))
            {
                Debug.LogError("PlayerContext requires a GroundDetectorComponent component.");
            }
            
            if (!TryGetComponent(out _movement))
            {
                Debug.LogError("PlayerContext requires a MovementComponent component.");
            }
            
            if (!TryGetComponent(out _aim))
            {
                Debug.LogError("PlayerContext requires an AimComponent component.");
            }
            
            if (!TryGetComponent(out _weaponHandler))
            {
                Debug.LogError("PlayerContext requires a WeaponHandlerComponent component.");
            }
            
            if (!TryGetComponent(out _playerAdditiveRotations))
            {
                Debug.LogError("PlayerContext requires a PlayerAdditiveRotationsComponent component.");
            }
            
            #endregion

            // Initialize state machines
            CombatStateMachine = new CharacterStateMachine();
            PostureStateMachine = new CharacterStateMachine();
            CoverStateMachine = new CharacterStateMachine();
            InteractionStateMachine = new CharacterStateMachine();
            MovementStateMachine = new CharacterStateMachine();
        }
        
        // Helpers
        
        public Transform Transform => transform;
        
        
        
        
        
        
        
        

        public Vector3 Velocity
        {
            get
            {
                if (_movement == null)
                {
                    return Vector3.zero;
                }
                
                return _movement.Velocity;
                
            }
            set
            {
                _movement.Velocity = value;
            }
        }

        /// <summary>Incline angle from GroundDetectorComponent, or 0 if none present.</summary>
        public float InclineAngle => _groundDetector != null ? _groundDetector.InclineAngle : 0f;

        /// <summary>True when there is not enough headroom to stand (ceiling check).</summary>
        public bool CannotStandUp =>  _groundDetector != null && _groundDetector.CannotStandUp;

        public float CurrentSpeed => new Vector3(Velocity.x, 0f, Velocity.z).magnitude;

        public Vector3 MoveInputWorld
        {
            get
            {
                if (_inputReader == null || _cameraController == null)
                {
                    return Vector3.zero;
                }

                Vector2 move = _inputReader._moveComposite;
                Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
                Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
                return camFwd * move.y + camRight * move.x;
            }
        }

        // Simple movement lock system: sources can request a lock; movement is considered locked
        // if there is at least one active source. This keeps it simple but deterministic.
        private readonly System.Collections.Generic.HashSet<string> _movementLockSources = new System.Collections.Generic.HashSet<string>();
        private bool _isGrounded;

        public bool IsMovementLocked => _movementLockSources.Count > 0;

        // Request movement to be locked by a named source (e.g., "Reload", "Mantle").
        public void RequestMovementLock(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }
            
            _movementLockSources.Add(source);
        }

        // Release a previously requested movement lock.
        
        public void ReleaseMovementLock(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }
            
            _movementLockSources.Remove(source);
        }


        

        public void SnapToPosition(Vector3 worldPos)
        {
            _characterController.enabled = false;
            transform.position = worldPos;
            _characterController.enabled = true;
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            
            if (worldDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            }
        }

        // Helper to tick all orthogonal machines from an external owner (PlayerController).
        public void TickMachines()
        {
            CombatStateMachine?.Tick();
            PostureStateMachine?.Tick();
            CoverStateMachine?.Tick();
            InteractionStateMachine?.Tick();
        }
    }
}
