using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수리 오브젝트 — IInteraction 구현
/// 상호작용 1회당 5~12% 랜덤 누적, 1초 쿨타임, 100% 달성 시 수리 완료
/// </summary>
public class RepairObject : MonoBehaviour, IInteraction, IPlayerInteractionAnimationTarget
{
    [Header("상호작용 UI")]
    [SerializeField] private string _interactionLabel = "[E] 수리";

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
    [Tooltip("ambientVFX를 몇 초 간격으로 재생할지 (1회성 파티클이라 반복 재생이 필요함)")]
    [SerializeField] private float ambientVfxInterval = 0.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource repairSFX;

    // 수리 완료 시 발행 (StageManager, StageUIManager가 구독)
    public event Action OnRepaired;

    private float     _repairProgress   = 0f;
    private bool      _isRepaired       = false;
    private float     _lastInteractTime = -999f;
    private Transform _playerTransform;
    private ParticleSystem _ambientParticles;
    private Coroutine _ambientVfxCoroutine;

    public bool   IsRepaired => _isRepaired;
    public string RepairName => repairName;
    public bool CanPlayPlayerInteractionAnimation => !_isRepaired && Time.time - _lastInteractTime >= cooldown;

    // IInteraction
    public string InteractionLabel => _interactionLabel;

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

        if (ambientVFX != null)
        {
            ambientVFX.SetActive(true);

            // ambientVFX가 1회성(loop=false) 파티클이라 SetActive(true) 한 번으로는
            // 한 번만 재생되고 끝남 — 코루틴으로 일정 간격마다 다시 Play() 해준다.
            _ambientParticles = ambientVFX.GetComponent<ParticleSystem>();
            if (_ambientParticles == null)
                _ambientParticles = ambientVFX.GetComponentInChildren<ParticleSystem>(true);

            if (_ambientParticles != null)
                _ambientVfxCoroutine = StartCoroutine(AmbientVFXLoop());
            else
                Debug.LogWarning("[RepairObject] ambientVFX에 ParticleSystem이 없어 반복 재생을 할 수 없습니다.");
        }
    }

    /// <summary>ambientVFX(1회성 파티클)를 일정 간격마다 다시 재생한다. 수리 완료 시 정지된다.</summary>
    private IEnumerator AmbientVFXLoop()
    {
        while (!_isRepaired)
        {
            _ambientParticles.Play(true); // withChildren: 자식 파티클도 함께 재생
            yield return new WaitForSeconds(ambientVfxInterval);
        }
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
        if (!CanPlayPlayerInteractionAnimation) { Debug.Log($"[RepairObject] 쿨타임 중"); return; }

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

        if (_ambientVfxCoroutine != null)
        {
            StopCoroutine(_ambientVfxCoroutine);
            _ambientVfxCoroutine = null;
        }

        if (repairSliderUI != null) repairSliderUI.SetActive(false);
        if (ambientVFX != null)     ambientVFX.SetActive(false);
        Debug.Log("[RepairObject] 수리 완료!");
        OnRepaired?.Invoke();
    }
}
