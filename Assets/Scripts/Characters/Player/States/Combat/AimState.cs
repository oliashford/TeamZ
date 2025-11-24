using TeamZ.Characters.Core;
using TeamZ.InputSystem;
using UnityEngine;
using TeamZ.Characters.Player.Components;

namespace TeamZ.Characters.Player.States.Combat
{
    public class AimState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;
        private readonly InputReader _input;
        private readonly WeaponHandlerComponent _weaponHandler;
        private readonly AimComponent _aimComponent;

        public AimState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;
            
            _input = _context.inputReader;
            _weaponHandler = _context.weaponHandler;
            _aimComponent = _context.aim;
        }

        public void Enter()
        {
            _input.onFirePerformed += OnFirePerformed;
            _input.onReload += OnReload;
        }

        public void Tick()
        {
            if (!_aimComponent.IsAiming)
            {
                _context.CombatStateMachine.SetState(new CombatIdleState(_context, _owner));
            }
        }

        public void FixedTick()
        {
            // No physics logic needed in AimState.
        }

        public void Exit()
        {
            _input.onFirePerformed -= OnFirePerformed;
            _input.onReload -= OnReload;
        }

        private void OnReload()
        {
            _context.CombatStateMachine.SetState(new ReloadState(_context, _owner, 1.2f));
        }

        private void OnFirePerformed()
        {
            Ray aimRay = _aimComponent.GetAimRay();
            
            bool isAiming = _aimComponent.IsAiming;

            bool fired = _weaponHandler.TryFire(aimRay, isAiming);
            
            if (!fired)
            {
                if (_context != null && _owner != null)
                {
                    _context.CombatStateMachine.SetState(new ReloadState(_context, _owner, 1.2f));
                }
                return;
            }

            if (_context != null && _context.animator != null)
            {
                //_context.Animator.SetTrigger("Fire");
            }
        }
    }
}
