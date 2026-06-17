using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 전용 HUD — 마인크래프트 스타일 세그먼트 분할 HP바.
    /// 
    /// [구조]
    ///  HUDRoot (Canvas)
    ///   └ BossNameText (TMP)
    ///   └ SegmentContainer (HorizontalLayoutGroup)
    ///        └ Segment_0 ... Segment_N  (Image × segmentCount)
    ///             └ Image.fillAmount 으로 칸별 HP 표현
    ///
    /// [사용법]
    ///  BossSector에서 BossStatus.OnHPChanged를 이 Manager에 연결합니다.
    ///  Show(bossName, maxHP) → 등장 / Hide() → 사망/퇴장
    /// </summary>
    public class BossHUDManager : MonoBehaviour
    {
        public static BossHUDManager Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TextMeshProUGUI bossNameText;

        [Header("세그먼트 HP바")]
        [Tooltip("세그먼트(칸) 프리팹 — Image 컴포넌트가 있어야 합니다.")]
        [SerializeField] private Image segmentPrefab;

        [Tooltip("세그먼트들을 담을 부모 Transform (HorizontalLayoutGroup 권장)")]
        [SerializeField] private Transform segmentContainer;

        [Tooltip("HP바를 몇 칸으로 나눌지 (페이즈 수와 맞추세요)")]
        [SerializeField] private int segmentCount = 3;

        [Header("색상")]
        [SerializeField] private Color fullColor    = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] private Color emptyColor   = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color segmentGapColor = new Color(0f, 0f, 0f, 0.8f);

        [Tooltip("분기패턴(무적) 진행 중 채워진 세그먼트에 적용할 색상")]
        [SerializeField] private Color invincibleColor = new Color(0.2f, 0.5f, 0.95f);

        // 생성된 세그먼트 Image 배열
        private Image[] _segments;
        private int _maxHP;
        private int _currentHP;
        private bool _isInvincible;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (hudRoot != null) hudRoot.SetActive(false);
        }

        /// <summary>
        /// 보스 등장 시 HUD를 초기화하고 표시합니다.
        /// </summary>
        public void Show(string bossName, int maxHP)
        {
            _maxHP     = maxHP;
            _currentHP = maxHP;

            if (bossNameText != null)
                bossNameText.text = bossName;

            BuildSegments();
            RefreshSegments();

            if (hudRoot != null) hudRoot.SetActive(true);
        }

        /// <summary>
        /// 보스 사망 또는 퇴장 시 HUD를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            if (hudRoot != null) hudRoot.SetActive(false);
        }

        /// <summary>
        /// BossStatus.OnHPChanged 이벤트 수신 — HP바 갱신.
        /// </summary>
        public void OnHPChanged(int current, int max)
        {
            _currentHP = current;
            _maxHP     = max;
            RefreshSegments();
        }

        /// <summary>
        /// 보스의 무적(분기패턴) 상태를 HUD에 반영합니다.
        /// BossController가 분기패턴 시작/종료 시점에 호출합니다.
        /// </summary>
        public void SetInvincibleVisual(bool isInvincible)
        {
            _isInvincible = isInvincible;
            RefreshSegments();
        }

        // ── 내부 ──────────────────────────────────────────

        /// <summary>
        /// segmentCount 수만큼 세그먼트 Image를 동적 생성합니다.
        /// 이미 생성되어 있으면 재생성합니다.
        /// </summary>
        private void BuildSegments()
        {
            if (segmentPrefab == null || segmentContainer == null)
            {
                Debug.LogError("[BossHUDManager] segmentPrefab 또는 segmentContainer가 연결되지 않았습니다.");
                return;
            }

            // 기존 세그먼트 제거
            foreach (Transform child in segmentContainer)
                Destroy(child.gameObject);

            _segments = new Image[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                Image seg = Instantiate(segmentPrefab, segmentContainer);
                seg.type       = Image.Type.Filled;
                seg.fillMethod = Image.FillMethod.Horizontal;
                seg.fillAmount = 1f;
                seg.color      = fullColor;
                _segments[i]   = seg;
            }
        }

        /// <summary>
        /// 현재 HP 비율에 따라 각 세그먼트의 fillAmount와 색상을 갱신합니다.
        /// 칸당 HP = maxHP / segmentCount
        /// </summary>
        private void RefreshSegments()
        {
            if (_segments == null || _segments.Length == 0) return;
            if (_maxHP <= 0) return;

            float hpPerSegment = (float)_maxHP / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                if (_segments[i] == null) continue;

                // 이 세그먼트가 담당하는 HP 범위
                float segMin = hpPerSegment * i;
                float segMax = hpPerSegment * (i + 1);

                float fill;
                if (_currentHP >= segMax)
                    fill = 1f;                                          // 완전히 찬 칸
                else if (_currentHP <= segMin)
                    fill = 0f;                                          // 완전히 빈 칸
                else
                    fill = (_currentHP - segMin) / hpPerSegment;       // 부분적으로 채워진 칸

                _segments[i].fillAmount = fill;
                _segments[i].color      = fill > 0f ? (_isInvincible ? invincibleColor : fullColor) : emptyColor;
            }
        }
    }
}
