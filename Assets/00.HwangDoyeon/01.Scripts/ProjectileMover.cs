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
            // 1. 상태 설정
            fireDirection           = worldDirection.normalized;
            moveSpeedUnitsPerSecond = speed;
            lifeTimeSeconds         = lifeTime;
            damageAmount            = damage;
            remainingLifeSeconds    = lifeTime;
            isLaunched              = true;

            // 2. 위치·회전을 월드 기준으로 설정 (SetActive 이전에 반드시 처리)
            transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.LookRotation(fireDirection));

            // 3. 활성화 (이 시점에 이미 올바른 위치가 설정되어 있음)
            gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            // isLaunched만 초기화 — 위치 초기화는 하지 않음
            // (localPosition = Vector3.zero 가 터렛 회전 시 본체 내부를 가리켜
            //  다음 Launch에서 오작동을 일으키므로 제거)
            isLaunched = false;
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
            /*if (!other.CompareTag("Player"))
            {
                ReturnToPool();
                return; 
            }*/


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

        /// <summary>
        /// 풀이 고갈됐을 때 외부에서 강제로 슬롯을 회수합니다.
        /// </summary>
        public void ForceReturn()
        {
            ReturnToPool();
        }
    }
}
