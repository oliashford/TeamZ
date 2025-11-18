using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Enemy
{
    // Placeholder enemy controller that will eventually drive AI via the same core
    // systems (motor, detectors, state machine) as the player.
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField]
        private MovementComponent _motor;

        private CharacterStateMachine _stateMachine;

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<MovementComponent>();
            }

            _stateMachine = new CharacterStateMachine();
        }

        private void Update()
        {
            _stateMachine.Tick();
        }
    }
}
