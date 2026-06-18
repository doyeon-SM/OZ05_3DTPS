using _00.ChoiHeesu._01.Script;
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
        public WeaponEffectPrinter EffectPrinter;
        public int Damage;
        public float SpreadAngle;
    }

    public class TPS_TwoStepHitscanSystem : MonoBehaviour
    {
        private const float ShotHitPadding = 0.25f;

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
            Vector3 shotDirection = GetBaseShotDirection(aimResult, request);
            shotDirection = ApplySpread(shotDirection, request.SpreadAngle);
            float castDistance = GetShotCastDistance(aimResult, request, shotDirection);
            return FireRayFromMuzzle(request, shotDirection, castDistance);
        }

        private Vector3 GetBaseShotDirection(AimResult aimResult, HitscanFireRequest request)
        {
            Vector3 toAimPoint = aimResult.point - request.Muzzle.position;

            if (toAimPoint.sqrMagnitude < 0.0001f)
            {
                toAimPoint = request.AimCamera.transform.forward;
            }

            Vector3 shotDirection = toAimPoint.normalized;

            return shotDirection;
        }

        private float GetShotCastDistance(AimResult aimResult, HitscanFireRequest request, Vector3 shotDirection)
        {
            if (!aimResult.didHit)
                return request.ShotRange;

            float distanceToAimPoint = Vector3.Distance(request.Muzzle.position, aimResult.point);
            float castDistance = distanceToAimPoint + ShotHitPadding;

            if (TryGetAimSurfaceDistance(aimResult, request, shotDirection, out float aimSurfaceDistance))
                castDistance = Mathf.Max(castDistance, aimSurfaceDistance + ShotHitPadding);

            return Mathf.Min(request.ShotRange, castDistance);
        }

        private bool TryGetAimSurfaceDistance(AimResult aimResult, HitscanFireRequest request, Vector3 shotDirection, out float distance)
        {
            distance = 0f;

            float denominator = Vector3.Dot(shotDirection, aimResult.hit.normal);
            if (Mathf.Abs(denominator) < 0.0001f)
                return false;

            float numerator = Vector3.Dot(aimResult.point - request.Muzzle.position, aimResult.hit.normal);
            distance = numerator / denominator;
            return distance > 0f;
        }

        private ShotResult FireRayFromMuzzle(HitscanFireRequest request, Vector3 shotDirection, float castDistance)
        {
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

        private Vector3 ApplySpread(Vector3 baseDirection, float spreadAngle)
        {
            float safeSpreadAngle = Mathf.Max(spreadAngle, 0f);
            if (safeSpreadAngle <= 0f)
                return baseDirection;

            Vector2 randomOffset = Random.insideUnitCircle * Mathf.Tan(safeSpreadAngle * Mathf.Deg2Rad);
            Vector3 right = Vector3.Cross(Vector3.up, baseDirection);

            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.Cross(Vector3.forward, baseDirection);

            right.Normalize();
            Vector3 up = Vector3.Cross(baseDirection, right).normalized;
            return (baseDirection + right * randomOffset.x + up * randomOffset.y).normalized;
        }

private void HandleHit(RaycastHit hit, AimResult aimResult, HitscanFireRequest request)
        {
            string aimName = aimResult.didHit ? aimResult.hit.collider.name : "없음";
            string shotName = hit.collider.name;
            Debug.Log($"카메라 조준: {aimName} / 실제 타격: {shotName}");

            HitFeedbackEvents.RaiseHit(hit);

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(request.Damage);
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
            request.EffectPrinter?.PrintFireEffects(request.Muzzle, shotResult, true);

            if (shotResult.didHit)
            {
                HandleHit(shotResult.hit, aimResult, request);
            }

            return true;
        }

        public bool FireShotgun(HitscanFireRequest request, int pelletCount)
        {
            if (!CanFire(request))
                return false;

            AimResult aimResult = ResolveAimPoint(request);
            Vector3 baseDirection = GetBaseShotDirection(aimResult, request);
            int safePelletCount = Mathf.Max(pelletCount, 1);

            AimDirection = baseDirection;

            for (int i = 0; i < safePelletCount; i++)
            {
                Vector3 shotDirection = ApplySpread(baseDirection, request.SpreadAngle);
                float castDistance = GetShotCastDistance(aimResult, request, shotDirection);
                ShotResult shotResult = FireRayFromMuzzle(request, shotDirection, castDistance);

                DrawDebugRays(aimResult, shotResult, request);
                request.EffectPrinter?.PrintFireEffects(request.Muzzle, shotResult, i == 0);

                if (shotResult.didHit)
                {
                    HandleHit(shotResult.hit, aimResult, request);
                }
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
