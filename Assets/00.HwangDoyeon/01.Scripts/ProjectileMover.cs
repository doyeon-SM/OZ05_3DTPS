using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace TurretDemo
{
    /// <summary>
    /// 오브젝트 풀링 방식의 발사체 이동 컴포넌트.
    /// - Player 태그에 닿으면 TakeDamage 후 풀 반환.
    /// - 수명 초과 시 풀 반환.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileMover : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("초당 이동 거리(월드 단위).")]
        private float moveSpeedUnitsPerSecond = 12f;

        [SerializeField]
        [Tooltip("생존 시간(초) 이후 비활성화.")]
        private float lifeTimeSeconds = 3f;

        [SerializeField]
        [Tooltip("Projectile 1발당 데미지량.")]
        private float damageAmount = 10f;

        private Vector3 fireDirection;
        private float   remainingLifeSeconds;
        private bool    isLaunched;

        public void Launch(Vector3 worldPosition, Vector3 worldDirection,
                           float speed, float lifeTime, float damage)
        {
            fireDirection           = worldDirection.normalized;
            moveSpeedUnitsPerSecond = speed;
            lifeTimeSeconds         = lifeTime;
            damageAmount            = damage;
            remainingLifeSeconds    = lifeTime;

            transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.LookRotation(fireDirection));

            isLaunched = true;
            gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            isLaunched = false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isLaunched) return;

            transform.position += fireDirection * (moveSpeedUnitsPerSecond * Time.deltaTime);

            remainingLifeSeconds -= Time.deltaTime;
            if (remainingLifeSeconds <= 0f)
                ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isLaunched) return;
            if (!other.CompareTag("Player")) return;

            PlayerStatus ps = other.GetComponentInParent<PlayerStatus>();
            if (ps == null) return;

            ps.TakeDamage((int)damageAmount);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            isLaunched = false;
            gameObject.SetActive(false);
        }
    }
}
