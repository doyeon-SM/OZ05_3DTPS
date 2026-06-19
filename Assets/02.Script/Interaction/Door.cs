using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteraction
{
    [Header("상호작용 UI")]
    [SerializeField] private string _interactionLabel_open = "[E] 열기";
    [SerializeField] private string _interactionLabel_close = "[E] 닫기";

    private bool _isDoorOpen;
    private bool _isActive = true;

    [SerializeField] private GameObject leftDoorObject;
    [SerializeField] private GameObject rightDoorObject;

    [Tooltip("문이 열릴 때 이동할 거리 (left: -offset, right: +offset)")]
    [SerializeField] private float openOffset = 1f;

    [Tooltip("문이 이동하는 데 걸리는 시간(초)")]
    [SerializeField] private float slideDuration = 0.5f;

    [Header("잠금 표시")]
    [Tooltip("상호작용이 막힌 상태(비활성)일 때만 표시할 오브젝트. SetDoorActive(false)이면 ON, SetDoorActive(true)이면 OFF.")]
    [SerializeField] private GameObject _lockedIndicatorObject;

    [Header("SFX")]
    [Tooltip("열릴 때 재생할 SFX")]
    [SerializeField] private AudioClip openSFX;
    [Tooltip("닫힐 때 재생할 SFX (비워두면 열기 SFX 재용)")]
    [SerializeField] private AudioClip closeSFX;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource _audioSource;

    private Vector3 _leftClosedPos;
    private Vector3 _rightClosedPos;

    private Coroutine _slideCoroutine;

    public bool IsDoorOpen   => _isDoorOpen;
    public bool IsDoorActive => _isActive;

    // IInteraction
    public string InteractionLabel => _isDoorOpen ? _interactionLabel_close : _interactionLabel_open;

    private void Awake()
    {
        if (leftDoorObject  != null) _leftClosedPos  = leftDoorObject.transform.localPosition;
        if (rightDoorObject != null) _rightClosedPos = rightDoorObject.transform.localPosition;

        // AudioSource 자동 추가 (없으면 생성)
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake  = false;
        _audioSource.spatialBlend = 1f; // 3D 사운드

        // 초기 상태(_isActive = true)에 맞춰 잠금 표시 오브젝트 동기화
        ApplyLockedIndicator();
    }

    public void Interaction()
    {
        // 비활성 상태(SetDoorActive(false))면 어떤 동작도 하지 않는다.
        if (!_isActive)
        {
            Debug.Log("[Door] 비활성 상태 — 상호작용 무시");
            return;
        }

        Debug.Log("[Door] 상호작용 실행 - 현재 상태: " + (_isDoorOpen ? "열림" : "닫힘"));
        if (!_isDoorOpen) Open();
        else              Close();
    }

    private void PlaySFX(bool isOpening)
    {
        AudioClip clip = isOpening ? openSFX : (closeSFX != null ? closeSFX : openSFX);
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void Open()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        Vector3 leftTarget  = _leftClosedPos  + new Vector3(-openOffset, 0f, 0f);
        Vector3 rightTarget = _rightClosedPos + new Vector3( openOffset, 0f, 0f);
        _slideCoroutine = StartCoroutine(SlideAll(leftTarget, rightTarget));
        _isDoorOpen = true;
        PlaySFX(true);
    }

    private void Close()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideAll(_leftClosedPos, _rightClosedPos));
        _isDoorOpen = false;
        PlaySFX(false);
    }

    private IEnumerator SlideAll(Vector3 leftTarget, Vector3 rightTarget)
    {
        Vector3 leftStart  = leftDoorObject  != null ? leftDoorObject.transform.localPosition  : leftTarget;
        Vector3 rightStart = rightDoorObject != null ? rightDoorObject.transform.localPosition : rightTarget;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            if (leftDoorObject  != null) leftDoorObject.transform.localPosition  = Vector3.Lerp(leftStart,  leftTarget,  t);
            if (rightDoorObject != null) rightDoorObject.transform.localPosition = Vector3.Lerp(rightStart, rightTarget, t);
            yield return null;
        }
        if (leftDoorObject  != null) leftDoorObject.transform.localPosition  = leftTarget;
        if (rightDoorObject != null) rightDoorObject.transform.localPosition = rightTarget;
        _slideCoroutine = null;
    }

    public void SetDoorActive(bool set)
    {
        _isActive = set;
        ApplyLockedIndicator();
    }

    /// <summary>_isActive 상태에 맞춰 잠금 표시 오브젝트를 켜고 끈다.</summary>
    private void ApplyLockedIndicator()
    {
        if (_lockedIndicatorObject != null)
            _lockedIndicatorObject.SetActive(!_isActive);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isDoorOpen
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
