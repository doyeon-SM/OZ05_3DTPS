using _00.ChoiHeesu._01.Script;
using _00.ChoiHeesu._03.WeaponChangeSystem;
using _01.Scenes.PhaseValidation;
using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponEffectPrinter : MonoBehaviour
    {
        [Header("Muzzle Effect")]
        [SerializeField] private WeaponPrefabSetting currentWeaponPrefabSetting;

        [Header("Hit Effect")]
        [SerializeField] private HitEffectPooling hitEffectPooling;
        [SerializeField, HideInInspector] private GameObject hitEffectPrefab;
        [SerializeField, HideInInspector] private float hitEffectLifeTime = 0.2f;
        [SerializeField, HideInInspector] private float hitEffectSurfaceOffset = 0.01f;

        [Header("Bullet Trail")]
        [SerializeField] private bool useBulletTrail = true;
        [SerializeField] private float bulletTrailLifeTime = 0.05f;
        [SerializeField] private float bulletTrailStartWidth = 0.02f;
        [SerializeField] private float bulletTrailEndWidth = 0.005f;
        [SerializeField] private Material bulletTrailMaterial;
        [SerializeField] private Color bulletTrailStartColor = Color.white;
        [SerializeField] private Color bulletTrailEndColor = new Color(1f, 1f, 1f, 0f);

        private Material runtimeBulletTrailMaterial;

        private void OnDestroy()
        {
            if (runtimeBulletTrailMaterial != null)
                Destroy(runtimeBulletTrailMaterial);
        }

        public void SetCurrentWeaponPrefabSetting(WeaponPrefabSetting nextWeaponPrefabSetting)
        {
            currentWeaponPrefabSetting = nextWeaponPrefabSetting;
        }

        public void StopMuzzleEffect()
        {
            if (currentWeaponPrefabSetting == null)
                return;

            currentWeaponPrefabSetting.StopMuzzleEffect();
        }

        public void ApplyHitEffectSettingsIfEmpty(GameObject fallbackHitEffectPrefab, float fallbackHitEffectLifeTime)
        {
            if (hitEffectPrefab == null && fallbackHitEffectPrefab != null)
            {
                hitEffectPrefab = fallbackHitEffectPrefab;
                hitEffectLifeTime = Mathf.Max(fallbackHitEffectLifeTime, 0.01f);
            }

            if (TryGetHitEffectPooling(out HitEffectPooling pooling))
                ApplyLegacyHitEffectSettings(pooling);
        }

        public void PrintFireEffects(Transform muzzle, ShotResult shotResult, bool playMuzzleEffect)
        {
            if (playMuzzleEffect)
                PrintMuzzleEffect(muzzle);

            PrintBulletTrail(shotResult);

            if (shotResult.didHit)
                PrintHitEffect(shotResult.hit);
        }

        private void PrintMuzzleEffect(Transform muzzle)
        {
            if (currentWeaponPrefabSetting == null && muzzle != null)
                currentWeaponPrefabSetting = muzzle.GetComponentInParent<WeaponPrefabSetting>(true);

            if (currentWeaponPrefabSetting == null)
                return;

            currentWeaponPrefabSetting.PlayMuzzleEffect();
        }

        private void PrintHitEffect(RaycastHit hit)
        {
            if (!TryGetHitEffectPooling(out HitEffectPooling pooling))
                return;

            ApplyLegacyHitEffectSettings(pooling);

            Vector3 hitNormal = GetSafeHitNormal(hit);
            Vector3 spawnPosition = hit.point + hitNormal * pooling.HitEffectSurfaceOffset;
            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            pooling.TryPlayHitEffect(spawnPosition, spawnRotation);
        }

        private void PrintBulletTrail(ShotResult shotResult)
        {
            if (!useBulletTrail)
                return;

            if (bulletTrailLifeTime <= 0f)
                return;

            Vector3 endPoint = GetBulletTrailEndPoint(shotResult);
            if ((endPoint - shotResult.origin).sqrMagnitude < 0.0001f)
                return;

            GameObject trailObject = new GameObject("BulletTrail");
            LineRenderer lineRenderer = trailObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, shotResult.origin);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.startWidth = Mathf.Max(bulletTrailStartWidth, 0f);
            lineRenderer.endWidth = Mathf.Max(bulletTrailEndWidth, 0f);
            lineRenderer.startColor = bulletTrailStartColor;
            lineRenderer.endColor = bulletTrailEndColor;
            lineRenderer.numCapVertices = 2;

            Material trailMaterial = GetBulletTrailMaterial();
            if (trailMaterial != null)
                lineRenderer.material = trailMaterial;

            Destroy(trailObject, bulletTrailLifeTime);
        }

        private Material GetBulletTrailMaterial()
        {
            if (bulletTrailMaterial != null)
                return bulletTrailMaterial;

            if (runtimeBulletTrailMaterial != null)
                return runtimeBulletTrailMaterial;

            Shader trailShader = Shader.Find("Sprites/Default");
            if (trailShader == null)
                trailShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (trailShader == null)
                trailShader = Shader.Find("Unlit/Color");

            if (trailShader == null)
                return null;

            runtimeBulletTrailMaterial = new Material(trailShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            return runtimeBulletTrailMaterial;
        }

        private Vector3 GetBulletTrailEndPoint(ShotResult shotResult)
        {
            if (shotResult.didHit)
                return shotResult.hit.point;

            Vector3 direction = shotResult.direction.sqrMagnitude > 0.0001f
                ? shotResult.direction.normalized
                : transform.forward;

            return shotResult.origin + direction * Mathf.Max(shotResult.distance, 0f);
        }

        private Vector3 GetSafeHitNormal(RaycastHit hit)
        {
            if (hit.normal.sqrMagnitude < 0.0001f)
                return Vector3.up;

            return hit.normal.normalized;
        }

        private bool TryGetHitEffectPooling(out HitEffectPooling pooling)
        {
            if (hitEffectPooling != null)
            {
                pooling = hitEffectPooling;
                return true;
            }

            WeaponRuntimeManager runtimeManager = WeaponRuntimeManager.Instance;
            if (runtimeManager != null && runtimeManager.TryGetComponent(out hitEffectPooling))
            {
                pooling = hitEffectPooling;
                return true;
            }

            hitEffectPooling = FindFirstObjectByType<HitEffectPooling>(FindObjectsInactive.Include);
            pooling = hitEffectPooling;
            return pooling != null;
        }

        private void ApplyLegacyHitEffectSettings(HitEffectPooling pooling)
        {
            if (pooling == null || hitEffectPrefab == null)
                return;

            pooling.ConfigureIfEmpty(hitEffectPrefab, hitEffectLifeTime, hitEffectSurfaceOffset);
        }
    }
}
