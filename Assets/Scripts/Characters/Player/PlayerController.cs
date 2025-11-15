using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    /// <summary>
    /// High-level player component that owns the state machine and wires inputs to states.
    /// NOTE: Right now this is just scaffolding; your existing PlayerAnimationController
    /// in Core/PlayerLegacy still drives live gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Optional motor component used for movement. If null, one is fetched from this GameObject.")]
        [SerializeField]
        private CharacterMotorComponent _motor;

        private CharacterStateMachine _stateMachine;

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<CharacterMotorComponent>();
            }

            _stateMachine = new CharacterStateMachine();
        }

        private void Start()
        {
            // Entry state placeholder. In a later step we will construct a LocomotionState here.
            //_stateMachine.SetState(new LocomotionState(this, _motor));
        }

        private void Update()
        {
            _stateMachine.Tick();
        }
    }
}
