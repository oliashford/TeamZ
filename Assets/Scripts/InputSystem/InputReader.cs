using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamZ.InputSystem
{
    public class InputReader : MonoBehaviour, PlayerControls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        private PlayerControls _controls;

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        public Action onWalkToggled;
        public Action onFirePerformed;
        public Action onReload;
        public Action onMeleePerformed;
        public Action onGadgetPerformed;

        // Use ScriptableObject for movement tunables so they can be shared/per-character/configured
        [Header("Movement Config")]
        [Tooltip("MovementConfig ScriptableObject reference containing walk/sprint/accel/decel tunables")]
        public MovementConfig movementConfig;

        // Exposed runtime state
        public bool IsUsingGamepad { get; private set; }
        public bool IsWalkToggled { get; private set; }
        public bool IsCrouchToggled { get; private set; }
        public bool IsSprinting { get; private set; }

        // 0..1 value representing current movement speed fraction (used by locomotion systems)
        // Guaranteed to be in [0,1]. Consumers should multiply this by SprintMultiplier when sprinting.
        public float CurrentMoveSpeedFraction => Mathf.Clamp01(_currentMoveSpeed);

        // Expose active sprint multiplier separately so consumers don't need to treat >1 speeds specially
        public float ActiveSprintMultiplier => (movementConfig != null && IsSprinting) ? movementConfig.sprintMultiplier : 1f;

        // Internals for smoothing (fractions 0..1)
        private float _targetMoveSpeed;
        private float _currentMoveSpeed;

        /// <inheritdoc cref="OnEnable" />
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new PlayerControls();
                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        /// <inheritdoc cref="OnDisable" />
        public void OnDisable()
        {
            _controls.Player.Disable();
        }

        private void Update()
        {
            if (movementConfig == null)
            {
                // Nothing to do without settings; early exit
                return;
            }

            // Smoothly move current speed toward target using different accel/decel rates
            if (!Mathf.Approximately(_currentMoveSpeed, _targetMoveSpeed))
            {
                float rate = (_currentMoveSpeed < _targetMoveSpeed) ? movementConfig.acceleration : movementConfig.deceleration;
                _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, _targetMoveSpeed, rate * Time.deltaTime);
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnLook callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLook(InputAction.CallbackContext context)
        {
            _mouseDelta = context.ReadValue<Vector2>();

            // Update device detection - if look came from gamepad (right stick) mark as gamepad
            if (context.control != null && context.control.device != null)
            {
                IsUsingGamepad = context.control.device is Gamepad;
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnMove callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
            _movementInputDetected = _moveComposite.magnitude > 0;

            // Detect whether the source of this input is a gamepad or not. This allows analogue sticks
            // to control speed by magnitude while keyboard defaults to digital 0/1 with walk toggle.
            if (context.control != null && context.control.device != null)
            {
                IsUsingGamepad = context.control.device is Gamepad;
            }

            // Compute target speed fraction based on device type and current flags
            float inputMagnitude = Mathf.Clamp01(_moveComposite.magnitude); // 0..1

            if (IsUsingGamepad)
            {
                // Analogue walking: stick magnitude maps smoothly between idle and run.
                // For gamepad we *don't* use the walk toggle; the stick magnitude itself defines pace.
                _targetMoveSpeed = inputMagnitude;
            }
            else
            {
                // Keyboard: default is run (1.0). If walk is toggled, use walkSpeedFactor.
                if (_movementInputDetected)
                {
                    float baseSpeed = (movementConfig != null && IsWalkToggled) ? Mathf.Clamp01(movementConfig.walkSpeedFactor) : 1f;
                    _targetMoveSpeed = Mathf.Clamp01(baseSpeed);
                }
                else
                {
                    _targetMoveSpeed = 0f;
                }
            }

            // Note: Sprint multiplier is NOT applied to _targetMoveSpeed. Instead, consumers should
            // read CurrentMoveSpeedFraction (0..1) and multiply by ActiveSprintMultiplier when calculating
            // final movement speed. This keeps the fraction normalized and separates sprint as a modifier.
        }

        /// <summary>
        ///     Defines the action to perform when the OnJump callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onJumpPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnToggleWalk callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            bool fromGamepad = context.control != null && context.control.device is Gamepad;
            if (fromGamepad)
            {
                // Ignore walk toggle when coming from a gamepad stick press (toggle not desired)
                return;
            }

            IsWalkToggled = !IsWalkToggled;
            onWalkToggled?.Invoke();

            // Recompute target speed immediately for keyboard so changes feel instant
            if (!_movementInputDetected)
            {
                // nothing to do if not moving
                return;
            }

            // For keyboard movement, change target speed to reflect new toggle
            if (!IsUsingGamepad)
            {
                if (movementConfig != null)
                {
                    _targetMoveSpeed = IsWalkToggled ? Mathf.Clamp01(movementConfig.walkSpeedFactor) : 1f;
                }
                else
                {
                    _targetMoveSpeed = IsWalkToggled ? 0.5f : 1f; // fallback if no config provided
                }
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnSprint callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // If we're currently crouched, cancel crouch when sprint starts
                if (IsCrouchToggled)
                {
                    IsCrouchToggled = false;
                    onCrouchDeactivated?.Invoke();
                }

                IsSprinting = true;
                onSprintActivated?.Invoke();

                // No need to change _targetMoveSpeed; sprint is exposed separately via ActiveSprintMultiplier
            }
            else if (context.canceled)
            {
                IsSprinting = false;
                onSprintDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnCrouch callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            // Make crouch a toggle instead of hold
            if (!context.performed)
            {
                return;
            }

            IsCrouchToggled = !IsCrouchToggled;
            if (IsCrouchToggled)
            {
                onCrouchActivated?.Invoke();
            }
            else
            {
                onCrouchDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnAim callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onAimActivated?.Invoke();
            }

            if (context.canceled)
            {
                onAimDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnReload callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnReload(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onReload?.Invoke();
        }

        public void OnMelee(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onMeleePerformed?.Invoke();
        }

        public void OnGadget(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onGadgetPerformed?.Invoke();
        }
        
        public void OnFire(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onFirePerformed?.Invoke();
        }
    }
}
