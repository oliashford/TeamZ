namespace TeamZ.Characters.Core
{
    // Base contract for a character state in the state machine.
    public interface ICharacterState
    {
        // Called once when the state becomes active.
        void Enter();

        // Called every frame while the state is active.
        void Tick();
        
        // Called from FixedUpdate (physics step) while the state is active.
        void FixedTick();

        // Called once when the state is about to be replaced.
        void Exit();
    }
}
