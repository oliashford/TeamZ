using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Reusable grounded check component for any character.
    /// </summary>
    public class GroundDetectorComponent : MonoBehaviour
    {
        [Tooltip("Radius of the check sphere (usually match CharacterController radius).")]
        [SerializeField]
        private float _groundCheckRadius = 0.25f;

        [Tooltip("Vertical offset from the character pivot down to where ground is tested.")]
        [SerializeField]
        private float _groundCheckOffset = 0.2f;

        [Tooltip("Layers that count as ground.")]
        [SerializeField]
        private LayerMask _groundMask;

        /// <summary>True when the character is currently grounded.</summary>
        public bool IsGrounded { get; private set; }

        private void Update()
        {
            Vector3 origin = transform.position;
            origin.y -= _groundCheckOffset;

            IsGrounded = Physics.CheckSphere(origin, _groundCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = transform.position;
            origin.y -= _groundCheckOffset;
            Gizmos.DrawWireSphere(origin, _groundCheckRadius);
        }
    }
}
