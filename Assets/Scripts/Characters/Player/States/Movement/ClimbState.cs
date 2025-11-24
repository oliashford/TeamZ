using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Movement
{
    public class ClimbState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly MovementComponent _movementComponent;
        private readonly Animator _animator;
        private readonly CharacterStateMachine _stateMachine;
        private readonly PlayerController _owner;

        private readonly Vector3 _ledgePosition;
        private readonly Vector3 _ledgeNormal;
        private readonly int _climbType;

        private const string ParamIsClimbing = "IsClimbing";
        private const string ParamClimbType = "ClimbType";

        // Legacy used tags/states; here we assume a Climb state on base layer tagged "Climb".
        private const string ClimbStateTag = "Climb";

        private float _climbStateTime;

        public ClimbState(PlayerContext context, PlayerController owner, Vector3 ledgePosition, Vector3 ledgeNormal, int climbType)
        {
            _context = context;
            _owner = owner;
            
            _movementComponent = _context.movement;
            _animator = _context.animator;

            _ledgePosition = ledgePosition;
            _ledgeNormal = ledgeNormal;
            _climbType = climbType;
        }

        public void Enter()
        {
            _climbStateTime = 0f;

            // Mirror legacy EnterClimbState debug output
            Debug.Log("EnterClimbState");

            // Zero full velocity and stop sliding
            Vector3 vel = _context.Velocity;
            
            vel.x = 0f;
            vel.y = 0f;
            vel.z = 0f;
            
            _context.Velocity = vel;
            _movementComponent.Velocity = vel;
            _owner?.DeactivateSliding();

            // Face inward toward the ledge.
            if (_ledgeNormal != Vector3.zero)
            {
                Vector3 forward = -_ledgeNormal;
                forward.y = 0f;
                
                if (forward.sqrMagnitude > 0.0001f)
                {
                    _context.Transform.rotation = Quaternion.LookRotation(forward);
                }
            }

            // Clear locomotion-style input flags so we don't look "stuck" in a move blend tree.
            _animator.SetBool("MovementInputTapped", false);
            _animator.SetBool("MovementInputPressed", false);
            _animator.SetBool("MovementInputHeld", false);

            // Tell Animator we are in climbing state and which variant to use.
            _animator.SetBool(ParamIsClimbing, true);
            _animator.SetFloat(ParamClimbType, _climbType);
        }

        public void Tick()
        {
            _climbStateTime += Time.deltaTime;

            // Keep the character roughly anchored to the ledge XZ while animation plays.
            Vector3 pos = _context.Transform.position;
            pos.x = Mathf.Lerp(pos.x, _ledgePosition.x, 10f * Time.deltaTime);
            pos.z = Mathf.Lerp(pos.z, _ledgePosition.z, 10f * Time.deltaTime);
            _context.Transform.position = pos;

            // When the climb animation is complete, finish and return to locomotion.
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

            bool legitFinish =
                _climbStateTime > 0.1f &&
                state.IsTag(ClimbStateTag) &&
                state.normalizedTime >= 0.98f;

            if (legitFinish)
            {
                FinishClimb();
            }
        }

        public void FixedTick()
        {
            // No physics to update during climb; anchoring is handled per-frame in Tick().
        }

        private void FinishClimb()
        {
            _animator.SetBool(ParamIsClimbing, false);

            // Place character on top of the obstacle, mirroring legacy FinishClimb logic.
            CharacterController controller = _context.Transform.GetComponent<CharacterController>();
            float capsuleHeight = controller != null ? controller.height : 2f;
            float forwardOffset = _owner != null ? _owner.LedgeForwardOffset : 0.4f;

            Vector3 forwardOnLedge = new Vector3(_context.Transform.forward.x, 0f, _context.Transform.forward.z).normalized;
            Vector3 targetPosition = _ledgePosition + forwardOnLedge * forwardOffset;

            // Stand capsule on top.
            targetPosition.y += capsuleHeight * 0.5f;

            if (controller != null)
            {
                Vector3 delta = targetPosition - _context.Transform.position;
                controller.Move(delta);
            }
            else
            {
                _context.Transform.position = targetPosition;
            }

            MoveState move = new MoveState(_context,_owner);

            _stateMachine.SetState(move);
        }

        public void Exit()
        {
            _animator.SetBool(ParamIsClimbing, false);
        }
    }
}
