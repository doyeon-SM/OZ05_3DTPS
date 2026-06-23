using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 바닥패턴3 "컨테이너" — 보스 자식으로 풀링되는 낙하형 장애물.
    ///
    /// [흐름]
    ///  1. BossFloorPatternController가 비활성 상태의 이 오브젝트를 활성화하고,
    ///     낙하 시작 위치(목표 지점 XZ, 로컬 Y는 그대로 -4 유지된 시작 높이)로 이동시킨 뒤 StartFalling() 호출.
    ///  2. fallSpeed(4m/s)로 낙하. 낙하 중 Player와 충돌 시 1회성 데미지(containerDamageMultiplier 적용).
    ///  3. 바닥(목표 Y)에 도달하면 정지 — 이후 상호작용(IInteraction) 대기 상태.
    ///  4. 상호작용(Interaction()) 시 비활성화 + 위치를 기본 대기 위치((0,8,0) 로컬)로 초기화.
    ///
    /// [참고]
    ///  - 컨테이너가 활성화 상태인 동안 BossFloorPatternController는 패턴3을 선택하지 않는다 (IsActive로 확인).
    /// </summary>
    public class BossPatternContainer : MonoBehaviour, IInteraction
    {
        private const string PlayerTag = "Player";

        [Header("낙하 설정")]
        [Tooltip("낙하 속도 (m/s)")]
        [SerializeField] private float fallSpeed = 4f;

        [Header("데미지")]
        [Tooltip("낙하 중 충돌 시 데미지 배율 (BossData.attackPower 기준)")]
        [SerializeField] private float containerDamageMultiplier = 1.2f;

        [Header("기본 대기 위치 (로컬)")]
        [SerializeField] private Vector3 idleLocalPosition = new Vector3(0f, 8f, 0f);

        [Header("상호작용")]
        [SerializeField] private string interactionLabel = "[E] 컨테이너 회수";

        public string InteractionLabel => interactionLabel;

        /// <summary>현재 낙하 중이거나 바닥에 정착해 상호작용을 기다리는 중인지 여부.</summary>
        public bool IsActive => gameObject.activeSelf;

        /// <summary>낙하를 시작할 대기 위치의 로컬 Y 좌표.</summary>
        public float IdleLocalY => idleLocalPosition.y;

        private bool _isFalling;
        private float _targetLocalY;
        private int _tickDamage;
        private bool _hasHitPlayer;

        [Header("상호작용 보상 (탄약)")]
        [Tooltip("상호작용 시 지급할 SMG 탄약 수량")]
        [SerializeField] private int smgAmmoReward = 50;
        [Tooltip("상호작용 시 지급할 샷건 탄약 수량")]
        [SerializeField] private int shotgunAmmoReward = 5;

        private const string SmgAmmoItemId = "smgammo";
        private const string ShotgunAmmoItemId = "sgammo";

        [Header("상호작용 보상 (체력)")]
        [Tooltip("상호작용 시 회복할 플레이어 체력")]
        [SerializeField] private int healthReward = 20;

        private PlayerInventory _playerInventory;
        private PlayerStatus _playerStatus;

        /// <summary>
        /// 낙하를 시작한다. 호출 전 transform.localPosition의 X,Z는 목표 지점으로,
        /// Y는 낙하 시작 높이(기존 idle Y)로 설정되어 있어야 한다.
        /// </summary>
        /// <param name="targetLocalY">바닥(목표) 로컬 Y 좌표 (예: -4)</param>
        /// <param name="tickDamage">충돌 시 적용할 데미지 (이미 배율 적용된 값)</param>
        public void StartFalling(float targetLocalY, int tickDamage)
        {
            _targetLocalY = targetLocalY;
            _tickDamage = tickDamage;
            _hasHitPlayer = false;
            _isFalling = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isFalling) return;

            Vector3 pos = transform.localPosition;
            pos.y -= fallSpeed * Time.deltaTime;

            if (pos.y <= _targetLocalY)
            {
                pos.y = _targetLocalY;
                _isFalling = false;
            }

            transform.localPosition = pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isFalling || _hasHitPlayer) return;
            if (!other.CompareTag(PlayerTag)) return;

            PlayerStatus playerStatus = other.GetComponentInParent<PlayerStatus>();
            if (playerStatus == null) return;

            _hasHitPlayer = true;
            playerStatus.TakeDamage(_tickDamage);
            Debug.Log($"[BossPatternContainer] 낙하 충돌 데미지 | damage={_tickDamage}");
        }

        /// <summary>BossData.attackPower 기준 컨테이너 데미지 계산.</summary>
        public int CalculateDamage(int baseAttackPower)
        {
            return Mathf.RoundToInt(baseAttackPower * containerDamageMultiplier);
        }

        // ── IInteraction ──────────────────────────────────────

        public void Interaction()
        {
            GrantHealthReward();
            GrantAmmoReward();

            _isFalling = false;
            gameObject.SetActive(false);
            transform.localPosition = idleLocalPosition;
            Debug.Log("[BossPatternContainer] 상호작용 - 컨테이너 비활성화 및 위치 초기화");
        }

        /// <summary>상호작용 보상으로 SMG/샷건 탄약을 플레이어 인벤토리에 지급한다.</summary>
        /// <summary>상호작용 보상으로 플레이어 체력을 회복한다.</summary>
        private void GrantHealthReward()
        {
            if (!CachePlayerStatus())
            {
                Debug.LogWarning("[BossPatternContainer] PlayerStatus를 찾을 수 없어 체력 보상을 지급하지 못했습니다.");
                return;
            }

            _playerStatus.Heal(healthReward);
            Debug.Log($"[BossPatternContainer] 체력 보상 지급 | +{healthReward}");
        }

        private void GrantAmmoReward()
        {
            if (!CachePlayerInventory())
            {
                Debug.LogWarning("[BossPatternContainer] PlayerInventory를 찾을 수 없어 탄약 보상을 지급하지 못했습니다.");
                return;
            }

            if (_playerInventory.TryAddItemsFromPickup(SmgAmmoItemId, smgAmmoReward, out int addedSmg))
                _playerInventory.EnqueuePickupMessage(SmgAmmoItemId, addedSmg);
            else
                Debug.LogWarning($"[BossPatternContainer] SMG 탄약 지급 실패 | itemId={SmgAmmoItemId}");

            if (_playerInventory.TryAddItemsFromPickup(ShotgunAmmoItemId, shotgunAmmoReward, out int addedSg))
                _playerInventory.EnqueuePickupMessage(ShotgunAmmoItemId, addedSg);
            else
                Debug.LogWarning($"[BossPatternContainer] 샷건 탄약 지급 실패 | itemId={ShotgunAmmoItemId}");
        }

        private bool CachePlayerInventory()
        {
            if (_playerInventory != null) return true;
            _playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            return _playerInventory != null;
        }

        private bool CachePlayerStatus()
        {
            if (_playerStatus != null) return true;
            _playerStatus = FindFirstObjectByType<PlayerStatus>(FindObjectsInactive.Include);
            return _playerStatus != null;
        }
    }
}
