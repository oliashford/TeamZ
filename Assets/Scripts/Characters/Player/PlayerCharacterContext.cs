using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacterContext : MonoBehaviour, ICharacterContext
    {
        [Header("External")]
        [SerializeField] private Animator _animator;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private ClimbDetectorComponent _climbDetectorComponent; 

        private CharacterController _controller;
        private ClimbDetectorComponent _climbDetector;

        public Animator Animator => _animator;
        public Transform Transform => transform;
        public Vector3 Velocity { get; set; }
        public bool IsGrounded => _controller.isGrounded;
        
        public ClimbDetectorComponent ClimbDetectorComponent => _climbDetector;

        public float CurrentSpeed => new Vector3(Velocity.x, 0f, Velocity.z).magnitude;

        public Vector3 MoveInputWorld
        {
            get
            {
                // Example using your camera-relative input
                Vector2 move = _inputReader._moveComposite;
                Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
                Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
                return camFwd * move.y + camRight * move.x;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _climbDetector = _climbDetectorComponent;
        }

        public void SnapToPosition(Vector3 worldPos)
        {
            _controller.enabled = false;
            transform.position = worldPos;
            _controller.enabled = true;
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            }
        }

        public InputReader InputReader => _inputReader;
        public CameraController CameraController => _cameraController;
    }
}
