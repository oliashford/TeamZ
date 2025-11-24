using TeamZ.Characters.Player.Components;
using TeamZ.Characters.Player.States;
using TeamZ.Characters.Player.States.Combat;
using TeamZ.Characters.Player.States.Cover;
using TeamZ.Characters.Player.States.Movement;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    // High-level player component that owns the state machine and wires inputs to 
    
    [RequireComponent(typeof(PlayerContext))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        private PlayerContext _context;
        private CharacterController _characterController;
        private PlayerController _movementComponent;
        private object _aimComponent;

        private void Awake()
        {
            // Get the PlayerContext first (it caches the frequently used components).
            if (!TryGetComponent(out _context))
            {
                Debug.LogError("PlayerController requires a PlayerContext component.");
                throw new System.InvalidOperationException("PlayerController requires a PlayerContext component.");
            }
            
        }

        private void Start()
        {
            _context.MovementStateMachine.SetState(new MovementIdleState(_context, this));
            _context.CombatStateMachine.SetState(new CombatIdleState(_context, this));
            _context.PostureStateMachine.SetState(new PostureIdleState(_context));
            _context.CoverStateMachine.SetState(new CoverIdleState(_context));
            _context.InteractionStateMachine.SetState(new CombatIdleState(_context, this));

            if (_context != null && _context.inputReader != null)
            {
                InputSystem.InputReader input = _context.inputReader;
                
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
            if (_context != null && _context.inputReader != null)
            {
                InputSystem.InputReader input = _context.inputReader;
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
            // Tick orthogonal machines first so they can request movement locks / modify context.
            if (_context != null)
            {
                _context.TickMachines();
            }

            // If an orthogonal machine has requested a movement lock, skip locomotion updates.
            if (_context == null || !_context.IsMovementLocked)
            {
                _context?.MovementStateMachine?.Tick();
            }
        }

        private void FixedUpdate()
        {
            // Run movement physics updates in FixedUpdate through the movement state machine.
            if (_context == null || _context.IsMovementLocked)
            {
                return;
            }
            
            _context?.MovementStateMachine?.FixedTick();
        }

        private void OnJumpPerformed()
        {
            // First, prefer climb if a climbable ledge is detected in front of the player.
            ClimbDetectorComponent climbDetector = _context != null ? _context.climbDetector : null;

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
                    if (_context != null && _context.animator != null)
                    {
                        _context.animator.SetFloat("ClimbType", (int)kind);
                    }

                    var climbState = new States.Movement.ClimbState(_context,this, ledgePos, ledgeNormal, (int)kind);

                    _context?.MovementStateMachine.SetState(climbState);
                    
                    return;
                }
            }

            // If we didn't find a climb, fall back to airborne behaviour.
            var airborne = new States.Movement.AirborneState(_context,this);

            _context?.MovementStateMachine.SetState(airborne);
        }

        private void OnAimActivated()
        {
            _context.IsAiming = true;
            
            if (_aimComponent != null)
            {
                _context.aim.SetAiming(true);
            }
        }

        private void OnAimDeactivated()
        {
            _context.IsAiming = false;
            
            if (_aimComponent != null)
            {
                _context.aim.SetAiming(false);
            }
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
            if (!cannotStand && _context.IsSliding)
            {
                SetCrouch(false);
            }
        }

        private void OnWalkToggled()
        {
            // Mirror legacy EnableWalk: only allow walk when grounded and not sprinting.
            bool grounded = _context != null && _context.IsGrounded;
            _context.IsWalking = !_context.IsWalking && grounded && !_context.IsSprinting;
        }

        private void OnSprintActivated()
        {
            // Mirror legacy ActivateSprint: cancel walk, sprint only when not crouching.
            if (!_context.IsCrouching)
            {
                _context.IsWalking = false;
                _context.IsSprinting = true;
            }
        }

        private void OnSprintDeactivated()
        {
            _context.IsSprinting = false;
        }

        // Fire is handled by the Combat state machine now.

        private void SetCrouch(bool crouch)
        {
            _context.IsCrouching = crouch;

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_characterController != null)
            {
                if (crouch)
                {
                    _characterController.center = new Vector3(0f, _context.playerConfig.capsuleCrouchingCentre, 0f);
                    _characterController.height = _context.playerConfig.capsuleCrouchingHeight;
                }
                else
                {
                    _characterController.center = new Vector3(0f, _context.playerConfig.capsuleStandingCentre, 0f);
                    _characterController.height = _context.playerConfig.capsuleStandingHeight;
                }
            }
        }

        public void ActivateSliding()
        {
            _context.IsSliding = true;
        }

        public void DeactivateSliding()
        {
            _context.IsSliding = false;
        }

        // Expose ledge offset so ClimbState can mirror legacy placement
        public float LedgeForwardOffset => _context.playerConfig.ledgeForwardOffset;

        // Debug helper: expose the current locomotion state's type name
        public string CurrentLocomotionStateName => _context != null && _context.MovementStateMachine != null && _context.MovementStateMachine.CurrentState != null
            ? _context.MovementStateMachine.CurrentState.GetType().Name
            : "None";
    }
}
