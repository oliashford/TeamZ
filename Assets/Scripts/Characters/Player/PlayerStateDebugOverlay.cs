using UnityEngine;

namespace TeamZ.Characters.Player
{
    [RequireComponent(typeof(PlayerController), typeof(PlayerContext))]
    public class PlayerStateDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        
        // Not serialized - fixed style/colors
        private GUIStyle _style;

        // Fixed background color (black) and style: white text, size 16
        private readonly Color _backgroundColor = Color.black;

        // Runtime-only 1x1 texture used for the GUI background
        private Texture2D _backgroundTexture;

        private PlayerController _controller;
        private PlayerContext _context;

        private void Awake()
        {
            TryGetComponent(out _controller);
            TryGetComponent(out _context);

            // Always create our own GUIStyle instance so it's not shared or serialized
            _style = new GUIStyle();
            _style.normal.textColor = Color.white;
            _style.fontSize = 24;

            // Ensure background texture exists and assign it
            EnsureBackgroundTexture();
            if (_backgroundTexture != null)
            {
                _style.normal.background = _backgroundTexture;
            }
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
                _backgroundTexture = null;
            }
        }

        private void EnsureBackgroundTexture()
        {
            if (_backgroundTexture == null)
                RecreateBackgroundTexture();
        }

        private void RecreateBackgroundTexture()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }

            _backgroundTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            _backgroundTexture.SetPixel(0, 0, _backgroundColor);
            _backgroundTexture.Apply();
            _backgroundTexture.hideFlags = HideFlags.DontSave;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            
            if (_controller == null || _context == null) return;

            string locomotion = _controller.CurrentLocomotionStateName;
            string combat = _context.CombatStateMachine?.CurrentState?.GetType().Name ?? "None";
            string posture = _context.PostureStateMachine?.CurrentState?.GetType().Name ?? "None";
            string cover = _context.CoverStateMachine?.CurrentState?.GetType().Name ?? "None";
            string interaction = _context.InteractionStateMachine?.CurrentState?.GetType().Name ?? "None";
            string movementLocked = _context.IsMovementLocked ? "LOCKED" : "Free";

            GUILayout.BeginArea(new Rect(10, 10, 600, 400), GUIContent.none, _style);
            GUILayout.Label($"Locomotion: {locomotion}", _style);
            GUILayout.Label($"Combat: {combat}", _style);
            GUILayout.Label($"Posture: {posture}", _style);
            GUILayout.Label($"Cover: {cover}", _style);
            GUILayout.Label($"Interaction: {interaction}", _style);
            GUILayout.Label($"Movement: {movementLocked}", _style);
            GUILayout.EndArea();
        }
    }
}
