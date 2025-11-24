using TeamZ.Characters.Core;

namespace TeamZ.Characters.Player.States.Interaction
{
    public class InteractionIdleState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;

        public InteractionIdleState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;
        }

        public void Enter()
        {
        }

        public void Tick()
        {
            // Placeholder for interaction checks (use, push/pull, start climb) that may request movement locks.
        }

        public void FixedTick()
        {
            // No physics logic needed in InteractionIdleState.
        }

        public void Exit()
        {
        }
    }
}
