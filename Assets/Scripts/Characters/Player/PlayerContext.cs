using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerContext : MonoBehaviour, ICharacterContext
    {
        [Header("External")]
        [SerializeField] private Animator _animator;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private ClimbDetectorComponent _climbDetectorComponent;
        [SerializeField] private GroundDetectorComponent _groundDetectorComponent;

        private CharacterController _controller;
        private ClimbDetectorComponent _climbDetector;
        private CharacterMovementComponent _movement;
        private GroundDetectorComponent _groundDetector;

        public Animator Animator => _animator;
        public Transform Transform => transform;

        public Vector3 Velocity
        {
            get => _movement != null ? _movement.Velocity : Vector3.zero;
            set
            {
                if (_movement != null)
                {
                    _movement.Velocity = value;
                }
            }
        }

        public bool IsGrounded => _groundDetector != null ? _groundDetector.IsGrounded : _controller.isGrounded;

        /// <summary>Incline angle from GroundDetectorComponent, or 0 if none present.</summary>
        public float InclineAngle => _groundDetector != null ? _groundDetector.InclineAngle : 0f;

        /// <summary>True when there is not enough headroom to stand (ceiling check).</summary>
        public bool CannotStandUp => _groundDetector != null && _groundDetector.CannotStandUp;

        public ClimbDetectorComponent ClimbDetectorComponent => _climbDetector;

        public float CurrentSpeed => new Vector3(Velocity.x, 0f, Velocity.z).magnitude;

        public Vector3 MoveInputWorld
        {
            get
            {
                if (_inputReader == null || _cameraController == null)
                {
                    return Vector3.zero;
                }

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
            _movement = GetComponent<CharacterMovementComponent>();

            // Prefer explicitly assigned GroundDetectorComponent, otherwise search children.
            _groundDetector = _groundDetectorComponent != null
                ? _groundDetectorComponent
                : GetComponentInChildren<GroundDetectorComponent>();
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
