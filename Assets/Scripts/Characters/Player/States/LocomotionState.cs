using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player.States
{
    /// <summary>
    /// Placeholder locomotion state. This is where we will gradually migrate the
    /// walk/run/strafe logic from PlayerAnimationController.
    /// </summary>
    public class LocomotionState : ICharacterState
    {
        private readonly PlayerController _player;
        private readonly CharacterMovementComponent _motor;

        public LocomotionState(PlayerController player, CharacterMovementComponent motor)
        {
            _player = player;
            _motor = motor;
        }

        public void Enter()
        {
            // TODO: initialise locomotion state (hook input, reset timers etc.)
        }

        public void Tick()
        {
            // TODO: move and rotate character, update animator.
        }

        public void Exit()
        {
            // TODO: unhook input if needed.
        }
    }
}
