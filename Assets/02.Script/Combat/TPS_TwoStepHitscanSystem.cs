using _01.Scenes.PhaseValidation;
using UnityEngine;

namespace _02.Script.Combat
{
    public class TPS_TwoStepHitscanSystem : MonoBehaviour
    {
        [Header("RayCast를 적용할 객체")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;

        [Header("Ray 관련 변수")]
        [SerializeField] private float aimRange = 100f;
        [SerializeField] private float shotRange = 100f;
        [SerializeField] private float muzzleBlockRadius = 0.5f;

        [Header("무기 관리")]
        [SerializeField] private WeaponController weaponController;

        public Vector3 AimDirection { get; private set; } // 타겟 방향 저장용 변수

        [Header("Ray 입력 가능한 LayerMask 지정")]
        [SerializeField] private LayerMask aimMask;
        [SerializeField] private LayerMask shotMask;
        [SerializeField] private LayerMask muzzleBlockMask;

        [Header("피격 이펙트")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float shotRadius;

        #region Unity Functions

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (muzzle == null)
            {
                Debug.LogError("Muzzle이 설정되어 있지 않습니다.", this);
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }
        }

        #endregion

        private AimResult ResolveAimPoint()
        {
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            AimResult result = new AimResult
            {
                ray = aimRay,
                didHit = false,
                point = aimRay.GetPoint(aimRange)
            };

            if (Physics.Raycast(aimRay, out RaycastHit hit, aimRange, aimMask, QueryTriggerInteraction.Ignore))
            {
                result.didHit = true;
                result.hit = hit;
                result.point = hit.point;
            }

            return result;
        }

        private ShotResult FireFromMuzzle(AimResult aimResult)
        {
            Vector3 toAimPoint = aimResult.point - muzzle.position;

            if (toAimPoint.sqrMagnitude < 0.0001f)
            {
                toAimPoint = aimCamera.transform.forward;
            }

            Vector3 shotDirection = toAimPoint.normalized;
            float distanceToAimPoint = toAimPoint.magnitude;

            float castDistance = aimResult.didHit
                ? Mathf.Min(shotRange, distanceToAimPoint + 0.05f)
                : shotRange;

            ShotResult result = new ShotResult
            {
                origin = muzzle.position,
                direction = shotDirection,
                distance = castDistance,
                didHit = false
            };

            if (Physics.Raycast(muzzle.position, shotDirection, out RaycastHit shotHit, castDistance, shotMask,
                    QueryTriggerInteraction.Ignore))
            {
                result.didHit = true;
                result.hit = shotHit;
            }

            return result;
        }

        private void HandleHit(RaycastHit hit, AimResult aimResult)
        {
            string aimName = aimResult.didHit ? aimResult.hit.collider.name : "없음";
            string shotName = hit.collider.name;
            Debug.Log($"카메라 조준: {aimName} / 실제 피격: {shotName}");

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(weaponController.Damage);
            }

            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, 0.2f);
            }
        }

        private void DrawDebugRays(AimResult aim, ShotResult shot)
        {
            float aimDistance = aim.didHit ? aim.hit.distance : aimRange;
            Debug.DrawRay(aim.ray.origin, aim.ray.direction * aimDistance, Color.cyan, 0.5f);

            float shotDistance = shot.didHit ? shot.hit.distance : shot.distance;
            Debug.DrawRay(shot.origin, shot.direction * shotDistance, shot.didHit ? Color.red : Color.yellow, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (muzzle == null) return;

            Gizmos.color = Color.chartreuse;
            Gizmos.DrawWireSphere(muzzle.position, muzzleBlockRadius);
        }

        private bool IsMuzzleBlocked()
        {
            return Physics.CheckSphere(
                muzzle.position,
                muzzleBlockRadius,
                muzzleBlockMask,
                QueryTriggerInteraction.Ignore);
        }

        public bool Fire()
        {
            if (aimCamera == null || muzzle == null)
            {
                Debug.LogWarning("Aim Camera 또는 Muzzle이 없습니다.");
                return false;
            }

            if (IsMuzzleBlocked())
            {
                Debug.Log("발사 불가: 총구가 장애물에 너무 가깝습니다.");
                return false;
            }

            if (weaponController == null)
            {
                Debug.LogWarning("WeaponController가 없습니다.");
                return false;
            }

            if (!weaponController.TryFire())
            {
                return false;
            }

            AimResult aimResult = ResolveAimPoint();
            ShotResult shotResult = FireFromMuzzle(aimResult);

            AimDirection = shotResult.direction;
            DrawDebugRays(aimResult, shotResult);

            if (shotResult.didHit)
            {
                HandleHit(shotResult.hit, aimResult);
            }

            return true;
        }
    }
}
