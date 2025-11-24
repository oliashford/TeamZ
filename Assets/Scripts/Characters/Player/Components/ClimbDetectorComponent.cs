using UnityEngine;

namespace TeamZ.Characters.Player.Components
{
    /// <summary>
    /// High-level, reusable climb probe. For now this is standalone and not wired
    /// to your PlayerAnimationController climb logic, but the API mirrors what
    /// you're already using so we can hook it up later.
    /// </summary>
    public class ClimbDetectorComponent : MonoBehaviour
    {
        [Tooltip("Origin for the forward climb ray (e.g. chest height)")]
        [SerializeField]
        private Transform _rayOrigin;

        [Tooltip("How far forward to probe for climbable obstacles")]
        [SerializeField]
        private float _probeDistance = 1.0f;

        [Tooltip("Minimum height above feet for a ledge to be climbable")]
        [SerializeField]
        private float _minHeight = 0.3f;

        [Tooltip("Maximum height above feet for a low climb/vault")]
        [SerializeField]
        private float _maxLowHeight = 1.2f;

        [Tooltip("Maximum height above feet for a high climb (beyond this is mantle)")]
        [SerializeField]
        private float _maxHighHeight = 2.0f;

        [SerializeField]
        private LayerMask _climbMask;

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
        public bool TryDetectClimb(float feetY, out ClimbKind climbKind, out Vector3 ledgePosition, out Vector3 ledgeNormal) 
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
                if (_debugDraw)
                {
                    Debug.Log("TryDetectClimb: no front hit.");
                }

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
                if (_debugDraw)
                {
                    Debug.Log("TryDetectClimb: no top hit.");
                }

                return false;
            }

            float ledgeHeight = topHit.point.y - feetY;

            if (_debugDraw)
            {
                Debug.Log($"TryDetectClimb: ledgeHeight={ledgeHeight:0.00}, feetY={feetY:0.00}, topY={topHit.point.y:0.00}");
            }

            if (ledgeHeight < _minHeight)
            {
                if (_debugDraw)
                {
                    Debug.Log($"TryDetectClimb: height below min ({ledgeHeight:0.00} < {_minHeight:0.00}).");
                }

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
                Debug.Log($"TryDetectClimb: SUCCESS - kind={climbKind}, ledgePos={ledgePosition}, ledgeNormal={ledgeNormal}");
            }

            return true;
        }
    }
}
