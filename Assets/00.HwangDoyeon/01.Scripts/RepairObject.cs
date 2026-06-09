using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수리 오브젝트 — IInteraction 구현
/// 상호작용 1회당 5~12% 랜덤 누적, 1초 쿨타임, 100% 달성 시 수리 완료
/// </summary>
public class RepairObject : MonoBehaviour, IInteraction
{
    [Header("수리할 물건 장소/이름")]
    [SerializeField] private string repairName;

    [Header("수리 설정")]
    [SerializeField] private float minRepairPercent = 5f;
    [SerializeField] private float maxRepairPercent = 12f;
    [SerializeField] private float cooldown = 1f;

    [Header("UI")]
    [SerializeField] private GameObject repairSliderUI;
    [SerializeField] private Slider repairSlider;

    [Header("VFX")]
    [SerializeField] private GameObject ambientVFX;

    [Header("SFX")]
    [SerializeField] private AudioSource repairSFX;

    // 수리 완료 시 발행 (StageManager, StageUIManager가 구독)
    public event Action OnRepaired;

    private float     _repairProgress   = 0f;
    private bool      _isRepaired       = false;
    private float     _lastInteractTime = -999f;
    private Transform _playerTransform;

    public bool   IsRepaired => _isRepaired;
    public string RepairName => repairName;

    private void Awake()
    {
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null) _playerTransform = camera.transform;

        if (repairSlider != null)
        {
            repairSlider.minValue = 0f;
            repairSlider.maxValue = 100f;
            repairSlider.value    = 0f;
        }
        if (repairSliderUI != null) repairSliderUI.SetActive(false);
        if (ambientVFX != null)     ambientVFX.SetActive(true);
    }

    private void Update()
    {
        if (repairSliderUI == null || !repairSliderUI.activeSelf) return;
        if (_playerTransform == null) return;

        Vector3 dir = _playerTransform.position - repairSliderUI.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            repairSliderUI.transform.rotation = Quaternion.LookRotation(dir);
    }

    public void Interaction()
    {
        if (_isRepaired) { Debug.Log("[RepairObject] 이미 수리 완료."); return; }
        if (Time.time - _lastInteractTime < cooldown) { Debug.Log($"[RepairObject] 쿨타임 중"); return; }

        if (repairSliderUI != null) repairSliderUI.SetActive(true);

        _lastInteractTime = Time.time;
        float gain = UnityEngine.Random.Range(minRepairPercent, maxRepairPercent);
        _repairProgress = Mathf.Min(_repairProgress + gain, 100f);

        if (repairSFX != null)    repairSFX.Play();
        if (repairSlider != null) repairSlider.value = _repairProgress;

        Debug.Log($"[RepairObject] +{gain:F1}% → {_repairProgress:F1}%");

        if (_repairProgress >= 100f) CompleteRepair();
    }

    private void CompleteRepair()
    {
        _isRepaired = true;
        if (repairSliderUI != null) repairSliderUI.SetActive(false);
        if (ambientVFX != null)     ambientVFX.SetActive(false);

        Debug.Log("[RepairObject] 수리 완료!");

        // 완료 이벤트 발행
        OnRepaired?.Invoke();
    }
}
