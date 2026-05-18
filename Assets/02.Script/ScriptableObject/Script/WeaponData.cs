using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [CreateAssetMenu(
        fileName = "Weapon_",menuName = "WeaponData")]
    public class WeaponData :  ScriptableObject
    {
        [Header("기본 정보")]
        public string weaponId;
        public string weaponName;
        public WeaponType WeaponType;
        public Sprite icon;
        
        [Header("전투 수치")]
        public int damage;
        public float attackRange;
        public float attackRate;
        public float criticalChance;
        public float criticalMultiplier = 1.5f;
        
        [Header("원거리 무기")]
        public int magazineSize;
        public float reloadTime;
        public bool useAmmo;
        
        [Header("프리팹 및 이펙트")]
        public GameObject waeponPrefab;
        public GameObject projectilePrefab;
        public ParticleSystem fireEffect;
        public AudioClip fireSound;
    }
}