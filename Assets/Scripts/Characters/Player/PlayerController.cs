using Characters.Core;
using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    // High-level player component that owns the state machine and wires inputs to states.
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private MovementComponent _motor;

        [SerializeField]
        private PlayerContext _playerContext;

        [Header("Locomotion Tuning (mirrors legacy PlayerAnimationController)")]
        [SerializeField] private float _walkSpeed = 1.4f;
        [SerializeField] private float _runSpeed = 2.5f;
        [SerializeField] private float _sprintSpeed = 7f;
        [Tooltip("How quickly the character accelerates/decelerates")]
        [SerializeField] private float _speedChangeDamping = 10f;
        [Tooltip("How quickly the character rotates to face the target direction")]
        [SerializeField] private float _rotationSmoothing = 10f;
        [Tooltip("When true, character always faces camera direction instead of movement direction")]
        [SerializeField] private bool _alwaysStrafe = true;
        [Tooltip("Minimum angle threshold for forward strafe blending")]
        [SerializeField] private float _forwardStrafeMinThreshold = -55f;
        [Tooltip("Maximum angle threshold for forward strafe blending")]
        [SerializeField] private float _forwardStrafeMaxThreshold = 125f;

        [Header("Capsule / Crouch")]
        [SerializeField] private CharacterController _characterController;
        [Tooltip("Character capsule height when standing")]
        [SerializeField] private float _capsuleStandingHeight = 1.8f;
        [Tooltip("Character capsule center Y offset when standing")]
        [SerializeField] private float _capsuleStandingCentre = 0.93f;
        [Tooltip("Character capsule height when crouching")]
        [SerializeField] private float _capsuleCrouchingHeight = 1.2f;
        [Tooltip("Character capsule center Y offset when crouching")]
        [SerializeField] private float _capsuleCrouchingCentre = 0.6f;

        [Header("Airborne Settings (from legacy PlayerAnimationController)")]
        [Tooltip("Initial upward velocity applied when jumping")]
        [SerializeField] private float _jumpForce = 10f;
        [Tooltip("Multiplier applied to gravity when falling")]
        [SerializeField] private float _gravityMultiplier = 2f;

        [Header("Climb Settings")]
        [Tooltip("How far forward from the ledge to place the character after climbing")]
        [SerializeField] private float _ledgeForwardOffset = 0.4f;

        [Header("Combat / Aiming")]
        [SerializeField] private AimComponent _aimComponent;
        [SerializeField] private WeaponHandlerComponent _weaponHandler;

        // New high-level runtime flags mirrored from legacy controller
        private bool _isAiming;
        private bool _isCrouching;
        private bool _isSliding;
        private bool _isWalking;
        private bool _isSprinting;

        private CharacterStateMachine _stateMachine;

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<MovementComponent>();
            }

            if (_playerContext == null)
            {
                _playerContext = GetComponent<PlayerContext>();
            }

            if (_aimComponent == null)
            {
                _aimComponent = GetComponent<AimComponent>();
            }

            if (_weaponHandler == null)
            {
                _weaponHandler = GetComponent<WeaponHandlerComponent>();
            }
            
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            _stateMachine = new CharacterStateMachine();
        }

        private void Start()
        {
            States.PlayerLocomotionState locomotion = new States.PlayerLocomotionState(
                _playerContext,
                _motor,
                _walkSpeed,
                _runSpeed,
                _sprintSpeed,
                _speedChangeDamping,
                _rotationSmoothing,
                _alwaysStrafe,
                _forwardStrafeMinThreshold,
                _forwardStrafeMaxThreshold,
                this);

            _stateMachine.SetState(locomotion);

            if (_playerContext != null && _playerContext.InputReader != null)
            {
                InputSystem.InputReader input = _playerContext.InputReader;
                input.onJumpPerformed += OnJumpPerformed;
                input.onAimActivated += OnAimActivated;
                input.onAimDeactivated += OnAimDeactivated;
                input.onCrouchActivated += OnCrouchActivated;
                input.onCrouchDeactivated += OnCrouchDeactivated;
                input.onWalkToggled += OnWalkToggled;
                input.onSprintActivated += OnSprintActivated;
                input.onSprintDeactivated += OnSprintDeactivated;
                input.onFirePerformed += OnFirePerformed;
            }
        }

        private void OnDestroy()
        {
            if (_playerContext != null && _playerContext.InputReader != null)
            {
                InputSystem.InputReader input = _playerContext.InputReader;
                input.onJumpPerformed -= OnJumpPerformed;
                input.onAimActivated -= OnAimActivated;
                input.onAimDeactivated -= OnAimDeactivated;
                input.onCrouchActivated -= OnCrouchActivated;
                input.onCrouchDeactivated -= OnCrouchDeactivated;
                input.onWalkToggled -= OnWalkToggled;
                input.onSprintActivated -= OnSprintActivated;
                input.onSprintDeactivated -= OnSprintDeactivated;
                input.onFirePerformed -= OnFirePerformed;
            }
        }

        private void Update()
        {
            _stateMachine.Tick();
        }

        private void OnJumpPerformed()
        {
            // First, prefer climb if a climbable ledge is detected in front of the player.
            ClimbDetectorComponent climbDetector = _playerContext != null ? _playerContext.ClimbDetectorComponent : null;

            if (climbDetector != null)
            {
                // Approximate feet height using the CharacterController on this object.
                float feetY = 0f;
                if (_characterController != null)
                {
                    float centreY = transform.position.y + _characterController.center.y;
                    feetY = centreY - (_characterController.height * 0.5f) + _characterController.radius;
                }
                else
                {
                    feetY = transform.position.y;
                }

                if (climbDetector.TryDetectClimb(
                        feetY,
                        out ClimbDetectorComponent.ClimbKind kind,
                        out Vector3 ledgePos,
                        out Vector3 ledgeNormal) &&
                    kind != ClimbDetectorComponent.ClimbKind.None)
                {
                    // Drive animator climb type parameter from detected climb kind.
                    if (_playerContext != null && _playerContext.Animator != null)
                    {
                        int climbTypeHash = Animator.StringToHash("ClimbType");
                        _playerContext.Animator.SetInteger(climbTypeHash, (int)kind);
                    }

                    States.ClimbState climbState = new States.ClimbState(
                        _playerContext,
                        _motor,
                        _stateMachine,
                        this,
                        ledgePos,
                        ledgeNormal,
                        (int)kind);

                    _stateMachine.SetState(climbState);
                    return;
                }
            }

            // If we didn't find a climb, fall back to airborne behaviour.
            States.PlayerAirborneState airborne = new States.PlayerAirborneState(
                _playerContext,
                _motor,
                _stateMachine,
                _jumpForce,
                _gravityMultiplier,
                this);

            _stateMachine.SetState(airborne);
        }

        private void OnAimActivated()
        {
            _isAiming = true;
            
            if (_aimComponent != null)
            {
                _aimComponent.SetAiming(true);
            }
        }

        private void OnAimDeactivated()
        {
            _isAiming = false;
            
            if (_aimComponent != null)
            {
                _aimComponent.SetAiming(false);
            }
        }

        private void OnCrouchActivated()
        {
            // Mirror legacy: only crouch if grounded.
            if (_playerContext != null && _playerContext.IsGrounded)
            {
                SetCrouch(true);
            }
        }

        private void OnCrouchDeactivated()
        {
            // Only stand up if there is enough headroom and we are not sliding, mirroring legacy controller.
            bool cannotStand = _playerContext != null && _playerContext.CannotStandUp;
            if (!cannotStand && !_isSliding)
            {
                SetCrouch(false);
            }
        }

        private void OnWalkToggled()
        {
            // Mirror legacy EnableWalk: only allow walk when grounded and not sprinting.
            bool grounded = _playerContext != null && _playerContext.IsGrounded;
            _isWalking = !_isWalking && grounded && !_isSprinting;
        }

        private void OnSprintActivated()
        {
            // Mirror legacy ActivateSprint: cancel walk, sprint only when not crouching.
            if (!_isCrouching)
            {
                _isWalking = false;
                _isSprinting = true;
            }
        }

        private void OnSprintDeactivated()
        {
            _isSprinting = false;
        }

        private void OnFirePerformed()
        {
            Debug.Log("Fire!");
            
            if (_aimComponent == null || _weaponHandler == null)
            {
                return;
            }

            Ray aimRay = _aimComponent.GetAimRay();
            
            _weaponHandler.Fire(aimRay, _isAiming);
        }

        private void SetCrouch(bool crouch)
        {
            _isCrouching = crouch;

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_characterController != null)
            {
                if (crouch)
                {
                    _characterController.center = new Vector3(0f, _capsuleCrouchingCentre, 0f);
                    _characterController.height = _capsuleCrouchingHeight;
                }
                else
                {
                    _characterController.center = new Vector3(0f, _capsuleStandingCentre, 0f);
                    _characterController.height = _capsuleStandingHeight;
                }
            }
        }

        public void ActivateSliding()
        {
            _isSliding = true;
        }

        public void DeactivateSliding()
        {
            _isSliding = false;
        }

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float SpeedChangeDamping => _speedChangeDamping;
        public float RotationSmoothing => _rotationSmoothing;
        public bool AlwaysStrafe => _alwaysStrafe;
        public float ForwardStrafeMinThreshold => _forwardStrafeMinThreshold;
        public float ForwardStrafeMaxThreshold => _forwardStrafeMaxThreshold;

        public float JumpForce => _jumpForce;
        public float GravityMultiplier => _gravityMultiplier;

        public bool IsAiming => _isAiming;
        public bool IsCrouching => _isCrouching;
        public bool IsSliding => _isSliding;
        public bool IsWalking => _isWalking;
        public bool IsSprinting => _isSprinting;

        // Expose ledge offset so ClimbState can mirror legacy placement
        public float LedgeForwardOffset => _ledgeForwardOffset;
    }
}

