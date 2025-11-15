using TeamZ.Characters.Core;
using UnityEngine;

namespace TeamZ.Characters.Enemy
{
    /// <summary>
    /// Placeholder enemy controller that will eventually drive AI via the same core
    /// systems (motor, detectors, state machine) as the player.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [Tooltip("Motor used for enemy movement (shared implementation with player).")]
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

        private void Update()
        {
            _stateMachine.Tick();
        }
    }
}
