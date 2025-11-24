using UnityEngine;

namespace TeamZ.Characters.Core
{
    /// <summary>
    /// Small Animator helper extensions used to safely query animator parameters.
    /// </summary>
    public static class AnimatorExtensions
    {
        /// <summary>
        /// Returns true if the animator contains a parameter with the given name.
        /// Safe to call with a null animator (returns false).
        /// </summary>
        public static bool HasParameter(this Animator animator, string paramName)
        {
            if (animator == null || string.IsNullOrEmpty(paramName))
                return false;

            var parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == paramName)
                    return true;
            }

            return false;
        }
    }
}
