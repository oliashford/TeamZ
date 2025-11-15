using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States
{
    /// <summary>
    /// Handles jump and fall behaviour for the player, migrated from
    /// PlayerAnimationController while preserving timing and animator usage.
    /// </summary>
    public class PlayerAirborneState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly CharacterMovementComponent _motor;
        private readonly Animator _animator;
        private readonly InputReader _input;
        private readonly PlayerController _owner;
        private readonly CharacterStateMachine _stateMachine;

        private readonly float _jumpForce;
        private readonly float _gravityMultiplier;

        private float _fallStartTime;
        private bool _isJumpingPhase;

        public PlayerAirborneState(
            PlayerContext context,
            CharacterMovementComponent motor,
            CharacterStateMachine stateMachine,
            float jumpForce,
            float gravityMultiplier,
            PlayerController owner)
        {
            _context = context;
            _motor = motor;
            _animator = context.Animator;
            _input = context.InputReader;
            _owner = owner;
            _stateMachine = stateMachine;

            _jumpForce = jumpForce;
            _gravityMultiplier = gravityMultiplier;
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
                UpdateJumpPhase();
            }
            else
            {
                UpdateFallPhase();
            }
        }

        public void Exit()
        {
            // Clear jump bool when leaving airborne.
            _animator.SetBool("IsJumping", false);
        }

        private void EnterJumpPhase()
        {
            _isJumpingPhase = true;

            // Raise the jump bool and apply upward force (mirrors legacy EnterJumpState).
            _animator.SetBool("IsJumping", true);

            Vector3 vel = _context.Velocity;
            vel.y = _jumpForce;
            _context.Velocity = vel;
            _motor.Velocity = vel;
        }

        private void UpdateJumpPhase()
        {
            // Apply gravity.
            ApplyGravity();

            Vector3 vel = _context.Velocity;

            // When upward velocity has gone non-positive, transition to falling.
            if (vel.y <= 0f)
            {
                _animator.SetBool("IsJumping", false);
                EnterFallPhase(resetVerticalVelocity: false);
                return;
            }

            // Horizontal movement is still controlled by the locomotion inputs.
            ApplyHorizontalMovement();

            _motor.Move(_motor.Velocity * Time.deltaTime);
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
            _motor.Velocity = vel;
        }

        private void UpdateFallPhase()
        {
            // Update falling duration animator parameter.
            float fallingDuration = Time.time - _fallStartTime;
            _animator.SetFloat("FallingDuration", fallingDuration);

            ApplyGravity();
            ApplyHorizontalMovement();

            _motor.Move(_motor.Velocity * Time.deltaTime);

            // When grounded again, return to locomotion.
            if (_context.IsGrounded)
            {
                _stateMachine.SetState(
                    new PlayerLocomotionState(
                        _context,
                        _motor,
                        _owner.WalkSpeed,
                        _owner.RunSpeed,
                        _owner.SprintSpeed,
                        _owner.SpeedChangeDamping,
                        _owner.RotationSmoothing,
                        _owner.AlwaysStrafe,
                        _owner.ForwardStrafeMinThreshold,
                        _owner.ForwardStrafeMaxThreshold,
                        _owner));
            }
        }

        private void ApplyGravity()
        {
            Vector3 vel = _context.Velocity;

            if (vel.y > Physics.gravity.y)
            {
                vel.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;

                _context.Velocity = vel;
                _motor.Velocity = vel;
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
            float horizontalSpeed = _owner.RunSpeed;

            Vector3 current = _context.Velocity;
            Vector3 target;
            target.x = moveInputWorld.x * horizontalSpeed;
            target.y = current.y;
            target.z = moveInputWorld.z * horizontalSpeed;

            current.x = Mathf.Lerp(current.x, target.x, _owner.SpeedChangeDamping * Time.deltaTime);
            current.z = Mathf.Lerp(current.z, target.z, _owner.SpeedChangeDamping * Time.deltaTime);

            _context.Velocity = current;
            _motor.Velocity = current;

            // Face movement direction while airborne.
            Vector3 flatDir = new Vector3(current.x, 0f, current.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                _context.FaceDirection(flatDir);
            }
        }
    }
}
