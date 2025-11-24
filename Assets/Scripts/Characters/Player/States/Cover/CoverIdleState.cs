using TeamZ.Characters.Core;

namespace TeamZ.Characters.Player.States.Cover
{
    public class CoverIdleState : ICharacterState
    {
        private readonly PlayerContext _context;

        public CoverIdleState(PlayerContext context)
        {
            _context = context;
        }

        public void Enter()
        {
        }

        public void Tick()
        {
            // Placeholder for cover detection/enter logic.
        }

        public void FixedTick()
        {
            // No fixed update logic needed in CoverIdleState.
        }

        public void Exit()
        {
        }
    }
}
