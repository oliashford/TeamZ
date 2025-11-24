using UnityEngine;

namespace TeamZ.Weapons
{
    // Simple hitscan weapon used by TeamZ.Characters.Core.WeaponHandlerComponent.
    // This focuses on runtime behaviour and leaves visuals / VFX to be added later.
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponConfig _config;

        [Header("Optional IK Targets")]
        [Tooltip("Transform target for character's left hand IK")]
        [SerializeField] private Transform _leftHandIkTarget;

        [Header("Muzzle")]
        [Tooltip("Barrel tip position for bullet origin and visual effects")]
        [SerializeField] private Transform _muzzle;

        [Header("Visual Effects")]
        [Tooltip("Prefab containing a LineRenderer component for bullet tracers")]
        [SerializeField] private GameObject _tracerPrefab;
        
        [Tooltip("Duration before destroying tracer objects")]
        [SerializeField] private float _tracerDuration = 0.15f;

        [Tooltip("Muzzle flash effect spawned when firing")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _fireClip;

        private int _currentAmmo;

        public WeaponConfig Config => _config;
        public Transform LeftHandIkTarget => _leftHandIkTarget;
        public Transform Muzzle => _muzzle;


        public void Initialize(WeaponConfig config)
        {
            _config = config;

            // Initialize ammo from config magazine size
            _currentAmmo = _config != null ? Mathf.Max(1, _config.magazineSize) : 0;
        }

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Try to fire the weapon. Returns true if a shot was fired, false if out of ammo.
        /// </summary>
        public bool TryFire(Ray aimRay, bool isAiming)
        {
            if (_config == null)
            {
                Debug.LogWarning("Weapon fired without a WeaponConfig.", this);
            }

            if (_currentAmmo <= 0)
            {
                // No ammo to fire.
                return false;
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

            // Consume ammo
            _currentAmmo = Mathf.Max(0, _currentAmmo - 1);

            // Game-view tracer: from muzzle (if available) to the resolved hit point.
            Vector3 tracerStart = _muzzle != null ? _muzzle.position : rayOrigin;
            DrawTracer(tracerStart, hitPoint);
            PlayMuzzleFlash();
            PlayFireSound();

            return true;
        }

        private void DrawTracer(Vector3 start, Vector3 end)
        {
            if (_tracerPrefab == null)
            {
                // Fallback debug-only line in Scene view
                Debug.DrawLine(start, end, Color.cyan, 0.2f);
                return;
            }

            // Instantiate the tracer prefab and configure its LineRenderer
            GameObject tracerObj = Object.Instantiate(_tracerPrefab);
            LineRenderer lineRenderer = tracerObj.GetComponent<LineRenderer>();
            
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }
            else
            {
                Debug.LogWarning("Tracer prefab does not have a LineRenderer component.", this);
            }

            // Destroy the tracer after the specified duration
            Object.Destroy(tracerObj, _tracerDuration);
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

        public void Reload()
        {
            if (_config == null)
            {
                return;
            }

            _currentAmmo = Mathf.Max(1, _config.magazineSize);
        }

        public int CurrentAmmo => _currentAmmo;
    }
}