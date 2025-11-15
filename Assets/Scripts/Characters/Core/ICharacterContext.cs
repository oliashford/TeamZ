using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Things any humanoid character (player or enemy) can expose:
    /// movement info, animator, climb, etc.
    /// </summary>
    public interface ICharacterContext
    {
        Animator Animator { get; }
        Transform Transform { get; }

        // Movement
        Vector3 Velocity { get; set; }
        bool IsGrounded { get; }

        // High-level abilities
        ClimbDetectorComponent ClimbDetectorComponent { get; }

        // Parameters commonly pushed to the animator
        float CurrentSpeed { get; }
        Vector3 MoveInputWorld { get; }

        // Utility
        void SnapToPosition(Vector3 worldPos);
        void FaceDirection(Vector3 worldDirection);
    }
}