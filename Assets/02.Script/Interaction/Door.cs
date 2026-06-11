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
    }

    public void Interaction()
    {
        Debug.Log("[Door] 상호작용 실행 - 현재 상태: " + (_isDoorOpen ? "열림" : "닫힘"));
        if (!_isDoorOpen && _isActive) Open();
        else                           Close();
    }

    private void Open()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        Vector3 leftTarget  = _leftClosedPos  + new Vector3(-openOffset, 0f, 0f);
        Vector3 rightTarget = _rightClosedPos + new Vector3( openOffset, 0f, 0f);
        _slideCoroutine = StartCoroutine(SlideAll(leftTarget, rightTarget));
        _isDoorOpen = true;
    }

    private void Close()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideAll(_leftClosedPos, _rightClosedPos));
        _isDoorOpen = false;
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

    public void SetDoorActive(bool set) { _isActive = set; }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isDoorOpen
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
