using UnityEngine;

    [CreateAssetMenu(
        fileName = "Weapon_",menuName = "WeaponData_S.O")]
    public class WeaponData :  ScriptableObject
    {
        [Header("기본 정보")]
        public string weaponId;             // 총기 ID
        public string weaponName;           // 총기 Display Name
        public WeaponClass WeaponType;      // 총기 타입
        public Sprite icon;                 // 출력용 icon
        
        [Header("전투 수치")]
        public int Damage;                  // 총기의 기본 데미지
        public float CriticalMultiplier;    // 치명타 적용 데미지 배율
        public float RPM;                   // 분당 발사 수 - (Rounds Per Minute)
        public float ReloadTime;            // 재장전 시간
        public int MagazineSize;            // 탄창당 최대 총알 수 
        public bool useAmmo;                // 총알 사용 여부
        
        /* - 치명타 확률과 거리판정 한계 판정은 논의 후 다시 추가.
        public float criticalChance;
        public float criticalMultiplier = 1.5f;
         public float attackRange;*/
       
        
        [Header("프리팹 및 이펙트")]
        public GameObject waeponPrefab;     // 무기 프리팹
        public Transform MuzzleTransform;   // 무기 사출구 오브젝트(위치)
        public ParticleSystem fireEffect;   // 사격 이펙트
        public AudioClip fireSound;         // 사격 사운드
        
        /* - 현재 총기는 RayCast 형식으로 구현 되었으니 projectile은 뺐습니다.
         * public GameObject projectilePrefab;
         */
    }
