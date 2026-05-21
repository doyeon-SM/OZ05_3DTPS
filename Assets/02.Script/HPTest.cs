using _01.Scenes.PhaseValidation;
using UnityEngine;

namespace _02.Script.Status
{
    public class HPTest : MonoBehaviour
    {
        [Header("Damage Area")]
        [SerializeField] private BoxCollider boxCollider;

        [Header("Damage Settings")]
        [SerializeField] private int damage = 5;
        [SerializeField] private float damageTickTime = 1f;

        private float currentTime;

        private void Reset()
        {
            boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            currentTime += Time.deltaTime;

            if (currentTime < damageTickTime)
                return;

            currentTime = 0f;

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}