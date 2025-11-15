using System;
using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Simple, generic state machine for character logic.
    /// </summary>
    public class CharacterStateMachine
    {
        public ICharacterState CurrentState { get; private set; }

        public event Action<ICharacterState> OnStateChanged;

        public void SetState(ICharacterState newState)
        {
            if (newState == CurrentState)
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
            OnStateChanged?.Invoke(CurrentState);
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}
