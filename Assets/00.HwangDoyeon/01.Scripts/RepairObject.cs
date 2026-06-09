using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수리 오브젝트 — IInteraction 구현
/// 상호작용 1회당 5~12% 랜덤 누적, 1초 쿨타임, 100% 달성 시 수리 완료
/// SliderUI는 Y축 빌보드(좌우만 플레이어 방향)로 항상 플레이어를 향함
/// </summary>
public class RepairObject : MonoBehaviour, IInteraction
{
    // ───────────────────────────── Inspector
    [Header("수리 설정")]
    [Tooltip("1회 상호작용당 최소 수리율 (%)")]
    [SerializeField] private float minRepairPercent = 5f;
    [Tooltip("1회 상호작용당 최대 수리율 (%)")]
    [SerializeField] private float maxRepairPercent = 12f;
    [Tooltip("상호작용 쿨타임 (초)")]
    [SerializeField] private float cooldown = 1f;

    [Header("UI")]
    [Tooltip("오브젝트 위에 떠있는 World Canvas 루트 GameObject")]
    [SerializeField] private GameObject repairSliderUI;
    [Tooltip("World Canvas 안의 Slider 컴포넌트")]
    [SerializeField] private Slider repairSlider;

    [Header("VFX")]
    [Tooltip("수리 중 표시되는 주변 VFX (수리 완료 시 꺼짐)")]
    [SerializeField] private GameObject ambientVFX;

    [Header("SFX")]
    [Tooltip("수리 시도 시 재생되는 AudioSource")]
    [SerializeField] private AudioSource repairSFX;

    // ───────────────────────────── Runtime
    private float     _repairProgress   = 0f;
    private bool      _isRepaired       = false;
    private float     _lastInteractTime = -999f;
    private Transform _playerTransform;

    // ─────────────────────────── Public API
    public bool IsRepaired => _isRepaired;

    // ─────────────────────────── Unity Events
    private void Awake()
    {
        // 플레이어 Transform 캐싱
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null) _playerTransform = camera.transform;

        // 슬라이더 초기화
        if (repairSlider != null)
        {
            repairSlider.minValue = 0f;
            repairSlider.maxValue = 100f;
            repairSlider.value    = 0f;
        }

        // 슬라이더 UI는 첫 상호작용 전까지 숨김
        if (repairSliderUI != null)
            repairSliderUI.SetActive(false);

        // VFX 초기 활성화
        if (ambientVFX != null)
            ambientVFX.SetActive(true);
    }

    private void Update()
    {
        // 슬라이더 UI가 활성화된 동안만 Y축 빌보드 회전 (좌우만, 상하 고정)
        if (repairSliderUI == null || !repairSliderUI.activeSelf) return;
        if (_playerTransform == null) return;

        Vector3 dir = _playerTransform.position - repairSliderUI.transform.position;
        dir.y = 0f; // 상하 회전 고정
        if (dir.sqrMagnitude > 0.001f)
            repairSliderUI.transform.rotation = Quaternion.LookRotation(dir);
    }

    // ─────────────────────────── IInteraction
    public void Interaction()
    {
        if (_isRepaired)
        {
            Debug.Log("[RepairObject] 이미 수리 완료된 오브젝트입니다.");
            return;
        }

        // 쿨타임 체크
        if (Time.time - _lastInteractTime < cooldown)
        {
            Debug.Log($"[RepairObject] 쿨타임 중 ({cooldown - (Time.time - _lastInteractTime):F1}초 남음)");
            return;
        }

        // 첫 상호작용 시 슬라이더 UI 표시
        if (repairSliderUI != null)
            repairSliderUI.SetActive(true);

        _lastInteractTime = Time.time;

        // 수리율 랜덤 증가
        float gain = Random.Range(minRepairPercent, maxRepairPercent);
        _repairProgress = Mathf.Min(_repairProgress + gain, 100f);

        Debug.Log($"[RepairObject] 수리 시도 +{gain:F1}% → 현재 {_repairProgress:F1}%");

        // SFX 재생
        if (repairSFX != null)
            repairSFX.Play();

        // 슬라이더 갱신
        if (repairSlider != null)
            repairSlider.value = _repairProgress;

        // 수리 완료 판정
        if (_repairProgress >= 100f)
            CompleteRepair();
    }

    // ─────────────────────────── Private
    private void CompleteRepair()
    {
        _isRepaired = true;

        if (repairSliderUI != null)
            repairSliderUI.SetActive(false);

        if (ambientVFX != null)
            ambientVFX.SetActive(false);

        Debug.Log("[RepairObject] 수리 완료!");
    }
}
