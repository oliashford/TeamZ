using UnityEngine;

namespace TeamZ.Weapons
{
    /// <summary>
    /// Simple hitscan weapon used by <see cref="TeamZ.Characters.Core.WeaponHandlerComponent"/>.
    /// This focuses on runtime behaviour and leaves visuals / VFX to be added later.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponConfig _config;

        [Header("Optional IK Targets")]
        [Tooltip("Target used by IK for the character's left hand.")]
        [SerializeField] private Transform _leftHandIkTarget;

        [Header("Muzzle")]
        [Tooltip("Barrel tip used for visual alignment and tracers.")]
        [SerializeField] private Transform _muzzle;

        [Header("Visual Effects")]
        [Tooltip("Optional line renderer used to display a brief bullet tracer.")]
        [SerializeField] private LineRenderer _tracerLine;
        [Tooltip("Duration, in seconds, to show the tracer after firing.")]
        [SerializeField] private float _tracerDuration = 0.15f; // increased for visibility while tuning

        [Tooltip("Optional muzzle flash prefab instantiated at the muzzle when firing.")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        [Header("Audio")]
        [Tooltip("AudioSource used to play firing sounds.")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Sound played when this weapon fires.")]
        [SerializeField] private AudioClip _fireClip;

        public WeaponConfig Config => _config;
        public Transform LeftHandIkTarget => _leftHandIkTarget;
        public Transform Muzzle => _muzzle;

        private float _tracerTimer;
        private bool _tracerActive;

        public void Initialize(WeaponConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            // Do not force-disable the tracer here; let the prefab control its initial state.
        }

        private void Update()
        {
            // No automatic tracer disabling; the LineRenderer will simply be updated
            // on each fire call. If you want time-based hiding later, we can reintroduce
            // a timer, but for now we keep it simple and predictable.
        }

        public void Fire(Ray aimRay, bool isAiming)
        {
            if (_config == null)
            {
                Debug.LogWarning("Weapon fired without a WeaponConfig.", this);
            }

            Vector3 dir = aimRay.direction;

            float spread = isAiming ? _config.adsSpread : _config.hipSpread;
            if (spread > 0f)
            {
                dir = ApplySpread(dir, spread);
            }

            float maxDistance = _config != null ? _config.maxDistance : 100f;
            Vector3 rayOrigin = aimRay.origin;

            Vector3 hitPoint;
            bool hitSomething;
            if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, maxDistance, _config.hitMask))
            {
                hitSomething = true;
                hitPoint = hit.point;

                // Scene-view debug from camera origin
                Debug.DrawLine(rayOrigin, hit.point, Color.red, 0.2f);

                if (_config.impactEffectPrefab != null)
                {
                    Object.Instantiate(_config.impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }

                // TODO: damage handling via hit.collider.GetComponent<IDamageable>() etc.
            }
            else
            {
                hitSomething = false;
                hitPoint = rayOrigin + dir * maxDistance;

                // Scene-view debug from camera origin
                Debug.DrawLine(rayOrigin, hitPoint, Color.yellow, 0.2f);
            }

            // Game-view tracer: from muzzle (if available) to the resolved hit point.
            Vector3 tracerStart = _muzzle != null ? _muzzle.position : rayOrigin;
            DrawTracer(tracerStart, hitPoint);
            PlayMuzzleFlash();
            PlayFireSound();
        }

        private void DrawTracer(Vector3 start, Vector3 end)
        {
            if (_tracerLine == null)
            {
                // Fallback debug-only line in Scene view
                Debug.DrawLine(start, end, Color.cyan, 0.2f);
                return;
            }

            _tracerLine.positionCount = 2;
            _tracerLine.SetPosition(0, start);
            _tracerLine.SetPosition(1, end);
            _tracerLine.enabled = true;
        }

        private void PlayMuzzleFlash()
        {
            if (_muzzleFlashPrefab == null || _muzzle == null)
            {
                return;
            }

            GameObject flash = Object.Instantiate(_muzzleFlashPrefab, _muzzle.position, _muzzle.rotation);
            // Auto-destroy after a short time to avoid clutter; assumes a quick flash effect.
            Object.Destroy(flash, 2f);
        }

        private void PlayFireSound()
        {
            if (_audioSource == null || _fireClip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(_fireClip);
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadAngleDegrees)
        {
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(-spreadAngleDegrees, spreadAngleDegrees),
                Random.Range(-spreadAngleDegrees, spreadAngleDegrees),
                0f);

            return (randomRot * direction).normalized;
        }

        public void AimMuzzleAt(Vector3 worldPoint, float rotateSpeed)
        {
            if (_muzzle == null)
            {
                return;
            }

            Vector3 dir = (worldPoint - _muzzle.position).normalized;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // Assume weapon forward (+Z) points down the barrel.
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }
}