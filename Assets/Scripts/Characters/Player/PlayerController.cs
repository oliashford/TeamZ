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
        [SerializeField] private float _forwardStrafeMinThreshold = 30f;
        [SerializeField] private float _forwardStrafeMaxThreshold = 150f;

        [Header("Airborne Settings (from legacy PlayerAnimationController)")]
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravityMultiplier = 2f;

        [Header("Lock-on Settings")]
        [SerializeField] private Transform _targetLockOnPos;

        private readonly System.Collections.Generic.List<GameObject> _currentTargetCandidates = new System.Collections.Generic.List<GameObject>();
        private GameObject _currentLockOnTarget;
        private bool _isLockedOn;

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

            // Subscribe to lock-on toggle from input so we keep legacy behaviour.
            if (_context != null && _context.InputReader != null)
            {
                _context.InputReader.onLockOnToggled += ToggleLockOn;
                _context.InputReader.onJumpPerformed += OnJumpPerformed;
            }
        }

        private void OnDestroy()
        {
            if (_context != null && _context.InputReader != null)
            {
                _context.InputReader.onLockOnToggled -= ToggleLockOn;
                _context.InputReader.onJumpPerformed -= OnJumpPerformed;
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
            // When jump is pressed, move to airborne. Climb will be layered on later
            // using the ClimbDetectorComponent.
            var airborne = new States.PlayerAirborneState(
                _context,
                _motor,
                _stateMachine,
                _jumpForce,
                _gravityMultiplier,
                this);

            _stateMachine.SetState(airborne);
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

        #endregion
    }
}
