using _02.Script.ScriptableObject;
using UnityEngine;

    [CreateAssetMenu(
        fileName = "Weapon_",menuName = "WeaponData_S.O")]
    public class WeaponData :  ScriptableObject
    {
        [Header("기본 정보")]
        public string WeaponId;             // 총기 ID
        public string WeaponName;           // 총기 Display Name
        [TextArea] public string WeaponDescription;    // UI용 string Text
        public WeaponClass WeaponType;      // 총기 타입
        public Tier  tier;                  // 총기 등급
        
        [Header("전투 수치")]
        public int Damage;                  // 총기의 기본 데미지
        public float CriticalMultiplier;    // 치명타 적용 데미지 배율
        public float RPM;                   // 분당 발사 수 - (Rounds Per Minute)
        public float ReloadTime;            // 재장전 시간
        public int MagazineSize;            // 탄창당 최대 총알 수
        public int BulletCost;              // 1회 사격당 소모되는 Ammo Cost
        public bool UseAmmo;                // 총알 사용 여부
        public bool AutoFire;               // 자동 사격 여부 ( true 라면 꾹 누르면 계속 나가는 )
        
        
        [Header("프리팹 및 이펙트")]
        public GameObject WeaponPrefab;     // 무기 프리팹
        public ParticleSystem FireEffect;   // 사격 이펙트
        public AudioClip FireSound;         // 사격 사운드
        public Sprite UnLockIcon;           // 출력용 icon
        
    }
