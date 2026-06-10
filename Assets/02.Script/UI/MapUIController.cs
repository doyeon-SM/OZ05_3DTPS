using UnityEngine;
using StarterAssets;
using System;

/// <summary>
/// 맵 UI에서 플레이어가 속한 구역을 표시하는 컨트롤러.
/// 각 MapZone은 월드 AABB 범위와 해당 구역의 UI 고정 좌표를 가진다.
/// 플레이어가 구역 안에 있으면 playerIcon이 그 구역의 UI 좌표로 이동한다.
/// 어떤 구역에도 속하지 않으면 playerIcon은 숨긴다.
/// </summary>
public class MapUIController : MonoBehaviour
{
    // -------------------------------------------------------
    // 내부 데이터 클래스
    // -------------------------------------------------------
    [Serializable]
    public class MapZone
    {
        [Tooltip("구역 이름 (식별용)")]
        public string zoneName;

        [Tooltip("구역 월드 범위 최솟값 (XZ 평면 기준)")]
        public Vector2 worldMin;

        [Tooltip("구역 월드 범위 최댓값 (XZ 평면 기준)")]
        public Vector2 worldMax;

        [Tooltip("이 구역에 해당하는 playerIcon의 UI anchoredPosition")]
        public Vector2 iconPosition;

        /// <summary>월드 XZ 좌표가 이 구역 안에 있는지 판별</summary>
        public bool Contains(Vector3 worldPos)
        {
            return worldPos.x >= worldMin.x && worldPos.x <= worldMax.x &&
                   worldPos.z >= worldMin.y && worldPos.z <= worldMax.y;
        }
    }

    // -------------------------------------------------------
    // Inspector 필드
    // -------------------------------------------------------
    [Header("맵 UI")]
    [SerializeField] private GameObject mapRootUI;
    [SerializeField] private RectTransform playerIcon;

    [Header("구역 목록")]
    [Tooltip("플레이어 위치를 판별할 구역 리스트. 위쪽 항목이 우선순위가 높다.")]
    [SerializeField] private MapZone[] zones;

    [Header("입력")]
    [Tooltip("Inspector에서 직접 연결하거나 비워두면 런타임에 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private StarterAssetsInputs _input;

    // -------------------------------------------------------
    // 런타임 상태
    // -------------------------------------------------------
    private Transform _playerTransform;
    private bool _isMapOpen;
    private int _currentZoneIndex = -999;   // -1 = 구역 없음

    // -------------------------------------------------------
    // Unity 이벤트
    // -------------------------------------------------------
    private void Awake()
    {
        if (mapRootUI != null)
            mapRootUI.SetActive(false);
    }

    private void Update()
    {
        HandleMapInput();

        //if (_isMapOpen)
        //    UpdatePlayerIcon();
    }

    // -------------------------------------------------------
    // 플레이어 캐싱
    // -------------------------------------------------------
    private bool TryCachePlayer()
    {
        if (_input != null && _playerTransform != null)
            return true;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return false;

        _playerTransform = player.transform;

        if (_input == null)
            _input = player.GetComponent<StarterAssetsInputs>();

        if (_input == null)
        {
            Debug.LogWarning("[MapUIController] StarterAssetsInputs 컴포넌트를 찾을 수 없습니다.");
            return false;
        }

        return true;
    }

    // -------------------------------------------------------
    // 입력 처리
    // -------------------------------------------------------
    private void HandleMapInput()
    {
        if (!TryCachePlayer())
            return;

        if (_input.Map)
        {
            _input.Map = false;
            UpdatePlayerIcon();
            ToggleMap();           
        }
    }

    private void ToggleMap()
    {
        _isMapOpen = !_isMapOpen;
        if (mapRootUI != null)
            mapRootUI.SetActive(_isMapOpen);
    }

    // -------------------------------------------------------
    // 구역 판별 및 아이콘 갱신
    // -------------------------------------------------------
    private void UpdatePlayerIcon()
    {
        if (playerIcon == null || _playerTransform == null || zones == null)
            return;

        int foundIndex = -1;
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].Contains(_playerTransform.position))
            {
                foundIndex = i;
                break;
            }
        }

        // 구역 변경이 없으면 갱신 스킵
        if (foundIndex == _currentZoneIndex)
            return;

        _currentZoneIndex = foundIndex;

        if (foundIndex == -1)
        {
            // 어떤 구역에도 없으면 아이콘 숨김
            playerIcon.gameObject.SetActive(false);
            Debug.Log("[MapUIController] 플레이어가 등록된 구역 밖에 있습니다.");
        }
        else
        {
            // 해당 구역의 고정 UI 좌표로 이동
            playerIcon.gameObject.SetActive(true);
            playerIcon.anchoredPosition = zones[foundIndex].iconPosition;
            Debug.Log($"[MapUIController] 구역 진입: {zones[foundIndex].zoneName} → UI 좌표 {zones[foundIndex].iconPosition}");
        }
    }
}
