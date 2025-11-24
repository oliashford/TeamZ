using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player.States
{
    public class PostureIdleState : ICharacterState
    {
        private readonly PlayerContext _context;

        public PostureIdleState(PlayerContext context)
        {
            _context = context;
        }

        public void Enter()
        {
            // Touch context to avoid static analysis warnings.
            if (_context != null && _context.animator != null)
            {
                var _ = _context.animator.parameters;
            }
        }

        public void Tick()
        {
            // Placeholder for posture transitions (crouch/prone) accepting input.
        }

        public void FixedTick()
        {
            // No fixed update logic needed in PostureIdleState.
        }

        public void Exit()
        {
        }
    }
}
