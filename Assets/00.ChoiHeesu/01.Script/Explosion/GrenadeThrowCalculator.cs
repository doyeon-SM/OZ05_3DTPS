using UnityEngine;

namespace _00.ChoiHeesu._01.Script.Explosion
{
    public struct GrenadeThrowData
    {
        public Vector3 TargetPoint;
        public Vector3 ThrowDirection;
        public Vector3 Impulse;
        public Vector3 InitialVelocity;
        public float GrenadeMass;
    }

    public static class GrenadeThrowCalculator
    {
        public static bool TryCalculateThrowData(
            Camera aimCamera,
            Transform throwPoint,
            LayerMask aimLayerMask,
            float rayDistance,
            float fallbackDistance,
            float throwForce,
            float upwardModifier,
            float grenadeMass,
            Vector2 screenPosition,
            out GrenadeThrowData throwData)
        {
            throwData = default;

            if (aimCamera == null || throwPoint == null)
                return false;

            float safeRayDistance = Mathf.Max(rayDistance, 0.01f);
            float safeFallbackDistance = Mathf.Max(fallbackDistance, 0.01f);
            float safeThrowForce = Mathf.Max(throwForce, 0f);
            float safeGrenadeMass = Mathf.Max(grenadeMass, 0.0001f);

            Ray aimRay = aimCamera.ScreenPointToRay(screenPosition);
            Vector3 targetPoint = aimRay.origin + aimRay.direction * safeFallbackDistance;

            if (Physics.Raycast(aimRay, out RaycastHit hit, safeRayDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
                targetPoint = hit.point;

            Vector3 aimDirection = targetPoint - throwPoint.position;
            if (aimDirection.sqrMagnitude <= 0.0001f)
                aimDirection = aimCamera.transform.forward;

            Vector3 throwDirection = (aimDirection.normalized + Vector3.up * Mathf.Max(upwardModifier, 0f)).normalized;
            Vector3 impulse = throwDirection * safeThrowForce;

            throwData = new GrenadeThrowData
            {
                TargetPoint = targetPoint,
                ThrowDirection = throwDirection,
                Impulse = impulse,
                InitialVelocity = impulse / safeGrenadeMass,
                GrenadeMass = safeGrenadeMass
            };

            return true;
        }
    }
}
