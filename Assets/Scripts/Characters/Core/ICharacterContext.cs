using UnityEngine;

namespace TeamZ.Characters.Core
{
    // Things any humanoid character (player or enemy) can expose: movement info, animator, climb, etc.
    public interface ICharacterContext
    {
        Animator animator { get; }
        Transform Transform { get; }

        // Movement
        Vector3 Velocity { get; set; }
        bool IsGrounded { get; }

        // Parameters commonly pushed to the animator
        float CurrentSpeed { get; }
        Vector3 MoveInputWorld { get; }

        // Utility
        void SnapToPosition(Vector3 worldPos);
        void FaceDirection(Vector3 worldDirection);
    }
}