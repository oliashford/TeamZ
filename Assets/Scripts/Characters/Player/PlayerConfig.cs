using UnityEngine;

namespace TeamZ.Characters.Player
{
    [CreateAssetMenu(menuName = "TeamZ/Configurations/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Locomotion Tuning")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;
        [SerializeField, Min(0f)] private float _runSpeed = 2.5f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 7f;

        [Tooltip("How quickly the character accelerates/decelerates")]
        [SerializeField, Min(0f)] private float _speedChangeDamping = 10f;

        [Tooltip("How quickly the character rotates to face the target direction")]
        [SerializeField, Min(0f)] private float _rotationSmoothing = 10f;

        [Tooltip("When true, character always faces camera direction instead of movement direction")]
        [SerializeField] private bool _alwaysStrafe = true;

        [Tooltip("Minimum angle threshold for forward strafe blending")]
        [SerializeField] private float _forwardStrafeMinThreshold = -55f;

        [Tooltip("Maximum angle threshold for forward strafe blending")]
        [SerializeField] private float _forwardStrafeMaxThreshold = 125f;

        [Header("Capsule / Crouch")]
        [Tooltip("Character capsule height when standing")]
        [SerializeField, Min(0f)] private float _capsuleStandingHeight = 1.8f;

        [Tooltip("Character capsule center Y offset when standing")]
        [SerializeField] private float _capsuleStandingCentre = 0.93f;

        [Tooltip("Character capsule height when crouching")]
        [SerializeField, Min(0f)] private float _capsuleCrouchingHeight = 1.2f;

        [Tooltip("Character capsule center Y offset when crouching")]
        [SerializeField] private float _capsuleCrouchingCentre = 0.6f;

        [Header("Airborne Settings (from legacy PlayerAnimationController)")]
        [Tooltip("Initial upward velocity applied when jumping")]
        [SerializeField] private float _jumpForce = 10f;

        [Tooltip("Multiplier applied to gravity when falling")]
        [SerializeField, Min(0f)] private float _gravityMultiplier = 2f;

        [Header("Climb Settings")]
        [Tooltip("How far forward from the ledge to place the character after climbing")]
        [SerializeField] private float _ledgeForwardOffset = 0.4f;

        // Public read-only properties keep the previous API surface used by PlayerController
        public float walkSpeed => _walkSpeed;
        public float runSpeed => _runSpeed;
        public float sprintSpeed => _sprintSpeed;
        public float speedChangeDamping => _speedChangeDamping;
        public float rotationSmoothing => _rotationSmoothing;
        public bool alwaysStrafe => _alwaysStrafe;
        public float forwardStrafeMinThreshold => _forwardStrafeMinThreshold;
        public float forwardStrafeMaxThreshold => _forwardStrafeMaxThreshold;

        public float capsuleStandingHeight => _capsuleStandingHeight;
        public float capsuleStandingCentre => _capsuleStandingCentre;
        public float capsuleCrouchingHeight => _capsuleCrouchingHeight;
        public float capsuleCrouchingCentre => _capsuleCrouchingCentre;

        public float jumpForce => _jumpForce;
        public float gravityMultiplier => _gravityMultiplier;

        public float ledgeForwardOffset => _ledgeForwardOffset;

        private void OnValidate()
        {
            // Basic sanity checks to catch obvious misconfiguration early in editor.
            if (_walkSpeed < 0f) Debug.LogError("PlayerConfig.walkSpeed must be >= 0", this);
            if (_runSpeed < 0f) Debug.LogError("PlayerConfig.runSpeed must be >= 0", this);
            if (_sprintSpeed < 0f) Debug.LogError("PlayerConfig.sprintSpeed must be >= 0", this);
            if (_speedChangeDamping < 0f) Debug.LogError("PlayerConfig.speedChangeDamping must be >= 0", this);
            if (_rotationSmoothing < 0f) Debug.LogError("PlayerConfig.rotationSmoothing must be >= 0", this);
            if (_capsuleStandingHeight <= 0f) Debug.LogError("PlayerConfig.capsuleStandingHeight must be > 0", this);
            if (_capsuleCrouchingHeight <= 0f) Debug.LogError("PlayerConfig.capsuleCrouchingHeight must be > 0", this);
            if (_gravityMultiplier < 0f) Debug.LogError("PlayerConfig.gravityMultiplier must be >= 0", this);
        }
    }
}
