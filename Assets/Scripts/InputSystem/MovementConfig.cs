using UnityEngine;

namespace TeamZ.InputSystem
{
    [CreateAssetMenu(menuName = "TeamZ/Configurations/MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Speed")]
        [Range(0f, 1f)]
        [Tooltip("Factor applied when walking (keyboard toggle). 0..1 where 1 == run speed")]
        public float walkSpeedFactor = 0.5f;

        [Tooltip("Multiplier applied when sprinting (applied separately from the normalized movement fraction)")]
        public float sprintMultiplier = 1.5f;

        [Header("Smoothing")]
        [Tooltip("How quickly movement speed moves toward the target (units per second)")]
        public float acceleration = 8f;

        [Tooltip("How quickly movement speed decelerates toward the target (units per second)")]
        public float deceleration = 10f;
    }
}