using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteraction
{
    private bool _isDoorOpen;

    [SerializeField] private GameObject leftDoorObject;
    [SerializeField] private GameObject rightDoorObject;

    [Tooltip("문이 열릴 때 이동할 거리 (left: -offset, right: +offset)")]
    [SerializeField] private float openOffset = 1f;

    [Tooltip("문이 이동하는 데 걸리는 시간(초)")]
    [SerializeField] private float slideDuration = 0.5f;

    private Vector3 _leftClosedPos;
    private Vector3 _rightClosedPos;

    private Coroutine _slideCoroutine;

    public bool IsDoorOpen => _isDoorOpen;

    private void Awake()
    {
        if (leftDoorObject != null)
            _leftClosedPos = leftDoorObject.transform.localPosition;

        if (rightDoorObject != null)
            _rightClosedPos = rightDoorObject.transform.localPosition;
    }

    // IInteraction 구현 — InteractionController가 [E]키 입력 시 호출
    public void Interaction()
    {
        Debug.Log("[Door] 상호작용 실행 - 현재 상태: " + (_isDoorOpen ? "열림" : "닫힘"));
        if (!_isDoorOpen) Open();
        else              Close();
    }

    private void Open()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);

        Vector3 leftTarget  = _leftClosedPos  + new Vector3(-openOffset, 0f, 0f);
        Vector3 rightTarget = _rightClosedPos + new Vector3( openOffset, 0f, 0f);

        _slideCoroutine = StartCoroutine(SlideAll(leftTarget, rightTarget));
        _isDoorOpen = true;
        //Debug.Log("[Door] 열림");
    }

    private void Close()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);

        _slideCoroutine = StartCoroutine(SlideAll(_leftClosedPos, _rightClosedPos));
        _isDoorOpen = false;
        //Debug.Log("[Door] 닫힘");
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

        // 최종 위치 정확히 고정
        if (leftDoorObject  != null) leftDoorObject.transform.localPosition  = leftTarget;
        if (rightDoorObject != null) rightDoorObject.transform.localPosition = rightTarget;

        _slideCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isDoorOpen
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
