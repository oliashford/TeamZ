using UnityEngine;
using TeamZ.Weapons;

namespace TeamZ.Characters.Core
{
    [RequireComponent(typeof(Animator))]
    public class WeaponHandlerComponent : MonoBehaviour
    {
        [Header("Sockets")]
        [SerializeField] private Transform _rightHandSocket;
        [SerializeField] private Transform _holsterSocket;

        [Header("Initial Weapon (optional)")]
        [SerializeField] private WeaponConfig _startingWeaponConfig;

        [Header("Runtime")]
        [SerializeField] private Weapon _currentWeapon;
        [SerializeField] private bool _isHolstered = true;

        [Header("Aiming")]
        [SerializeField] private AimComponent _aimComponent;
        [SerializeField] private float _adsAimRotateSpeed = 20f;

        private Animator _animator;

        private static readonly int _drawHash = Animator.StringToHash("DrawWeapon");
        private static readonly int _holsterHash = Animator.StringToHash("HolsterWeapon");

        public Weapon CurrentWeapon => _currentWeapon;
        public bool IsUnholstered => _currentWeapon != null && !_isHolstered;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_aimComponent == null)
            {
                _aimComponent = GetComponent<AimComponent>();
            }
        }

        private void Start()
        {
            if (_startingWeaponConfig != null)
            {
                EquipWeapon(_startingWeaponConfig);
            }
        }

        private void LateUpdate()
        {
            if (_currentWeapon == null || _aimComponent == null)
            {
                return;
            }

            if (!_aimComponent.isAiming)
            {
                return;
            }

            Vector3 aimPoint = _aimComponent.GetAimPoint(_currentWeapon.Config != null ? _currentWeapon.Config.maxDistance : 100f);
            _currentWeapon.AimMuzzleAt(aimPoint, _adsAimRotateSpeed);
        }

        public void EquipWeapon(WeaponConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
            }

            if (config.prefab == null)
            {
                Debug.LogWarning($"WeaponConfig '{config.name}' has no prefab assigned.", this);
                return;
            }

            // For the current prototype, spawn the weapon directly in the right hand
            // so it is always drawn. We'll reintroduce holster parenting when we
            // hook up draw/put-back animations.
            Transform parentSocket = _rightHandSocket != null ? _rightHandSocket : _holsterSocket;
            GameObject weaponGo = Instantiate(config.prefab, parentSocket);

            _currentWeapon = weaponGo.GetComponent<Weapon>();
            if (_currentWeapon == null)
            {
                _currentWeapon = weaponGo.AddComponent<Weapon>();
            }

            _currentWeapon.Initialize(config);
            _isHolstered = false;
        }

        public void ToggleHolster()
        {
            if (_currentWeapon == null)
            {
                return;
            }

            if (_isHolstered)
            {
                _animator.SetTrigger(_drawHash);
            }
            else
            {
                _animator.SetTrigger(_holsterHash);
            }
        }

        // Animation Event hook – called from Draw animation when weapon is in the hand.
        public void OnWeaponDrawn()
        {
            if (_currentWeapon == null || _rightHandSocket == null)
            {
                return;
            }

            _currentWeapon.transform.SetParent(_rightHandSocket, false);
            _isHolstered = false;
        }

        // Animation Event hook – called from Holster animation when weapon is back on the belt.
        public void OnWeaponHolstered()
        {
            if (_currentWeapon == null || _holsterSocket == null)
            {
                return;
            }

            _currentWeapon.transform.SetParent(_holsterSocket, false);
            _isHolstered = true;
        }

        public void Fire(Ray aimRay, bool isAiming)
        {
            if (_currentWeapon == null)
            {
                return;
            }

            _currentWeapon.Fire(aimRay, isAiming);
        }
    }
}