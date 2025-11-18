using UnityEngine;

namespace Utils
{
    [ExecuteAlways]
    public class HandIkGizmo : MonoBehaviour
    {
        public enum HandType
        {
            Left,
            Right
        }

        [Header("Hand IK Helper")]
        [SerializeField] private HandType _handType = HandType.Right;

        [Tooltip("Gizmo display size")]
        [SerializeField] private float _size = 0.1f;
        [SerializeField] private Color _color = Color.cyan;
        
        [Tooltip("Draw local axes (RGB = XYZ)")]
        [SerializeField] private bool _drawAxes = true;
        
        [Tooltip("Draw gizmo even when object is not selected")]
        [SerializeField] private bool _drawWhenNotSelected = true;

        private void OnDrawGizmos()
        {
            if (!_drawWhenNotSelected)
            {
                return;
            }

            DrawHandGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            // Always draw when selected
            if (!_drawWhenNotSelected)
            {
                DrawHandGizmo();
            }
        }

        private void DrawHandGizmo()
        {
            // Scene view / editor only (Gizmos never show in builds)
            Gizmos.color = _color;

            // Palm
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // A little "palm" box
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_size, _size * 0.2f, _size));

            // Draw "fingers" as lines to show orientation
            float fingerLength = _size * 0.7f;
            int fingerCount = 4;
            float spread = _size * 0.3f;

            for (int i = 0; i < fingerCount; i++)
            {
                float offset = (i - (fingerCount - 1) * 0.5f) * spread;
                Vector3 fingerBase = new Vector3(offset, 0f, _size * 0.5f);
                Vector3 fingerTip = fingerBase + Vector3.forward * fingerLength;
                Gizmos.DrawLine(fingerBase, fingerTip);
            }

            // Thumb (different side for left/right)
            float thumbSide = _handType == HandType.Right ? 1f : -1f;
            Vector3 thumbBasePos = new Vector3(thumbSide * _size * 0.6f, 0f, 0f);
            Vector3 thumbTipPos = thumbBasePos + (Vector3.forward + Vector3.right * thumbSide) * (fingerLength * 0.7f);
            Gizmos.DrawLine(thumbBasePos, thumbTipPos);

            if (_drawAxes)
            {
                // Little local axes to show rotation
                float axisLen = _size * 0.8f;

                Gizmos.color = Color.red; // X
                Gizmos.DrawLine(Vector3.zero, Vector3.right * axisLen);

                Gizmos.color = Color.green; // Y
                Gizmos.DrawLine(Vector3.zero, Vector3.up * axisLen);

                Gizmos.color = Color.blue; // Z
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * axisLen);

                Gizmos.color = _color;
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}