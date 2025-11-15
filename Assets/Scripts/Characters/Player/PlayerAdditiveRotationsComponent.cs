using UnityEngine;
using TeamZ.Characters.Core;

namespace TeamZ.Characters.Player
{
    /// <summary>
    /// Recreates the head look, body look and lean rotational additives
    /// from the legacy PlayerAnimationController, but as a standalone
    /// component driven by PlayerContext.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAdditiveRotationsComponent : MonoBehaviour
    {
        [Header("External")]
        [SerializeField] private PlayerContext _context;

        [Header("Settings (copied from PlayerAnimationController)")]
        [SerializeField] private bool _enableHeadTurn = true;
        [SerializeField] private float _headLookDelay;
        [SerializeField] private float _headLookX;
        [SerializeField] private float _headLookY;
        [SerializeField] private AnimationCurve _headLookXCurve;

        [SerializeField] private bool _enableBodyTurn = true;
        [SerializeField] private float _bodyLookDelay;
        [SerializeField] private float _bodyLookX;
        [SerializeField] private float _bodyLookY;
        [SerializeField] private AnimationCurve _bodyLookXCurve;

        [Header("Lean")]
        [SerializeField] private bool _enableLean = true;
        [SerializeField] private float _leanDelay;
        [SerializeField] private float _leanValue;
        [SerializeField] private AnimationCurve _leanCurve;
        [SerializeField] private float _leansHeadLooksDelay;

        private float _rotationRate;
        private float _initialLeanValue;
        private float _initialTurnValue;
        private Vector3 _currentRotation;
        private Vector3 _previousRotation;

        private Animator _animator;

        private int _leanValueHash;
        private int _headLookXHash;
        private int _headLookYHash;
        private int _bodyLookXHash;
        private int _bodyLookYHash;

        private void Awake()
        {
            if (_context == null)
            {
                _context = GetComponent<PlayerContext>();
            }

            _animator = _context != null ? _context.Animator : GetComponentInChildren<Animator>();

            _leanValueHash = Animator.StringToHash("LeanValue");
            _headLookXHash = Animator.StringToHash("HeadLookX");
            _headLookYHash = Animator.StringToHash("HeadLookY");
            _bodyLookXHash = Animator.StringToHash("BodyLookX");
            _bodyLookYHash = Animator.StringToHash("BodyLookY");

            _previousRotation = transform.forward;
        }

        private void Update()
        {
            if (_animator == null || _context == null || _context.CameraController == null)
            {
                return;
            }

            // Delays (mirrors VariableOverrideDelayTimer usage)
            _headLookDelay = VariableOverrideDelayTimer(_headLookDelay);
            _bodyLookDelay = VariableOverrideDelayTimer(_bodyLookDelay);
            _leanDelay = VariableOverrideDelayTimer(_leanDelay);

            bool canHeadTurn = _headLookDelay == 0.0f;
            bool canBodyTurn = _bodyLookDelay == 0.0f;
            bool canLean = _leanDelay == 0.0f;

            CalculateRotationalAdditives(canLean && _enableLean, canHeadTurn && _enableHeadTurn, canBodyTurn && _enableBodyTurn);

            // Push into animator
            _animator.SetFloat(_leanValueHash, _leanValue);
            _animator.SetFloat(_headLookXHash, _headLookX);
            _animator.SetFloat(_headLookYHash, _headLookY);
            _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            _animator.SetFloat(_bodyLookYHash, _bodyLookY);
        }

        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            if (headLookActivated || leansActivated || bodyLookActivated)
            {
                _currentRotation = transform.forward;

                _rotationRate = _currentRotation != _previousRotation
                    ? Vector3.SignedAngle(_currentRotation, _previousRotation, Vector3.up) / Time.deltaTime * -1f
                    : 0f;
            }

            // Lean
            _initialLeanValue = leansActivated ? _rotationRate : 0f;
            float leanSmoothness = 5f;
            float maxLeanRotationRate = 275.0f;

            // Legacy uses speed / sprintSpeed as reference; here we approximate using current horizontal speed
            float speed2D = _context.CurrentSpeed;
            float sprintSpeed = Mathf.Max(0.01f, (_context != null ? _context.GetComponent<PlayerController>()?.SprintSpeed ?? 7f : 7f));
            float referenceValue = speed2D / sprintSpeed;
            _leanValue = CalculateSmoothedValue(
                _leanValue,
                _initialLeanValue,
                maxLeanRotationRate,
                leanSmoothness,
                _leanCurve,
                referenceValue,
                true
            );

            float headTurnSmoothness = 5f;

            // When turning in place, headLookX is driven by camera rotation offset.
            // We approximate _cameraRotationOffset by angle between character forward and camera forward.
            Vector3 characterForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
            Vector3 cameraForward = _context.CameraController.GetCameraForwardZeroedYNormalised();
            float cameraOffset = Vector3.SignedAngle(characterForward, cameraForward, Vector3.up);

            if (headLookActivated && Mathf.Abs(cameraOffset) > 10f)
            {
                _initialTurnValue = cameraOffset;
                _headLookX = Mathf.Lerp(_headLookX, _initialTurnValue / 200f, headTurnSmoothness * Time.deltaTime);
            }
            else
            {
                _initialTurnValue = headLookActivated ? _rotationRate : 0f;
                _headLookX = CalculateSmoothedValue(
                    _headLookX,
                    _initialTurnValue,
                    maxLeanRotationRate,
                    headTurnSmoothness,
                    _headLookXCurve,
                    _headLookX,
                    false
                );
            }

            float bodyTurnSmoothness = 5f;

            _initialTurnValue = bodyLookActivated ? _rotationRate : 0f;

            _bodyLookX = CalculateSmoothedValue(
                _bodyLookX,
                _initialTurnValue,
                maxLeanRotationRate,
                bodyTurnSmoothness,
                _bodyLookXCurve,
                _bodyLookX,
                false
            );

            // Vertical tilt from camera pitch
            float cameraTilt = _context.CameraController.GetCameraTiltX();
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180f;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            _headLookY = cameraTilt;
            _bodyLookY = cameraTilt;

            _previousRotation = _currentRotation;
        }

        private float CalculateSmoothedValue(
            float mainVariable,
            float newValue,
            float maxRateChange,
            float smoothness,
            AnimationCurve referenceCurve,
            float referenceValue,
            bool isMultiplier)
        {
            float changeVariable = newValue / maxRateChange;
            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

            if (isMultiplier)
            {
                float multiplier = referenceCurve != null ? referenceCurve.Evaluate(referenceValue) : 1f;
                changeVariable *= multiplier;
            }
            else
            {
                changeVariable = referenceCurve != null ? referenceCurve.Evaluate(changeVariable) : changeVariable;
            }

            if (!Mathf.Approximately(changeVariable, mainVariable))
            {
                changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);
            }

            return changeVariable;
        }

        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }

            return timeVariable;
        }
    }
}
