using _01.Scenes.PhaseValidation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script.Explosion
{
    [DisallowMultipleComponent]
    public class GrenadeProjectile : MonoBehaviour
    {
        [Header("Explosion")]
        [SerializeField] private float explosionTime = 3f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float grenadeDamage = 100f;
        [SerializeField] private LayerMask explosionInteractionLayer;
        [SerializeField] private float delayTime = 2f;

        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private GameObject meshRoot;
        [SerializeField] private GameObject explosionEffect;

        private bool hasExploded;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void Start()
        {
            if (explosionEffect != null)
                explosionEffect.SetActive(false);

            StartCoroutine(ExplosionRoutine());
        }

        private IEnumerator ExplosionRoutine()
        {
            yield return new WaitForSeconds(Mathf.Max(explosionTime, 0f));

            Explode();

            yield return new WaitForSeconds(Mathf.Max(delayTime, 0f));

            Destroy(gameObject);
        }

        private void Explode()
        {
            if (hasExploded)
                return;

            hasExploded = true;

            StopRigidbody();
            SetCollidersEnabled(false);

            if (meshRoot != null)
                meshRoot.SetActive(false);

            PlayExplosionEffect();
            ApplyExplosionDamage();
        }

        private void StopRigidbody()
        {
            if (rb == null)
                return;

            SetLinearVelocity(rb, Vector3.zero);
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            if (colliders == null)
                return;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = isEnabled;
            }
        }

        private void PlayExplosionEffect()
        {
            if (explosionEffect == null)
                return;

            explosionEffect.SetActive(true);

            ParticleSystem[] particleSystems = explosionEffect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null)
                    particleSystems[i].Play(true);
            }

            AudioSource[] audioSources = explosionEffect.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                    audioSources[i].Play();
            }
        }

        private void ApplyExplosionDamage()
        {
            int damage = Mathf.RoundToInt(Mathf.Max(grenadeDamage, 0f));
            if (damage <= 0)
                return;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                Mathf.Max(explosionRadius, 0f),
                explosionInteractionLayer,
                QueryTriggerInteraction.Ignore);

            HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damagedTargets.Contains(damageable))
                    continue;

                damagedTargets.Add(damageable);
                damageable.TakeDamage(damage);
            }
        }

        private void CacheReferences()
        {
            if (rb == null)
                TryGetComponent(out rb);

            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(true);
        }

        private static void SetLinearVelocity(Rigidbody targetRigidbody, Vector3 velocity)
        {
            // Unity 6에서는 linearVelocity, 이전 버전에서는 velocity를 사용합니다.
#if UNITY_6000_0_OR_NEWER
            targetRigidbody.linearVelocity = velocity;
#else
            targetRigidbody.velocity = velocity;
#endif
        }

        private void OnValidate()
        {
            explosionTime = Mathf.Max(explosionTime, 0f);
            explosionRadius = Mathf.Max(explosionRadius, 0f);
            grenadeDamage = Mathf.Max(grenadeDamage, 0f);
            delayTime = Mathf.Max(delayTime, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
