using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Movement
{
    /// <summary>
    /// Minimal placeholder combat idle state. Extend this with aiming/firing/reload logic.
    /// </summary>
    public class MovementIdleState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;

        public MovementIdleState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;
         }

        public void Enter()
        {
        }

        public void Tick()
        {
            // No logic here; input callbacks drive transitions.
        }

        public void FixedTick()
        {
            // No fixed update logic needed in MovementIdleState.
        }

        public void Exit()
        {
        }
    }
}
