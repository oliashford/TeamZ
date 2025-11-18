using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player.States
{
    /// <summary>
    /// Basic climb state for the player. Assumes a climb animation is playing in the Animator
    /// and simply keeps the character in place until the animation signals completion.
    /// This is a simplified port of the legacy climb behaviour; more detailed root-motion
    /// syncing can be added later.
    /// </summary>
    public class ClimbState : ICharacterState
    {
        private readonly PlayerContext _playerContext;
        private readonly MovementComponent _motor;
        private readonly Animator _animator;
        private readonly CharacterStateMachine _stateMachine;
        private readonly PlayerController _owner;

        private readonly Vector3 _ledgePosition;
        private readonly Vector3 _ledgeNormal;
        private readonly int _climbType;

        private readonly int _isClimbingHash = Animator.StringToHash("IsClimbing");
        private readonly int _climbTypeHash = Animator.StringToHash("ClimbType");

        // Legacy used tags/states; here we assume a Climb state on base layer tagged "Climb".
        private const string ClimbStateTag = "Climb";

        private float _climbStateTime;

        public ClimbState(
            PlayerContext context,
            MovementComponent motor,
            CharacterStateMachine stateMachine,
            PlayerController owner,
            Vector3 ledgePosition,
            Vector3 ledgeNormal,
            int climbType)
        {
            _playerContext = context;
            _motor = motor;
            _stateMachine = stateMachine;
            _owner = owner;
            _animator = context.Animator;

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
            Vector3 vel = _playerContext.Velocity;
            vel.x = 0f;
            vel.y = 0f;
            vel.z = 0f;
            _playerContext.Velocity = vel;
            _motor.Velocity = vel;
            _owner?.DeactivateSliding();

            // Face inward toward the ledge.
            if (_ledgeNormal != Vector3.zero)
            {
                Vector3 forward = -_ledgeNormal;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    _playerContext.Transform.rotation = Quaternion.LookRotation(forward);
                }
            }

            // Clear locomotion-style input flags so we don't look "stuck" in a move blend tree.
            _animator.SetBool("MovementInputTapped", false);
            _animator.SetBool("MovementInputPressed", false);
            _animator.SetBool("MovementInputHeld", false);

            // Tell Animator we are in climbing state and which variant to use.
            _animator.SetBool(_isClimbingHash, true);
            _animator.SetInteger(_climbTypeHash, _climbType);
        }

        public void Tick()
        {
            _climbStateTime += Time.deltaTime;

            // Keep the character roughly anchored to the ledge XZ while animation plays.
            Vector3 pos = _playerContext.Transform.position;
            pos.x = Mathf.Lerp(pos.x, _ledgePosition.x, 10f * Time.deltaTime);
            pos.z = Mathf.Lerp(pos.z, _ledgePosition.z, 10f * Time.deltaTime);
            _playerContext.Transform.position = pos;

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

        private void FinishClimb()
        {
            _animator.SetBool(_isClimbingHash, false);

            // Place character on top of the obstacle, mirroring legacy FinishClimb logic.
            CharacterController controller = _playerContext.Transform.GetComponent<CharacterController>();
            float capsuleHeight = controller != null ? controller.height : 2f;
            float forwardOffset = _owner != null ? _owner.LedgeForwardOffset : 0.4f;

            Vector3 forwardOnLedge = new Vector3(_playerContext.Transform.forward.x, 0f, _playerContext.Transform.forward.z).normalized;
            Vector3 targetPosition = _ledgePosition + forwardOnLedge * forwardOffset;

            // Stand capsule on top.
            targetPosition.y += capsuleHeight * 0.5f;

            if (controller != null)
            {
                Vector3 delta = targetPosition - _playerContext.Transform.position;
                controller.Move(delta);
            }
            else
            {
                _playerContext.Transform.position = targetPosition;
            }

            PlayerLocomotionState locomotion = new PlayerLocomotionState(
                _playerContext,
                _motor,
                _owner.WalkSpeed,
                _owner.RunSpeed,
                _owner.SprintSpeed,
                _owner.SpeedChangeDamping,
                _owner.RotationSmoothing,
                _owner.AlwaysStrafe,
                _owner.ForwardStrafeMinThreshold,
                _owner.ForwardStrafeMaxThreshold,
                _owner);

            _stateMachine.SetState(locomotion);
        }

        public void Exit()
        {
            _animator.SetBool(_isClimbingHash, false);
        }
    }
}
