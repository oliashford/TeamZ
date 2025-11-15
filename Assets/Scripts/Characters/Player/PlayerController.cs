using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    /// <summary>
    /// High-level player component that owns the state machine and wires inputs to states.
    /// NOTE: Right now this is just scaffolding; your existing PlayerAnimationController
    /// in Core/PlayerLegacy still drives live gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour, ILockOnReceiver
    {
        [Tooltip("Movement component used for player movement. If null, one is fetched from this GameObject.")]
        [SerializeField]
        private CharacterMovementComponent _motor;

        [Tooltip("Context object exposing animator, input, camera etc.")]
        [SerializeField]
        private PlayerContext _context;

        [Header("Locomotion Tuning (mirrors legacy PlayerAnimationController)")]
        [SerializeField] private float _walkSpeed = 1.4f;
        [SerializeField] private float _runSpeed = 2.5f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _speedChangeDamping = 10f;
        [SerializeField] private float _rotationSmoothing = 10f;
        [SerializeField] private bool _alwaysStrafe = true;
        [SerializeField] private float _forwardStrafeMinThreshold = -55f;
        [SerializeField] private float _forwardStrafeMaxThreshold = 125f;

        [Header("Capsule / Crouch")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _capsuleStandingHeight = 1.8f;
        [SerializeField] private float _capsuleStandingCentre = 0.93f;
        [SerializeField] private float _capsuleCrouchingHeight = 1.2f;
        [SerializeField] private float _capsuleCrouchingCentre = 0.6f;

        [Header("Airborne Settings (from legacy PlayerAnimationController)")]
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravityMultiplier = 2f;

        [Header("Climb Settings")]
        [SerializeField] private float _ledgeForwardOffset = 0.4f;

        [Header("Lock-on Settings")]
        [SerializeField] private Transform _targetLockOnPos;

        private readonly System.Collections.Generic.List<GameObject> _currentTargetCandidates = new System.Collections.Generic.List<GameObject>();
        private GameObject _currentLockOnTarget;
        private bool _isLockedOn;

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
                _motor = GetComponent<CharacterMovementComponent>();
            }

            if (_context == null)
            {
                _context = GetComponent<PlayerContext>();
            }

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_targetLockOnPos == null && _context != null)
            {
                var t = _context.transform.Find("TargetLockOnPos");
                if (t != null)
                {
                    _targetLockOnPos = t;
                }
            }

            _stateMachine = new CharacterStateMachine();
        }

        private void Start()
        {
            var locomotion = new States.PlayerLocomotionState(
                _context,
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

            if (_context != null && _context.InputReader != null)
            {
                var input = _context.InputReader;
                input.onLockOnToggled += ToggleLockOn;
                input.onJumpPerformed += OnJumpPerformed;
                input.onAimActivated += OnAimActivated;
                input.onAimDeactivated += OnAimDeactivated;
                input.onCrouchActivated += OnCrouchActivated;
                input.onCrouchDeactivated += OnCrouchDeactivated;
                input.onWalkToggled += OnWalkToggled;
                input.onSprintActivated += OnSprintActivated;
                input.onSprintDeactivated += OnSprintDeactivated;
            }
        }

        private void OnDestroy()
        {
            if (_context != null && _context.InputReader != null)
            {
                var input = _context.InputReader;
                input.onLockOnToggled -= ToggleLockOn;
                input.onJumpPerformed -= OnJumpPerformed;
                input.onAimActivated -= OnAimActivated;
                input.onAimDeactivated -= OnAimDeactivated;
                input.onCrouchActivated -= OnCrouchActivated;
                input.onCrouchDeactivated -= OnCrouchDeactivated;
                input.onWalkToggled -= OnWalkToggled;
                input.onSprintActivated -= OnSprintActivated;
                input.onSprintDeactivated -= OnSprintDeactivated;
            }
        }

        private void Update()
        {
            _stateMachine.Tick();

            // Continuously update best target when not locked-on, mirroring legacy behaviour.
            UpdateBestTarget();
        }

        private void OnJumpPerformed()
        {
            // First, prefer climb if a climbable ledge is detected in front of the player.
            var climbDetector = _context != null ? _context.ClimbDetectorComponent : null;

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
                    if (_context != null && _context.Animator != null)
                    {
                        int climbTypeHash = Animator.StringToHash("ClimbType");
                        _context.Animator.SetInteger(climbTypeHash, (int)kind);
                    }

                    var climbState = new States.ClimbState(
                        _context,
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
            var airborne = new States.PlayerAirborneState(
                _context,
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
        }

        private void OnAimDeactivated()
        {
            _isAiming = false;
        }

        private void OnCrouchActivated()
        {
            // Mirror legacy: only crouch if grounded.
            if (_context != null && _context.IsGrounded)
            {
                SetCrouch(true);
            }
        }

        private void OnCrouchDeactivated()
        {
            // Only stand up if there is enough headroom and we are not sliding, mirroring legacy controller.
            bool cannotStand = _context != null && _context.CannotStandUp;
            if (!cannotStand && !_isSliding)
            {
                SetCrouch(false);
            }
        }

        private void OnWalkToggled()
        {
            // Mirror legacy EnableWalk: only allow walk when grounded and not sprinting.
            bool grounded = _context != null && _context.IsGrounded;
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

        #region ILockOnReceiver

        public void AddTargetCandidate(GameObject target)
        {
            if (target != null && !_currentTargetCandidates.Contains(target))
            {
                _currentTargetCandidates.Add(target);
            }
        }

        public void RemoveTarget(GameObject target)
        {
            if (target != null && _currentTargetCandidates.Contains(target))
            {
                _currentTargetCandidates.Remove(target);
            }
        }

        #endregion

        #region Lock-on Logic (ported from legacy PlayerAnimationController)

        private void ToggleLockOn()
        {
            EnableLockOn(!_isLockedOn);
        }

        private void EnableLockOn(bool enable)
        {
            _isLockedOn = enable;

            var cameraController = _context != null ? _context.CameraController : null;
            if (cameraController != null && _targetLockOnPos != null)
            {
                cameraController.LockOn(enable, _targetLockOnPos);
            }

            if (enable && _currentLockOnTarget != null)
            {
                var lockOn = _currentLockOnTarget.GetComponent<ObjectLockOn>();
                if (lockOn != null)
                {
                    lockOn.Highlight(true, true);
                }
            }
        }

        private void UpdateBestTarget()
        {
            GameObject newBestTarget;

            if (_currentTargetCandidates.Count == 0)
            {
                newBestTarget = null;
            }
            else if (_currentTargetCandidates.Count == 1)
            {
                newBestTarget = _currentTargetCandidates[0];
            }
            else
            {
                newBestTarget = null;
                float bestTargetScore = 0f;

                var cameraController = _context != null ? _context.CameraController : null;
                if (cameraController == null)
                {
                    return;
                }

                foreach (GameObject target in _currentTargetCandidates)
                {
                    var lockOn = target.GetComponent<ObjectLockOn>();
                    if (lockOn != null)
                    {
                        lockOn.Highlight(false, false);
                    }

                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    float distanceScore = 1 / distance * 100;

                    Vector3 targetDirection = target.transform.position - cameraController.GetCameraPosition();
                    float angleInView = Vector3.Dot(targetDirection.normalized, cameraController.GetCameraForward());
                    float angleScore = angleInView * 40;

                    float totalScore = distanceScore + angleScore;

                    if (totalScore > bestTargetScore)
                    {
                        bestTargetScore = totalScore;
                        newBestTarget = target;
                    }
                }
            }

            if (!_isLockedOn)
            {
                _currentLockOnTarget = newBestTarget;

                if (_currentLockOnTarget != null)
                {
                    var lockOn = _currentLockOnTarget.GetComponent<ObjectLockOn>();
                    if (lockOn != null)
                    {
                        lockOn.Highlight(true, false);
                    }
                }
            }
            else
            {
                if (_currentLockOnTarget != null && _currentTargetCandidates.Contains(_currentLockOnTarget))
                {
                    var lockOn = _currentLockOnTarget.GetComponent<ObjectLockOn>();
                    if (lockOn != null)
                    {
                        lockOn.Highlight(true, true);
                    }
                }
                else
                {
                    _currentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }

        public bool IsLockedOn => _isLockedOn;
        public GameObject CurrentLockOnTarget => _currentLockOnTarget;

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
#endregion