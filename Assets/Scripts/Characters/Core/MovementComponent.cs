using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Thin wrapper over CharacterController that exposes a clean movement API.
    /// This lets Player and Enemy share the same movement primitive.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MovementComponent : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _characterController;

        /// <summary>Current velocity used for movement and gravity.</summary>
        public Vector3 Velocity { get; set; }

        public CharacterController Controller
        {
            get
            {
                if (_characterController == null)
                {
                    _characterController = GetComponent<CharacterController>();
                }

                return _characterController;
            }
        }

        private void Reset()
        {
            _characterController = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Moves the character using the Unity CharacterController.
        /// </summary>
        public void Move(Vector3 delta)
        {
            Controller.Move(delta);
        }
    }
}
