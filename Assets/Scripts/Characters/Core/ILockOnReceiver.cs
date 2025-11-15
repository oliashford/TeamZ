using UnityEngine;

namespace TeamZ.Characters.Core
{
    public interface ILockOnReceiver
    {
        void AddTargetCandidate(GameObject target);
        void RemoveTarget(GameObject target);
    }
}

