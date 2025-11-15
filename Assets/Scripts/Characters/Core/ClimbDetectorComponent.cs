using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// High-level, reusable climb probe. For now this is standalone and not wired
    /// to your PlayerAnimationController climb logic, but the API mirrors what
    /// you're already using so we can hook it up later.
    /// </summary>
    public class ClimbDetectorComponent : MonoBehaviour
    {
        [Tooltip("Origin for the forward climb ray (e.g. chest height).")]
        [SerializeField]
        private Transform _rayOrigin;

        [Tooltip("How far forward to probe for climbable obstacles.")]
        [SerializeField]
        private float _probeDistance = 1.0f;

        [Tooltip("Minimum height above feet for a ledge to be climbable.")]
        [SerializeField]
        private float _minHeight = 0.3f;

        [Tooltip("Maximum height above feet for a low climb / vault.")]
        [SerializeField]
        private float _maxLowHeight = 1.2f;

        [Tooltip("Maximum height above feet for a high climb (beyond this is mantle or too high).")]
        [SerializeField]
        private float _maxHighHeight = 2.0f;

        [Tooltip("Layers that are considered climbable surfaces.")]
        [SerializeField]
        private LayerMask _climbMask;

        [Tooltip("Enable debug drawing for climb probes.")]
        [SerializeField]
        private bool _debugDraw;

        public enum ClimbKind
        {
            None = 0,
            Low = 1,
            High = 2,
            Mantle = 3
        }

        /// <summary>
        /// Performs a climb probe and returns the result.
        /// </summary>
        public bool TryDetectClimb(
            float feetY,
            out ClimbKind climbKind,
            out Vector3 ledgePosition,
            out Vector3 ledgeNormal)
        {
            climbKind = ClimbKind.None;
            ledgePosition = Vector3.zero;
            ledgeNormal = Vector3.zero;

            Vector3 origin = _rayOrigin != null ? _rayOrigin.position : transform.position + Vector3.up * 1.0f;
            Vector3 direction = transform.forward;

            if (_debugDraw)
            {
                Debug.DrawRay(origin, direction * _probeDistance, Color.cyan);
            }

            if (!Physics.Raycast(origin, direction, out RaycastHit frontHit, _probeDistance, _climbMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float maxHeight = 4.0f;
            Vector3 topOrigin = new Vector3(frontHit.point.x, transform.position.y + maxHeight, frontHit.point.z);

            if (_debugDraw)
            {
                Debug.DrawRay(topOrigin, Vector3.down * (maxHeight + 1f), Color.yellow);
            }

            if (!Physics.Raycast(topOrigin, Vector3.down, out RaycastHit topHit, maxHeight + 1f, _climbMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float ledgeHeight = topHit.point.y - feetY;

            if (ledgeHeight < _minHeight)
            {
                return false;
            }

            if (ledgeHeight <= _maxLowHeight)
            {
                climbKind = ClimbKind.Low;
            }
            else if (ledgeHeight <= _maxHighHeight)
            {
                climbKind = ClimbKind.High;
            }
            else
            {
                climbKind = ClimbKind.Mantle;
            }

            ledgePosition = topHit.point;
            ledgeNormal = frontHit.normal;

            if (_debugDraw)
            {
                Debug.DrawRay(ledgePosition, Vector3.up * 0.3f, Color.green);
            }

            return true;
        }
    }
}
