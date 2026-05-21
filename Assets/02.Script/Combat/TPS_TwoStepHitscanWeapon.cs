using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class TPS_TwoStepHitscanWeapon : MonoBehaviour
    {
        [Header("RayCast를 적용할 객체")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        
        // [S.O로 총기 정보 받아오기 전에 구현 먼저 해보기 위한 변수들 ] 
        [SerializeField] private float aimRange = 100f;
        [SerializeField] private float shotRange = 100f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float muzzleBlockRadius = 0.5f;
        [SerializeField] private float RPM = 800.0f;
        //무기 변경시 RPM을 받아와서 해당 딜레이 타임 1번 지정
        [SerializeField] private float shotDelayTime;
        //격발 시 딜레이 시간 저장
        [SerializeField] private float saveTime = -999f;
        //타겟 방향 direction 저장용 변수
        public Vector3 AimDirection { get; private set; }
        //탄창 구현
        

        [Header("각 탐지 Ray에 입력가능한 LayerMask 지정 ")]
        [SerializeField] private LayerMask aimMask;
        [SerializeField] private LayerMask shotMask;
        [SerializeField] private LayerMask muzzleBlockMask;
        
        [Header("피격 되면 피격 위치에 나오는 이펙트.")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float shotRadius;

        #region Unity Functions

        private void Awake()
        {
            if(aimCamera == null) aimCamera = Camera.main;
            if(muzzle == null)Debug.LogError("Aim camera not set", this);
            
            //임시
            shotDelayTime = RPMCalculate(RPM);
        }

        #endregion
      
        private AimResult ResolveAimPoint()
        {
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            //카메라 중앙점에 적중한 물체의 정보를 담는 그릇.
            AimResult result = new AimResult
            {
                ray = aimRay,
                didHit = false,
                point = aimRay.GetPoint(aimRange)
            };

            if (Physics.Raycast(aimRay, out RaycastHit hit,
                    aimRange, aimMask, QueryTriggerInteraction.Ignore))
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

            //방향만 얻음 ( normalized ) 
            //속력 -> 길이 magnitude
            Vector3 shotDirection = toAimPoint.normalized;
            float distanceToAimPoint = toAimPoint.magnitude;

            float castDistance = aimResult.didHit
                ? Mathf.Min(shotRange, distanceToAimPoint + 0.05f)
                : shotRange;
            
            //초기화.
            ShotResult result = new ShotResult
            {
                origin = muzzle.position,
                direction = shotDirection,
                distance = castDistance,
                didHit = false
            };

            if (Physics.Raycast(muzzle.position, shotDirection , out RaycastHit shotHit,shotRange,shotMask, QueryTriggerInteraction.Ignore))
            {
                result.didHit = true;
                result.hit = shotHit;
            }

            return result;
        }
        private void HandleHit(RaycastHit hit, AimResult aimResult)
        {
            string aimName = aimResult.didHit
                ? aimResult.hit.collider.name
                : "없음";

            string shotName = hit.collider.name;
            Debug.Log($"카메라 조준: {aimName} / 실제 피격: {shotName}");

            IDamageable damageable =
                hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect , 0.2f);
                
            }
        }
        private void DrawDebugRays(AimResult aim, ShotResult shot)
        {
            float aimDistance = aim.didHit ? aim.hit.distance : aimRange;
            Debug.DrawRay(
                aim.ray.origin,
                aim.ray.direction * aimDistance,
                Color.cyan,
                0.5f);

            float shotDistance = shot.didHit ? shot.hit.distance : shot.distance;
            Debug.DrawRay(
                shot.origin,
                shot.direction * shotDistance,
                shot.didHit ? Color.red : Color.yellow,
                0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (muzzle != null)
                Gizmos.DrawWireSphere(muzzle.position, muzzleBlockRadius);
        }

        private bool IsMuzzleBlocked()
        {
            /*if (checkMuzzleBlocked == false)
            {
                return false;
            }*/

            return Physics.CheckSphere(
                muzzle.position,
                muzzleBlockRadius,
                muzzleBlockMask,
                QueryTriggerInteraction.Ignore);
        }
        public void Fire()
        {
            if (aimCamera == null || muzzle == null)
            {
                Debug.LogWarning("Aim Camera 또는 Muzzle이 없습니다.");
                return;
            }

            if (IsMuzzleBlocked())
            {
                Debug.Log("발사 불가: 총구가 장애물에 너무 가깝습니다.");
                return;
            }

            if ( Time.time <= saveTime + shotDelayTime)
            {
                Debug.Log("재장전중..");
                return;
            }
            //격발 된 시간 저장.
            saveTime = Time.time;
            
            AimResult aimResult = ResolveAimPoint();
            ShotResult shotResult = FireFromMuzzle(aimResult);
            
            //타겟방향 direction 저장
            AimDirection =  shotResult.direction;
            DrawDebugRays(aimResult, shotResult);

            if (shotResult.didHit)
            {
                HandleHit(shotResult.hit, aimResult);
            }
        }
        
        private float RPMCalculate(float rpm) //RPM을 받아서 초당 딜레이 타임으로 변환 계산합니다 ( 60sec / RPM ) 
        {
            if(rpm <= 0) return 0;
            return 60.0f / rpm;
        }
        //총으로 맞은 목표를 향한 벡터3를 반환
    }

}