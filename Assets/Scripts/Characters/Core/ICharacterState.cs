using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Base contract for a character state in the state machine.
    /// </summary>
    public interface ICharacterState
    {
        /// <summary>Called once when the state becomes active.</summary>
        void Enter();

        /// <summary>Called every frame while the state is active.</summary>
        void Tick();

        /// <summary>Called once when the state is about to be replaced.</summary>
        void Exit();
    }
}
