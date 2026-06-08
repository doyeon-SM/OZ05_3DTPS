using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Weapon_", menuName = "WeaponData_S.O")]
public class WeaponData : ScriptableObject
{
    [Header("Default Info")]
    public string WeaponId;
    public string WeaponName;
    [TextArea] public string WeaponDescription;
    public WeaponClass WeaponType;
    
    [Header("Instance Holder Transform")]
    public GameObject WeaponPrefab;
    public Vector3 HolderPosition;
    public Vector3 HolderRotation;
    
    [Header("Fire Mode")]
    [FormerlySerializedAs("FireMode")]
    public FireMode fireMode;

    [Header("Combat Value")]
    public int Damage;
    public float CriticalMultiplier;
    public float RPM;
    public float ReloadTime;
    public int MagazineSize;
    public int BulletCost = 1;
    public bool UseAmmo;

    [Header("Attack Pattern")]
    public float basicSpreadAngle = 0f;

    [Header("ShotGun")]
    public int pelletCount = 1;

    [Header("Burst Shot")]
    public int burstCount = 1;

    [Header("Prefab And Effects")]
    public ParticleSystem FireEffect;
    public AudioClip FireSound;
    public Sprite UnLockIcon;
    public Sprite LockIcon;
}
