using _01.Scenes.PhaseValidation;
using UnityEngine;

namespace _02.Script.Combat
{
    public struct HitscanFireRequest
    {
        public Camera AimCamera;
        public Transform Muzzle;
        public float AimRange;
        public float ShotRange;
        public float MuzzleBlockRadius;
        public LayerMask AimMask;
        public LayerMask ShotMask;
        public LayerMask MuzzleBlockMask;
        public GameObject HitEffectPrefab;
        public float HitEffectLifeTime;
        public int Damage;
    }

    public class TPS_TwoStepHitscanSystem : MonoBehaviour
    {
        public Vector3 AimDirection { get; private set; } // 타겟 방향 저장용 변수

        private AimResult ResolveAimPoint(HitscanFireRequest request)
        {
            Ray aimRay = request.AimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            AimResult result = new AimResult
            {
                ray = aimRay,
                didHit = false,
                point = aimRay.GetPoint(request.AimRange)
            };

            if (Physics.Raycast(aimRay, out RaycastHit hit, request.AimRange, request.AimMask, QueryTriggerInteraction.Ignore))
            {
                result.didHit = true;
                result.hit = hit;
                result.point = hit.point;
            }

            return result;
        }

        private ShotResult FireFromMuzzle(AimResult aimResult, HitscanFireRequest request)
        {
            Vector3 toAimPoint = aimResult.point - request.Muzzle.position;

            if (toAimPoint.sqrMagnitude < 0.0001f)
            {
                toAimPoint = request.AimCamera.transform.forward;
            }

            Vector3 shotDirection = toAimPoint.normalized;
            float distanceToAimPoint = toAimPoint.magnitude;

            float castDistance = aimResult.didHit
                ? Mathf.Min(request.ShotRange, distanceToAimPoint + 0.05f)
                : request.ShotRange;

            ShotResult result = new ShotResult
            {
                origin = request.Muzzle.position,
                direction = shotDirection,
                distance = castDistance,
                didHit = false
            };

            if (Physics.Raycast(request.Muzzle.position, shotDirection, out RaycastHit shotHit, castDistance, request.ShotMask,
                    QueryTriggerInteraction.Ignore))
            {
                result.didHit = true;
                result.hit = shotHit;
            }

            return result;
        }

        private void HandleHit(RaycastHit hit, AimResult aimResult, HitscanFireRequest request)
        {
            string aimName = aimResult.didHit ? aimResult.hit.collider.name : "없음";
            string shotName = hit.collider.name;
            Debug.Log($"카메라 조준: {aimName} / 실제 사격: {shotName}");

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(request.Damage);
            }

            if (request.HitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(request.HitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, request.HitEffectLifeTime);
            }
        }

        private void DrawDebugRays(AimResult aim, ShotResult shot, HitscanFireRequest request)
        {
            float aimDistance = aim.didHit ? aim.hit.distance : request.AimRange;
            Debug.DrawRay(aim.ray.origin, aim.ray.direction * aimDistance, Color.cyan, 0.5f);

            float shotDistance = shot.didHit ? shot.hit.distance : shot.distance;
            Debug.DrawRay(shot.origin, shot.direction * shotDistance, shot.didHit ? Color.red : Color.yellow, 0.5f);
        }

        private bool IsMuzzleBlocked(HitscanFireRequest request)
        {
            return Physics.CheckSphere(
                request.Muzzle.position,
                request.MuzzleBlockRadius,
                request.MuzzleBlockMask,
                QueryTriggerInteraction.Ignore);
        }

        public bool CanFire(HitscanFireRequest request)
        {
            if (request.AimCamera == null)
            {
                Debug.LogWarning("[TPS_TwoStepHitscanSystem] AimCamera가 null입니다.", this);
                return false;
            }

            if (request.Muzzle == null)
            {
                Debug.LogWarning("[TPS_TwoStepHitscanSystem] Muzzle이 null입니다.", this);
                return false;
            }

            if (IsMuzzleBlocked(request))
            {
                Debug.Log("발사 불가: 총구가 장애물에 너무 가깝습니다.");
                return false;
            }

            return true;
        }

        public bool Fire(HitscanFireRequest request)
        {
            if (!CanFire(request))
                return false;

            AimResult aimResult = ResolveAimPoint(request);
            ShotResult shotResult = FireFromMuzzle(aimResult, request);

            AimDirection = shotResult.direction;
            DrawDebugRays(aimResult, shotResult, request);

            if (shotResult.didHit)
            {
                HandleHit(shotResult.hit, aimResult, request);
            }

            return true;
        }

        public bool Fire()
        {
            Debug.LogWarning("[TPS_TwoStepHitscanSystem] Fire() 직접 호출은 더 이상 권장하지 않습니다. WeaponController를 통해 HitscanFireRequest를 전달해주세요.", this);
            return false;
        }
    }
}
