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
        private CharacterMovementComponent _motor;

        private CharacterStateMachine _stateMachine;

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<CharacterMovementComponent>();
            }

            _stateMachine = new CharacterStateMachine();
        }

        private void Update()
        {
            _stateMachine.Tick();
        }
    }
}
