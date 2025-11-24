using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Movement
{
    /// <summary>
    /// Core grounded locomotion for the player. This is a first step in
    /// extracting behaviour out of PlayerAnimationController while keeping
    /// existing movement semantics.
    /// </summary>
    public class MoveState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly MovementComponent _movementComponent;
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
        // private bool _isWalking;
        // private bool _isSprinting;
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
        private const float strafeDirectionDampTime = 5f; // matches _ANIMATION_DAMP_TIME in legacy controller

        // New: mirror legacy forward strafe and camera rotation offset behaviour
        private float _forwardStrafe = 1f;
        private float _cameraRotationOffset;
        private float _strafeAngle;

        // New: legacy-style turning-in-place flag
        private bool _isTurningInPlace;
        
        // Animator parameter names — prefer literal names here to avoid runtime type mismatch
        // (we'll avoid hashing for now to restore previous behaviour quickly).
        private const string ParamIsStrafing = "IsStrafing";
        private const string ParamIsJumping = "IsJumping";
        private const string ParamFallingDuration = "FallingDuration";
        private const string ParamLocomotionStartDirection = "LocomotionStartDirection";
        private const string ParamIsStarting = "IsStarting";
        private const string ParamMoveSpeed = "MoveSpeed";
        private const string ParamCurrentGait = "CurrentGait";
        private const string ParamIsGrounded = "IsGrounded";
        private const string ParamStrafeDirectionX = "StrafeDirectionX";
        private const string ParamStrafeDirectionZ = "StrafeDirectionZ";
        private const string ParamForwardStrafe = "ForwardStrafe";
        private const string ParamCameraRotationOffset = "CameraRotationOffset";
        private const string ParamIsTurningInPlace = "IsTurningInPlace";
        private const string ParamMovementInputHeld = "MovementInputHeld";
        private const string ParamMovementInputPressed = "MovementInputPressed";
        private const string ParamMovementInputTapped = "MovementInputTapped";
        private const string ParamShuffleDirectionX = "ShuffleDirectionX";
        private const string ParamShuffleDirectionZ = "ShuffleDirectionZ";
        private const string ParamIsCrouching = "IsCrouching";
        private const string ParamIsStopped = "IsStopped";

        // legacy-style movement direction in world space
        private Vector3 _moveDirection;

        public MoveState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;
            
            _movementComponent = _context.movement;
            _animator = _context.animator;
            _input = _context.inputReader;

            _walkSpeed = _context.playerConfig.walkSpeed;
            _runSpeed = _context.playerConfig.runSpeed;
            _sprintSpeed = _context.playerConfig.sprintSpeed;
            _speedChangeDamping = _context.playerConfig.speedChangeDamping;
            _rotationSmoothing = _context.playerConfig.rotationSmoothing;

            _alwaysStrafe = _context.playerConfig.alwaysStrafe;
            _forwardStrafeMinThreshold = _context.playerConfig.forwardStrafeMinThreshold;
            _forwardStrafeMaxThreshold = _context.playerConfig.forwardStrafeMaxThreshold;
        }

        public void Enter()
        {
            // Initial flags roughly matching PlayerAnimationController default
            _isStrafing = _alwaysStrafe; // respects camera-relative strafing by default

            // Ensure we don't start in a falling pose when entering locomotion.
            _context.animator.SetBool(ParamIsJumping, false);
            _context.animator.SetFloat(ParamFallingDuration, 0f);
        }

        public void Tick()
        {
            UpdateStateFlags();
            CalculateInputAndStarting();
            UpdateAnimator();
        }

        public void FixedTick()
        {
            // Perform physics / movement updates at fixed timestep
            UpdateLocomotionAndFacing(Time.fixedDeltaTime);
        }

        public void Exit()
        {
            // Nothing to clean up yet; high-level events are owned by PlayerController.
        }

        private void UpdateStateFlags()
        {
            // Flags are now driven by events on PlayerController; here we may add extra logic later if needed.
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

            bool isAiming = _owner != null && _context.IsAiming;
            bool isSprinting = _owner != null && _context.IsSprinting;

            // Sprinting forces non-strafe locomotion, mirroring legacy ActivateSprint behaviour.
            if (isSprinting)
            {
                _isStrafing = false;
            }
            else
            {
                // Shuffle / strafe values mirror legacy FaceMoveDirection behaviour.
                _isStrafing = _alwaysStrafe || isAiming;
            }

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
                        Vector3.Dot(characterRight, desired),
                        Time.deltaTime);
                }
                else
                {
                    // No input: keep shuffle from last value, reset strafe towards idle forward.
                    UpdateStrafeDirection(1f, 0f, Time.deltaTime);
                }
            }
            else
            {
                // In free run mode, Animator expects forward strafe values.
                _shuffleDirectionZ = 1f;
                _shuffleDirectionX = 0f;
                UpdateStrafeDirection(1f, 0f, Time.deltaTime);
            }

            // Mirror CheckIfStarting behaviour.
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer, Time.deltaTime);

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
                        _context.animator.SetFloat(ParamLocomotionStartDirection, _locomotionStartDirection);
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
            
            _context.animator.SetBool(ParamIsStarting, _isStarting);
        }

        private void UpdateStrafeDirection(float targetZ, float targetX, float deltaTime)
        {
            _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, targetZ, strafeDirectionDampTime * deltaTime);
            _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, targetX, strafeDirectionDampTime * deltaTime);
            _strafeDirectionZ = Mathf.Round(_strafeDirectionZ * 1000f) / 1000f;
            _strafeDirectionX = Mathf.Round(_strafeDirectionX * 1000f) / 1000f;
        }

        private float VariableOverrideDelayTimer(float timeVariable, float deltaTime)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= deltaTime;
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
        /// deltaTime is used so this method can be called from FixedTick or Update.
        /// </summary>
        private void UpdateLocomotionAndFacing(float deltaTime)
        {
            // ----- CalculateMoveDirection equivalent -----
            // Reconstruct _moveDirection exactly as legacy SamplePlayerAnimationController.CalculateInput did.
            if (_input != null)
            {
                Vector3 cameraForward = _context.cameraController.GetCameraForwardZeroedYNormalised();
                Vector3 cameraRight = _context.cameraController.GetCameraRightZeroedYNormalised();
                _moveDirection = cameraForward * _input._moveComposite.y + cameraRight * _input._moveComposite.x;
            }
            else
            {
                _moveDirection = Vector3.zero;
            }

            float targetMaxSpeed;
            
            if (!_context.IsGrounded)
            {
                targetMaxSpeed = new Vector3(_context.Velocity.x, 0f, _context.Velocity.z).magnitude;
            }
            else if (_owner != null && _context.IsCrouching)
            {
                targetMaxSpeed = _walkSpeed;
            }
            else if (_owner != null && _context.IsSprinting)
            {
                targetMaxSpeed = _sprintSpeed;
            }
            else if (_owner != null && _context.IsWalking)
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
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, _speedChangeDamping * deltaTime);
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, _speedChangeDamping * deltaTime);

            // Cache 2D speed as in legacy.
            _speed2D = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            // Apply back into context/motor.
            _context.Velocity = velocity;
            _movementComponent.Velocity = velocity;
            _movementComponent.Move(velocity * deltaTime);

            // ----- FaceMoveDirection equivalent -----
            Vector3 characterForward = new Vector3(_context.Transform.forward.x, 0f, _context.Transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(_context.Transform.right.x, 0f, _context.Transform.right.z).normalized;
            Vector3 directionForward = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;

            // Reuse cameraForward from above calculation when available; if input was null, recompute it safely here.
            Vector3 cameraForwardForFacing = _context.cameraController.GetCameraForwardZeroedYNormalised();


            _strafeAngle = characterForward != directionForward && directionForward != Vector3.zero
                ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up)
                : 0f;
            
            
            _isTurningInPlace = false;

            if (_isStrafing)
            {
                if (_moveDirection.magnitude > 0.01f)
                {
                    if (cameraForwardForFacing != Vector3.zero)
                    {
                        // Shuffle directions: immediate dot products, then held.
                        _shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        _shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                        // Strafe directions are damped.
                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward),
                            deltaTime);

                        // Camera rotation offset returns to 0 while moving.
                        _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * deltaTime);

                        // Forward strafe based on configurable strafe angle thresholds.
                        float targetValue =
                            _strafeAngle > _forwardStrafeMinThreshold && _strafeAngle < _forwardStrafeMaxThreshold ? 1f : 0f;

                        if (Mathf.Abs(_forwardStrafe - targetValue) <= 0.001f)
                        {
                            _forwardStrafe = targetValue;
                        }
                        else
                        {
                            float t = Mathf.Clamp01(20f * deltaTime);
                            _forwardStrafe = Mathf.SmoothStep(_forwardStrafe, targetValue, t);
                        }
                    }
                }
                else
                {
                    // No input: rotation offset returns to 0 while idle
                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * deltaTime);
                    
                    if (Mathf.Abs(_cameraRotationOffset) > 10)
                    {
                        _isTurningInPlace = true;
                    }
                }
            }
            else
            {
                // Free-run facing: rotate toward movement direction
                if (_moveDirection.magnitude > 0.01f)
                {
                    float t = Mathf.Clamp01(20f * deltaTime);
                    Quaternion newRot = Quaternion.LookRotation(new Vector3(_moveDirection.x, 0f, _moveDirection.z));
                    _context.Transform.rotation = Quaternion.Slerp(_context.Transform.rotation, newRot, t);
                }
            }

            // Animator parameter updates that require the movement values
            _context.animator.SetFloat(ParamMoveSpeed, _speed2D);
            
            _context.animator.SetInteger(ParamCurrentGait, (int) _currentGait);
            
            _context.animator.SetBool(ParamIsGrounded, _context.IsGrounded);
            _context.animator.SetFloat(ParamStrafeDirectionX, _strafeDirectionX);
            _context.animator.SetFloat(ParamStrafeDirectionZ, _strafeDirectionZ);
            _context.animator.SetFloat(ParamForwardStrafe, _forwardStrafe);
            _context.animator.SetFloat(ParamCameraRotationOffset, _cameraRotationOffset);
            _context.animator.SetBool(ParamIsTurningInPlace, _isTurningInPlace);
            _context.animator.SetBool(ParamMovementInputHeld, _movementInputHeld);
            _context.animator.SetBool(ParamMovementInputPressed, _movementInputPressed);
            _context.animator.SetBool(ParamMovementInputTapped, _movementInputTapped);
            _context.animator.SetFloat(ParamShuffleDirectionX, _shuffleDirectionX);
            _context.animator.SetFloat(ParamShuffleDirectionZ, _shuffleDirectionZ);
            _context.animator.SetBool(ParamIsCrouching, _owner != null && _context.IsCrouching);
            _context.animator.SetBool(ParamIsStopped, _speed2D < 0.01f);
         }
        
        
        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }
        
        
        private GaitState _currentGait;

        
        private void CalculateGait()
        {
            float runThreshold = (_walkSpeed + _runSpeed) / 2;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2;

            if (_speed2D < 0.01)
            {
                _currentGait = GaitState.Idle;
            }
            else if (_speed2D < runThreshold)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < sprintThreshold)
            {
                _currentGait = GaitState.Run;
            }
            else
            {
                _currentGait = GaitState.Sprint;
            }
        }
 
         private void UpdateAnimator()
         {
             // Set animator parameters that are independent of the movement timestep
             _context.animator.SetFloat(ParamIsStrafing, _isStrafing ? 1.0f : 0.0f);
             _context.animator.SetBool(ParamIsTurningInPlace, _isTurningInPlace);
             _context.animator.SetBool(ParamMovementInputHeld, _movementInputHeld);
         }
    }
}
