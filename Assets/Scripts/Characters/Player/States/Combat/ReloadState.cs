using TeamZ.Characters.Core;
using TeamZ.Characters.Player.Components;
using UnityEngine;

namespace TeamZ.Characters.Player.States.Combat
{
    public class ReloadState : ICharacterState
    {
        private readonly PlayerContext _context;
        private readonly PlayerController _owner;
        private readonly WeaponHandlerComponent _weaponHandler;
        private readonly float _duration;
        private float _timer;
        private const string LockSource = "Reload";

        public ReloadState(PlayerContext context, PlayerController owner, float duration)
        {
            _context = context;
            _owner = owner;
            
            _weaponHandler = _context.weaponHandler;
            
             _duration = duration;
            _timer = duration;
        }

        public void Enter()
        {
            // Request movement lock for the duration of the reload.
            _context?.RequestMovementLock(LockSource);

            // Trigger animator reload if present.
            if (_context != null && _context.animator != null)
            {
                _context.animator.SetTrigger("Reload");
            }

            // Optional: could play reload SFX via weapon handler if available.
        }

        public void Tick()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                _weaponHandler.ReloadCurrentWeapon();
                
                // End reload and return to idle combat state.
                _context?.ReleaseMovementLock(LockSource);
                
                _context.CombatStateMachine.SetState(new CombatIdleState(_context, _owner));
            }
        }

        public void FixedTick()
        {
            // No physics logic needed during reload.
        }

        public void Exit()
        {
            // Ensure lock is released.
            _context?.ReleaseMovementLock(LockSource);
        }
    }
}

