using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Movement
{
    public class AirborneState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;
        
        private readonly InputReader _input;
        private readonly MovementComponent _movementComponent;
        private readonly Animator _animator;

        private readonly float _jumpForce;
        private readonly float _gravityMultiplier;

        private float _fallStartTime;
        private bool _isJumpingPhase;

        public AirborneState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;
            
            _movementComponent = _context.movement;
            _animator = _context.animator;
            _input = _context.inputReader;

            _jumpForce = _context.playerConfig.jumpForce;
            _gravityMultiplier = _context.playerConfig.gravityMultiplier;
        }

        public void Enter()
        {
            // Start in jump phase if we are grounded when entering, otherwise treat as fall.
            if (_context.IsGrounded)
            {
                EnterJumpPhase();
            }
            else
            {
                EnterFallPhase(resetVerticalVelocity: false);
            }
        }

        public void Tick()
        {
            if (_isJumpingPhase)
            {
                // Animator-driven jump phase logic (timing-only). Horizontal motion handled in FixedTick.
                UpdateJumpPhase();
            }
            else
            {
                // Animator update (falling duration) kept on Update so UI/anim reacts per-frame.
                UpdateFallPhase_TickOnly();
            }
        }

        public void FixedTick()
        {
            if (_isJumpingPhase)
            {
                // Physics during jump
                ApplyGravity();

                Vector3 vel = _context.Velocity;

                // When upward velocity has gone non-positive, transition to falling phase.
                if (vel.y <= 0f)
                {
                    _context.animator.SetBool("IsJumping", false);
                    EnterFallPhase(resetVerticalVelocity: false);
                    return;
                }

                // Horizontal movement is controlled by locomotion inputs.
                ApplyHorizontalMovement();

                _movementComponent.Move(_movementComponent.Velocity * Time.fixedDeltaTime);
            }
            else
            {
                // Falling physics
                ApplyGravity();
                
                ApplyHorizontalMovement();
                
                _movementComponent.Move(_movementComponent.Velocity * Time.fixedDeltaTime);

                // When grounded again, return to locomotion
                if (_context.IsGrounded)
                {
                    _context.MovementStateMachine.SetState(new MoveState(_context, _owner));
                }
            }
        }

        public void Exit()
        {
            // Clear jump bool when leaving airborne.
            _context.animator.SetBool("IsJumping", false);
        }

        private void EnterJumpPhase()
        {
            _isJumpingPhase = true;

            // Raise the jump bool and apply upward force (mirrors legacy EnterJumpState).
            _context.animator.SetBool("IsJumping", true);

            Vector3 vel = _context.Velocity;
            
            vel.y = _jumpForce;
            
            _context.Velocity = vel;
            
            _movementComponent.Velocity = vel;
        }

        private void UpdateJumpPhase()
        {
            // Keep per-frame animator/state updates here; physics handled in FixedTick.
            Vector3 vel = _context.Velocity;

            // When upward velocity has gone non-positive, transition to falling.
            if (vel.y <= 0f)
            {
                _context.animator.SetBool("IsJumping", false);
                
                EnterFallPhase(resetVerticalVelocity: false);
                return;
            }

            // Horizontal movement is handled in FixedTick
        }

        private void EnterFallPhase(bool resetVerticalVelocity)
        {
            _isJumpingPhase = false;
            
            _fallStartTime = Time.time;

            Vector3 vel = _context.Velocity;
            
            if (resetVerticalVelocity)
            {
                vel.y = 0f;
            }
            _context.Velocity = vel;
            
            _movementComponent.Velocity = vel;
        }

        private void UpdateFallPhase_TickOnly()
        {
            // Per-frame animator update for falling duration.
            float fallingDuration = Time.time - _fallStartTime;
            
            _context.animator.SetFloat("FallingDuration", fallingDuration);
        }

        private void ApplyGravity()
        {
            Vector3 vel = _context.Velocity;

            if (vel.y > Physics.gravity.y)
            {
                vel.y += Physics.gravity.y * _gravityMultiplier * Time.fixedDeltaTime;

                _context.Velocity = vel;
                _movementComponent.Velocity = vel;
            }
        }

        /// <summary>
        /// Use the same horizontal input calculation as locomotion, but without changing gait.
        /// </summary>
        private void ApplyHorizontalMovement()
        {
            Vector3 moveInputWorld = _context.MoveInputWorld;

            // Use run speed as horizontal speed while airborne; mirrors legacy behaviour
            // where _targetMaxSpeed was generally maintained.
            float horizontalSpeed = _context.playerConfig.runSpeed;

            Vector3 current = _context.Velocity;
            Vector3 target;
            target.x = moveInputWorld.x * horizontalSpeed;
            target.y = current.y;
            target.z = moveInputWorld.z * horizontalSpeed;

            current.x = Mathf.Lerp(current.x, target.x, _context.playerConfig.speedChangeDamping * Time.fixedDeltaTime);
            current.z = Mathf.Lerp(current.z, target.z, _context.playerConfig.speedChangeDamping * Time.fixedDeltaTime);

            _context.Velocity = current;
            _movementComponent.Velocity = current;

            // Face movement direction while airborne.
            Vector3 flatDir = new Vector3(current.x, 0f, current.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                _context.FaceDirection(flatDir);
            }
        }
    }
}
