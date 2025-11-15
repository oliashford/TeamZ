using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States
{
    /// <summary>
    /// Core grounded locomotion for the player. This is a first step in
    /// extracting behaviour out of PlayerAnimationController while keeping
    /// existing movement semantics.
    /// </summary>
    public class PlayerLocomotionState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly CharacterMovementComponent _motor;
        private readonly Animator _animator;
        private readonly InputReader _input;
        private readonly PlayerController _owner;

        // Cached config values from context / inspector
        private readonly float _walkSpeed;
        private readonly float _runSpeed;
        private readonly float _sprintSpeed;
        private readonly float _speedChangeDamping;
        private readonly float _rotationSmoothing;

        // New: legacy-style config flags
        private readonly bool _alwaysStrafe;
        private readonly float _forwardStrafeMinThreshold;
        private readonly float _forwardStrafeMaxThreshold;

        // Simple locomotion state
        private bool _isWalking;
        private bool _isSprinting;
        private bool _isStrafing;

        private Vector3 _targetVelocity;

        // Mirror legacy state variables
        private float _speed2D;

        // Legacy-style input and starting flags
        private bool _movementInputTapped;
        private bool _movementInputPressed;
        private bool _movementInputHeld;
        private float _movementInputDuration;

        private bool _isStarting;
        private float _locomotionStartDirection;
        private float _locomotionStartTimer;
        private float _newDirectionDifferenceAngle;

        // Legacy-style shuffle and strafe values
        private float _shuffleDirectionX;
        private float _shuffleDirectionZ;
        private float _strafeDirectionX = 1f;
        private float _strafeDirectionZ;
        private const float StrafeDirectionDampTime = 5f; // matches _ANIMATION_DAMP_TIME in legacy controller

        // New: mirror legacy forward strafe and camera rotation offset behaviour
        private float _forwardStrafe = 1f;
        private float _cameraRotationOffset;
        private float _strafeAngle;

        // New: legacy-style turning-in-place flag
        private bool _isTurningInPlace;

        // legacy-style movement direction in world space
        private Vector3 _moveDirection;

        public PlayerLocomotionState(
            PlayerContext context,
            CharacterMovementComponent motor,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            float speedChangeDamping,
            float rotationSmoothing,
            bool alwaysStrafe,
            float forwardStrafeMinThreshold,
            float forwardStrafeMaxThreshold,
            PlayerController owner)
        {
            _context = context;
            _motor = motor;
            _animator = context.Animator;
            _input = context.InputReader;
            _owner = owner;

            _walkSpeed = walkSpeed;
            _runSpeed = runSpeed;
            _sprintSpeed = sprintSpeed;
            _speedChangeDamping = speedChangeDamping;
            _rotationSmoothing = rotationSmoothing;

            _alwaysStrafe = alwaysStrafe;
            _forwardStrafeMinThreshold = forwardStrafeMinThreshold;
            _forwardStrafeMaxThreshold = forwardStrafeMaxThreshold;
        }

        public void Enter()
        {
            // Initial flags roughly matching PlayerAnimationController default
            _isStrafing = _alwaysStrafe; // respects camera-relative strafing by default

            // Ensure we don't start in a falling pose when entering locomotion.
            _animator.SetBool("IsJumping", false);
            _animator.SetFloat("FallingDuration", 0f);

            // Subscribe to input events just like the legacy controller did.
            if (_input != null)
            {
                _input.onWalkToggled += OnWalkToggled;
                _input.onSprintActivated += OnSprintActivated;
                _input.onSprintDeactivated += OnSprintDeactivated;
            }
        }

        public void Tick()
        {
            UpdateStateFlags();
            CalculateInputAndStarting();
            UpdateLocomotionAndFacing();
            UpdateAnimator();
        }

        public void Exit()
        {
            if (_input != null)
            {
                _input.onWalkToggled -= OnWalkToggled;
                _input.onSprintActivated -= OnSprintActivated;
                _input.onSprintDeactivated -= OnSprintDeactivated;
            }
        }

        private void OnWalkToggled()
        {
            _isWalking = !_isWalking && _context.IsGrounded && !_isSprinting;
        }

        private void OnSprintActivated()
        {
            if (!_isWalking)
            {
                _isSprinting = true;
                _isStrafing = false;
            }
        }

        private void OnSprintDeactivated()
        {
            _isSprinting = false;

            bool isLockedOn = _owner != null && _owner.IsLockedOn;
            _isStrafing = _alwaysStrafe || isLockedOn;
        }

        private void UpdateStateFlags()
        {
            // Flags are now driven by events; here we may add extra logic later if needed.
        }

        private void CalculateInputAndStarting()
        {
            // Mirror legacy CalculateInput using InputReader state.
            if (_input != null && _input._movementInputDetected)
            {
                if (Mathf.Approximately(_movementInputDuration, 0f))
                {
                    _movementInputTapped = true;
                    _movementInputPressed = false;
                    _movementInputHeld = false;
                }
                else if (_movementInputDuration > 0f && _movementInputDuration < 0.15f)
                {
                    _movementInputTapped = false;
                    _movementInputPressed = true;
                    _movementInputHeld = false;
                }
                else
                {
                    _movementInputTapped = false;
                    _movementInputPressed = false;
                    _movementInputHeld = true;
                }

                _movementInputDuration += Time.deltaTime;
            }
            else
            {
                _movementInputDuration = 0f;
                _movementInputTapped = false;
                _movementInputPressed = false;
                _movementInputHeld = false;
            }

            // Compute move direction for starting and shuffle/strafe logic.
            Vector3 moveInputWorld = _context.MoveInputWorld;
            Vector3 characterForward = new Vector3(_context.Transform.forward.x, 0f, _context.Transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(_context.Transform.right.x, 0f, _context.Transform.right.z).normalized;
            Vector3 desired = new Vector3(moveInputWorld.x, 0f, moveInputWorld.z).normalized;

            _newDirectionDifferenceAngle = characterForward != desired
                ? Vector3.SignedAngle(characterForward, desired, Vector3.up)
                : 0f;

            // Shuffle / strafe values mirror legacy FaceMoveDirection behaviour.
            if (_isStrafing)
            {
                if (desired.magnitude > 0.01f)
                {
                    // Shuffle directions are immediate dot products and then held.
                    _shuffleDirectionZ = Vector3.Dot(characterForward, desired);
                    _shuffleDirectionX = Vector3.Dot(characterRight, desired);

                    // Strafe directions are damped.
                    UpdateStrafeDirection(
                        Vector3.Dot(characterForward, desired),
                        Vector3.Dot(characterRight, desired)
                    );
                }
                else
                {
                    // No input: keep shuffle from last value, reset strafe towards idle forward.
                    UpdateStrafeDirection(1f, 0f);
                }
            }
            else
            {
                // In free run mode, Animator expects forward strafe values.
                _shuffleDirectionZ = 1f;
                _shuffleDirectionX = 0f;
                UpdateStrafeDirection(1f, 0f);
            }

            // Mirror CheckIfStarting behaviour.
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);

            bool isStartingCheck = false;
            if (_locomotionStartTimer <= 0.0f)
            {
                if (desired.magnitude > 0.01f && _speed2D < 1f && !_isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                        _animator.SetFloat("LocomotionStartDirection", _locomotionStartDirection);
                    }

                    float delayTime = 0.2f;
                    _locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _isStarting = isStartingCheck;
            _animator.SetBool("IsStarting", _isStarting);
        }

        private void UpdateStrafeDirection(float targetZ, float targetX)
        {
            _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, targetZ, StrafeDirectionDampTime * Time.deltaTime);
            _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, targetX, StrafeDirectionDampTime * Time.deltaTime);
            _strafeDirectionZ = Mathf.Round(_strafeDirectionZ * 1000f) / 1000f;
            _strafeDirectionX = Mathf.Round(_strafeDirectionX * 1000f) / 1000f;
        }

        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }

            return timeVariable;
        }

        /// <summary>
        /// Combined legacy-style locomotion update: computes target velocity
        /// from _moveDirection and applies facing logic equivalent to
        /// SamplePlayerAnimationController.CalculateMoveDirection + FaceMoveDirection.
        /// </summary>
        private void UpdateLocomotionAndFacing()
        {
            // ----- CalculateMoveDirection equivalent -----
            // Reconstruct _moveDirection exactly as legacy SamplePlayerAnimationController.CalculateInput did.
            if (_input != null)
            {
                Vector3 cameraForward = _context.CameraController.GetCameraForwardZeroedYNormalised();
                Vector3 cameraRight = _context.CameraController.GetCameraRightZeroedYNormalised();
                _moveDirection = cameraForward * _input._moveComposite.y + cameraRight * _input._moveComposite.x;
            }
            else
            {
                _moveDirection = Vector3.zero;
            }

            float targetMaxSpeed;
            if (!_context.IsGrounded)
            {
                // Legacy used _currentMaxSpeed here; we approximate by preserving current horizontal speed.
                targetMaxSpeed = new Vector3(_context.Velocity.x, 0f, _context.Velocity.z).magnitude;
            }
            else if (_isSprinting)
            {
                targetMaxSpeed = _sprintSpeed;
            }
            else if (_isWalking)
            {
                targetMaxSpeed = _walkSpeed;
            }
            else
            {
                targetMaxSpeed = _runSpeed;
            }

            Vector3 velocity = _context.Velocity;

            // Project _moveDirection into velocity target, like legacy.
            Vector3 targetVelocity;
            targetVelocity.x = _moveDirection.x * targetMaxSpeed;
            targetVelocity.y = velocity.y; // preserve vertical
            targetVelocity.z = _moveDirection.z * targetMaxSpeed;

            // Damped horizontal velocity changes.
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, _speedChangeDamping * Time.deltaTime);
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, _speedChangeDamping * Time.deltaTime);

            // Cache 2D speed as in legacy.
            _speed2D = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            // Apply back into context/motor.
            _context.Velocity = velocity;
            _motor.Velocity = velocity;
            _motor.Move(velocity * Time.deltaTime);

            // ----- FaceMoveDirection equivalent -----
            Vector3 characterForward = new Vector3(_context.Transform.forward.x, 0f, _context.Transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(_context.Transform.right.x, 0f, _context.Transform.right.z).normalized;
            Vector3 directionForward = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;

            // Reuse cameraForward from above calculation when available; if input was null, recompute it safely here.
            Vector3 cameraForwardForFacing = _input != null
                ? _context.CameraController.GetCameraForwardZeroedYNormalised()
                : _context.CameraController.GetCameraForwardZeroedYNormalised();

            Quaternion strafingTargetRotation = cameraForwardForFacing != Vector3.zero
                ? Quaternion.LookRotation(cameraForwardForFacing)
                : _context.Transform.rotation;

            _strafeAngle = characterForward != directionForward && directionForward != Vector3.zero
                ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up)
                : 0f;

            if (_isStrafing)
            {
                if (_moveDirection.magnitude > 0.01f)
                {
                    if (cameraForwardForFacing != Vector3.zero)
                    {
                        // Shuffle directions: immediate dot products, then held.
                        _shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        _shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                        // Strafe directions: damped.
                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward));

                        // Camera rotation offset returns to 0 while moving.
                        _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                        // Forward strafe based on configurable strafe angle thresholds.
                        float targetValue =
                            _strafeAngle > _forwardStrafeMinThreshold && _strafeAngle < _forwardStrafeMaxThreshold ? 1f : 0f;

                        if (Mathf.Abs(_forwardStrafe - targetValue) <= 0.001f)
                        {
                            _forwardStrafe = targetValue;
                        }
                        else
                        {
                            float t = Mathf.Clamp01(20f * Time.deltaTime);
                            _forwardStrafe = Mathf.SmoothStep(_forwardStrafe, targetValue, t);
                        }
                    }

                    // Rotate character to match camera.
                    _context.Transform.rotation = Quaternion.Slerp(
                        _context.Transform.rotation,
                        strafingTargetRotation,
                        _rotationSmoothing * Time.deltaTime);

                    _isTurningInPlace = false;
                }
                else
                {
                    // No movement input: update strafe direction towards forward.
                    UpdateStrafeDirection(1f, 0f);

                    float t = 20f * Time.deltaTime;
                    float newOffset = 0f;

                    if (characterForward != cameraForwardForFacing && cameraForwardForFacing != Vector3.zero)
                    {
                        newOffset = Vector3.SignedAngle(characterForward, cameraForwardForFacing, Vector3.up);
                    }

                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, newOffset, t);

                    // Rotate character towards camera.
                    _context.Transform.rotation = Quaternion.Slerp(
                        _context.Transform.rotation,
                        strafingTargetRotation,
                        _rotationSmoothing * Time.deltaTime);

                    // Legacy threshold for turn-in-place.
                    _isTurningInPlace = Mathf.Abs(_cameraRotationOffset) > 10f;
                }
            }
            else
            {
                // Non-strafing: always treat shuffle as forward, face velocity.
                UpdateStrafeDirection(1f, 0f);
                _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                _shuffleDirectionZ = 1f;
                _shuffleDirectionX = 0f;

                Vector3 faceDirection = new Vector3(velocity.x, 0f, velocity.z);
                if (faceDirection != Vector3.zero)
                {
                    _context.Transform.rotation = Quaternion.Slerp(
                        _context.Transform.rotation,
                        Quaternion.LookRotation(faceDirection),
                        _rotationSmoothing * Time.deltaTime);
                }

                _forwardStrafe = Mathf.MoveTowards(_forwardStrafe, 1f, 5f * Time.deltaTime);
                _isTurningInPlace = false;
            }
        }

        private void UpdateAnimator()
        {
            float speed2D = _speed2D;
            _animator.SetFloat("MoveSpeed", speed2D);

            float runThreshold = (_walkSpeed + _runSpeed) / 2f;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2f;

            int gait;
            if (speed2D < 0.01f)
                gait = 0;
            else if (speed2D < runThreshold)
                gait = 1;
            else if (speed2D < sprintThreshold)
                gait = 2;
            else
                gait = 3;

            _animator.SetInteger("CurrentGait", gait);

            bool isGrounded = _context.IsGrounded;
            bool hasMoveInput = _context.MoveInputWorld.sqrMagnitude > 0.0001f;
            bool isStopped = !hasMoveInput && speed2D < 0.5f;

            _animator.SetBool("IsGrounded", isGrounded);
            _animator.SetBool("IsWalking", _isWalking && speed2D > 0.1f);
            _animator.SetBool("IsStopped", isStopped);

            // Movement input mode flags mirroring legacy controller.
            _animator.SetBool("MovementInputHeld", _movementInputHeld);
            _animator.SetBool("MovementInputPressed", _movementInputPressed);
            _animator.SetBool("MovementInputTapped", _movementInputTapped);

            // Legacy uses a float 0/1 for IsStrafing.
            _animator.SetFloat("IsStrafing", _isStrafing ? 1f : 0f);

            // Shuffle/strafe parameters used by locomotion blend tree.
            _animator.SetFloat("ShuffleDirectionX", _shuffleDirectionX);
            _animator.SetFloat("ShuffleDirectionZ", _shuffleDirectionZ);
            _animator.SetFloat("StrafeDirectionX", _strafeDirectionX);
            _animator.SetFloat("StrafeDirectionZ", _strafeDirectionZ);

            // Forward strafe and camera rotation offset (for turning/aiming blends).
            _animator.SetFloat("ForwardStrafe", _forwardStrafe);
            _animator.SetFloat("CameraRotationOffset", _cameraRotationOffset);

            // New: explicitly tell the Animator when we are turning in place (for shuffle/turn clips).
            _animator.SetBool("IsTurningInPlace", _isTurningInPlace);
        }
    }
}
