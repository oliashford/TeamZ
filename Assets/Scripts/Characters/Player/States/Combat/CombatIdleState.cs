using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using TeamZ.InputSystem;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Combat
{
    public class CombatIdleState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;
        
        private readonly InputReader _input;
        private readonly WeaponHandlerComponent _weaponHandler;
        private readonly AimComponent _aimComponent;

        public CombatIdleState(PlayerContext context, PlayerController owner)
        {
            _context = context;
            _owner = owner;

            _input = _context.inputReader;
            _weaponHandler = _context.weaponHandler;
            _aimComponent = _context.aim;
        }

        public void Enter()
        {
            // Subscribe to fire input.
            _input.onFirePerformed += OnFirePerformed;
            _input.onAimActivated += OnAimActivated;
            _input.onAimDeactivated += OnAimDeactivated;
            _input.onReload += OnReload;
        }

        public void Tick()
        {
            // No logic here; input callbacks drive transitions.
        }

        public void FixedTick()
        {
            // No fixed update logic needed for idle state.
        }

        public void Exit()
        {
            _input.onFirePerformed -= OnFirePerformed;
            _input.onAimActivated -= OnAimActivated;
            _input.onAimDeactivated -= OnAimDeactivated;
            _input.onReload -= OnReload;
        }

        private void OnAimActivated()
        {
            _context.CombatStateMachine.SetState(new AimState(_context, _owner));
        }

        private void OnAimDeactivated()
        {
            // No-op; remain in idle
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
            
            //animator.SetTrigger("Fire");
        }
    }
}
