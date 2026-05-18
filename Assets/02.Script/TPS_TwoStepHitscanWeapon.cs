using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class TPS_TwoStepHitscanWeapon : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float aimRange = 100f;
        [SerializeField] private float shotRange = 100f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float muzzleBlockRadius = 0.5f;
        
        [SerializeField] private LayerMask aimMask;
        [SerializeField] private LayerMask shotMask;
        [SerializeField] private LayerMask muzzleBlockMask;

        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float shotRadius = 0f;

        #region Unity Functions

        private void Awake()
        {
            if(aimCamera == null) aimCamera = Camera.main;
            if(muzzle == null)Debug.LogError("Aim camera not set", this);
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

            AimResult aimResult = ResolveAimPoint();
            ShotResult shotResult = FireFromMuzzle(aimResult);

            DrawDebugRays(aimResult, shotResult);

            if (shotResult.didHit)
            {
                HandleHit(shotResult.hit, aimResult);
            }
        }


    }

}