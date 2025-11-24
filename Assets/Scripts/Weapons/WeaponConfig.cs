using UnityEngine;

namespace TeamZ.Weapons
{
    [CreateAssetMenu(menuName = "TeamZ/Weapons/WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Basics")]
        public string weaponName;
        public GameObject prefab;

        [Header("Spread (degrees)")]
        [Tooltip("Bullet spread when hip firing")]
        public float hipSpread = 5f;

        [Tooltip("Bullet spread when aiming down sights")]
        public float adsSpread = 1f;

        [Header("Range / Mask")]
        public float maxDistance = 100f;

        public LayerMask hitMask = Physics.DefaultRaycastLayers;

        [Header("Impact")]
        [Tooltip("VFX prefab spawned at hit point")]
        public GameObject impactEffectPrefab;

        [Header("IK Offsets (optional, for tooling)")]
        public Vector3 rightHandPosition;
        public Vector3 rightHandEuler;
        public Vector3 leftHandPosition;
        public Vector3 leftHandEuler;

        [Header("ADS Camera Tweak (optional)")]
        [Tooltip("FOV offset applied when aiming with this weapon")]
        public float adsFovModifier = -10f;

        [Header("Ammo")]
        [Tooltip("Number of rounds in a full magazine")]
        public int magazineSize = 30;
    }
}