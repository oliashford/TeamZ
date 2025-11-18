using UnityEngine;

namespace TeamZ.Characters.Core
{
    [RequireComponent(typeof(Animator))]
    public class IKComponent : MonoBehaviour
    {
        [SerializeField] private WeaponHandlerComponent _weaponHandler;

        [Tooltip("Overall weight for left-hand IK when weapon is equipped and unholstered")]
        [Range(0f, 1f)]
        [SerializeField] private float _leftHandIkWeight = 1f;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_weaponHandler == null)
            {
                _weaponHandler = GetComponent<WeaponHandlerComponent>();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _weaponHandler == null)
            {
                return;
            }

            Weapons.Weapon currentWeapon = _weaponHandler.CurrentWeapon;
            bool canUseIk = currentWeapon != null && _weaponHandler.IsUnholstered;

            if (!canUseIk)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                return;
            }

            Transform leftTarget = currentWeapon.LeftHandIkTarget;
            if (leftTarget == null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                return;
            }

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _leftHandIkWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _leftHandIkWeight);

            _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget.position);
            _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftTarget.rotation);
        }
    }
}