using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 전용 특수패턴 "바닥패턴" 컨트롤러.
    ///
    /// [격자 정의]
    ///  - 보스(BossTransform) 위치를 중심으로 18m x 18m, 9x9 타일(타일당 2m x 2m).
    ///  - 타일 인덱스 i (0~8) → 중심 좌표 = bossPos + (-8 + i*2)
    ///  - 타일 범위 = [중심 - 1, 중심 + 1]
    ///  - 가로줄(행, row): Z 고정, X 전체(18m) — index는 Z방향 타일 인덱스
    ///  - 세로줄(열, col): X 고정, Z 전체(18m) — index는 X방향 타일 인덱스
    ///
    /// [패턴1: "#자"]
    ///  - 홀수(1,3,5,7,9번째) 또는 짝수(2,4,6,8번째) 줄을 랜덤 선택
    ///  - 가로줄들 동시 예고(2초) → 동시 공격 → 세로줄들 동시 예고(2초) → 동시 공격
    ///
    /// [패턴2: "파도"]
    ///  - 상/하/좌/우 중 랜덤 방향 선택
    ///  - 상/하: 세로줄(X 고정) 9개를 Z값 큰→작은(상) 또는 작은→큰(하) 순서로 0.2초 간격 예고(각 2초) 후 공격(파도타기)
    ///  - 좌/우: 가로줄(Z 고정) 9개를 X값 작은→큰(좌) 또는 큰→작은(우) 순서로 동일하게 진행
    ///
    /// [패턴3: "컨테이너"]
    ///  - 컨테이너가 비활성 상태일 때만 사용 가능
    ///  - 중앙 3x3(6m x 6m) 칸을 제외한 격자 중 랜덤 1칸에 원형 예고(0.5m→2m, 3초) 표시
    ///  - 동시에 컨테이너를 해당 위치 상공에서 4m/s로 낙하 시작
    ///  - 낙하 중 플레이어와 충돌 시 1회 데미지(attackPower * containerDamageMultiplier)
    ///  - 바닥 도착 후 상호작용 대기 상태로 전환, 상호작용 시 비활성화+초기화
    ///
    /// [데미지]
    ///  - 패턴1/2: BossData.attackPower * floorPatternMultiplier
    ///  - 패턴3: BossPatternContainer.CalculateDamage(attackPower) (자체 배율)
    /// </summary>
    public class BossFloorPatternController : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const string BossSpawnName = "BossSpawn";
        private const int GridSize = 9;
        private const float TileSize = 2f;
        private const float GridHalfExtent = 9f; // 9x9 grid, half of 18m

        [Header("보스 데이터")]
        [SerializeField] private BossStatus bossStatus;

        [Tooltip("공격 VFX/SFX 처리 컴포넌트 (비워두면 자동으로 GetComponentInParent 시도)")]
        [SerializeField] private BossEffectController effectController;

        [Tooltip("격자 중심으로 사용할 Transform (보통 보스 본체). 비워두면 this.transform 사용.")]
        [SerializeField] private Transform gridCenter;

        [Header("격자 예고 풀 (최대 9개)")]
        [SerializeField] private List<BossPatternFloorIndicator> floorIndicatorPool;

        [Header("컨테이너 패턴")]
        [SerializeField] private BossPatternContainer container;
        [SerializeField] private BossPatternCircleIndicator circleIndicator;

        [Header("패턴1/2 설정")]
        [Tooltip("줄 예고 시간(초)")]
        [SerializeField] private float lineTelegraphDuration = 2f;

        [Tooltip("패턴2 파도 - 줄과 줄 사이 시작 간격(초)")]
        [SerializeField] private float waveStepInterval = 0.2f;

        [Tooltip("예고 후 공격 표시 유지 시간(초)")]
        [SerializeField] private float attackShowDuration = 0.3f;

        [Header("다음 배율")]
        [Tooltip("패턴1/2(#자, 파도) 데미지 배율")]
        [SerializeField] private float floorPatternMultiplier = 1f;

        private void Awake()
        {
            if (effectController == null) effectController = GetComponentInParent<BossEffectController>();

            if (gridCenter == null)
            {
                GameObject spawn = GameObject.Find(BossSpawnName);
                if (spawn != null)
                {
                    gridCenter = spawn.transform;
                }
                else
                {
                    Debug.LogWarning($"[BossFloorPatternController] '{BossSpawnName}' 오브젝트를 찾지 못해 gridCenter를 자기 자신으로 설정합니다.");
                }
            }

            ReparentToGridCenter();
        }

        /// <summary>
        /// 인디케이터/컨테이너가 보스의 자식으로 있으면 보스 회전에 영향을 받으므로,
        /// gridCenter(BossSpawn, 회전 없음) 자식으로 재배치하여 월드 좌표/회전을 보존한다.
        /// </summary>
        private void ReparentToGridCenter()
        {
            if (gridCenter == null) return;

            for (int i = 0; i < floorIndicatorPool.Count; i++)
            {
                var indicator = floorIndicatorPool[i];
                if (indicator == null) continue;
                ReparentKeepWorld(indicator.transform, gridCenter);
            }

            if (circleIndicator != null)
                ReparentKeepWorld(circleIndicator.transform, gridCenter);

            if (container != null)
                ReparentKeepWorld(container.transform, gridCenter);
        }

        /// <summary>월드 위치/회전/스케일을 유지하며 부모를 변경한다.</summary>
        private void ReparentKeepWorld(Transform t, Transform newParent)
        {
            if (t.parent == newParent) return;

            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;
            Vector3 worldScale = t.lossyScale;

            t.SetParent(newParent, true); // worldPositionStays = true

            // SetParent(true)가 lossyScale을 완전히 보존하지 못하는 경우(부모 스케일 차이) 대비 재보정
            t.position = worldPos;
            t.rotation = worldRot;
        }

        private Transform GridCenterTransform => gridCenter != null ? gridCenter : transform;

        // ── 외부 진입점 ─────────────────────────────────────

        /// <summary>
        /// BossController에서 특수패턴 차례에 호출.
        /// 패턴1/2/3 중 랜덤 선택 (컨테이너 활성 상태면 1/2 중에서만 선택) 후 실행.
        /// </summary>
        public IEnumerator PlaySpecialPattern()
        {
            bool containerAvailable = container == null || !container.IsActive;

            int choice = containerAvailable
                ? Random.Range(0, 3)  // 0,1,2
                : Random.Range(0, 2); // 0,1

            switch (choice)
            {
                case 0:
                    yield return StartCoroutine(PlayHashPattern());
                    break;
                case 1:
                    yield return StartCoroutine(PlayWavePattern());
                    break;
                case 2:
                    yield return StartCoroutine(PlayContainerPattern());
                    break;
            }
        }

        // ── 격자 좌표 헬퍼 ───────────────────────────────────

        /// <summary>타일 인덱스(0~8) → 해당 축의 중심 월드 좌표 오프셋 (-8, -6, ..., 8).</summary>
        private float TileCenterOffset(int index) => -8f + index * 2f;

        /// <summary>가로줄(행) - Z 고정(index), X 전체. 반환: (worldCenter, sizeX, sizeZ)</summary>
        private (Vector3 center, float sizeX, float sizeZ) GetRowRect(int zIndex)
        {
            Vector3 origin = GridCenterTransform.position;
            float z = origin.z + TileCenterOffset(zIndex);
            Vector3 center = new Vector3(origin.x, origin.y, z);
            return (center, GridHalfExtent * 2f, TileSize);
        }

        /// <summary>세로줄(열) - X 고정(index), Z 전체. 반환: (worldCenter, sizeX, sizeZ)</summary>
        private (Vector3 center, float sizeX, float sizeZ) GetColRect(int xIndex)
        {
            Vector3 origin = GridCenterTransform.position;
            float x = origin.x + TileCenterOffset(xIndex);
            Vector3 center = new Vector3(x, origin.y, origin.z);
            return (center, TileSize, GridHalfExtent * 2f);
        }

        // ── 패턴1: "#자" ─────────────────────────────────────

        private IEnumerator PlayHashPattern()
        {
            // 홀수(1,3,5,7,9 -> index 0,2,4,6,8) 또는 짝수(2,4,6,8 -> index 1,3,5,7)
            bool useOdd = Random.value < 0.5f;
            List<int> indices = new List<int>();
            int start = useOdd ? 0 : 1;
            for (int i = start; i < GridSize; i += 2)
                indices.Add(i);

            // 가로줄 동시 예고 -> 공격
            yield return StartCoroutine(PlayLinesSimultaneous(indices, isRow: true));

            // 세로줄 동시 예고 -> 공격
            yield return StartCoroutine(PlayLinesSimultaneous(indices, isRow: false));
        }

        /// <summary>여러 줄을 동시에 예고(lineTelegraphDuration) 후 동시에 공격.</summary>
        private IEnumerator PlayLinesSimultaneous(List<int> indices, bool isRow)
        {
            var used = new List<BossPatternFloorIndicator>();

            for (int n = 0; n < indices.Count; n++)
            {
                if (n >= floorIndicatorPool.Count)
                {
                    Debug.LogWarning("[BossFloorPatternController] 예고 풀이 부족합니다.");
                    break;
                }

                var rect = isRow ? GetRowRect(indices[n]) : GetColRect(indices[n]);
                var indicator = floorIndicatorPool[n];
                indicator.SetRect(rect.center, rect.sizeX, rect.sizeZ);
                indicator.ShowTelegraph();
                used.Add(indicator);
            }

            if (effectController != null)
                effectController.OnTelegraphSFX_FloorPattern();

            yield return new WaitForSeconds(lineTelegraphDuration);

            // 동시 공격 판정 + 표시
            foreach (var idx in indices)
            {
                var rect = isRow ? GetRowRect(idx) : GetColRect(idx);
                ApplyFloorDamage(rect.center, rect.sizeX, rect.sizeZ);
            }

            if (effectController != null)
                effectController.OnHitSFX_FloorPattern();

            foreach (var indicator in used)
                indicator.ShowAttack();

            yield return new WaitForSeconds(attackShowDuration);

            foreach (var indicator in used)
                indicator.Hide();
        }

        // ── 패턴2: "파도" ─────────────────────────────────────

        private IEnumerator PlayWavePattern()
        {
            // 0:상(세로줄, Z큰->작은) 1:하(세로줄, Z작은->큰) 2:좌(가로줄, X작은->큰) 3:우(가로줄, X큰->작은)
            int direction = Random.Range(0, 4);
            bool isRow = direction == 2 || direction == 3;

            List<int> order = new List<int>();
            for (int i = 0; i < GridSize; i++) order.Add(i);

            switch (direction)
            {
                case 0: // 상: Z 큰 -> 작은 (index 8 -> 0)
                    order.Reverse();
                    break;
                case 1: // 하: Z 작은 -> 큰 (index 0 -> 8)
                    break;
                case 2: // 좌: X 작은 -> 큰 (index 0 -> 8)
                    break;
                case 3: // 우: X 큰 -> 작은 (index 8 -> 0)
                    order.Reverse();
                    break;
            }

            // 파도타기: 각 줄을 waveStepInterval 간격으로 순차 예고 시작,
            // 각 줄은 lineTelegraphDuration 후 공격.
            Coroutine lastRoutine = null;
            for (int n = 0; n < order.Count; n++)
            {
                int lineIndex = order[n];
                if (n >= floorIndicatorPool.Count)
                {
                    Debug.LogWarning("[BossFloorPatternController] 예고 풀이 부족합니다.");
                    break;
                }
                var indicator = floorIndicatorPool[n];

                lastRoutine = StartCoroutine(PlaySingleLine(lineIndex, isRow, indicator));

                if (n < order.Count - 1)
                    yield return new WaitForSeconds(waveStepInterval);
            }

            // 마지막 줄의 코루틴이 끝날 때까지 대기
            if (lastRoutine != null)
                yield return lastRoutine;
        }

        /// <summary>단일 줄에 대해 예고 -> 공격 -> 표시 종료를 수행.</summary>
        private IEnumerator PlaySingleLine(int lineIndex, bool isRow, BossPatternFloorIndicator indicator)
        {
            var rect = isRow ? GetRowRect(lineIndex) : GetColRect(lineIndex);

            indicator.SetRect(rect.center, rect.sizeX, rect.sizeZ);
            indicator.ShowTelegraph();

            if (effectController != null)
                effectController.OnTelegraphSFX_FloorPattern();

            yield return new WaitForSeconds(lineTelegraphDuration);

            ApplyFloorDamage(rect.center, rect.sizeX, rect.sizeZ);

            if (effectController != null)
                effectController.OnHitSFX_FloorPattern();

            indicator.ShowAttack();

            yield return new WaitForSeconds(attackShowDuration);

            indicator.Hide();
        }

        // ── 패턴3: "컨테이너" ─────────────────────────────────

        private IEnumerator PlayContainerPattern()
        {
            if (container == null)
            {
                Debug.LogWarning("[BossFloorPatternController] container가 설정되지 않았습니다.");
                yield break;
            }

            // 중앙 3x3(인덱스 3,4,5) 제외, 81 - 9 = 72칸 중 랜덤 선택
            int xIndex, zIndex;
            do
            {
                xIndex = Random.Range(0, GridSize);
                zIndex = Random.Range(0, GridSize);
            } while (IsCenterTile(xIndex) && IsCenterTile(zIndex));

            Vector3 origin = GridCenterTransform.position;
            float targetX = origin.x + TileCenterOffset(xIndex);
            float targetZ = origin.z + TileCenterOffset(zIndex);
            Vector3 targetWorld = new Vector3(targetX, origin.y, targetZ);

            // 원형 예고 표시 (3초간 0.5m -> 2m)
            if (circleIndicator != null)
                circleIndicator.Show(targetWorld);

            if (effectController != null)
                effectController.OnTelegraphSFX_FloorPattern();

            // 컨테이너 낙하 시작 - 로컬 XZ를 목표 지점으로, Y는 idle(대기) 높이에서 시작
            Transform parent = container.transform.parent;
            Vector3 targetLocal = parent != null ? parent.InverseTransformPoint(targetWorld) : targetWorld;
            float startLocalY = container.IdleLocalY;
            container.transform.localPosition = new Vector3(targetLocal.x, startLocalY, targetLocal.z);

            // Ground 레이어를 향해 레이캐스트하여 바닥 월드 Y를 구하고, 로컬 Y로 변환
            float floorLocalY = startLocalY - 4f; // 레이캐스트 실패 시 대체값 (기존 동작 유지)
            int groundLayerMask = LayerMask.GetMask("Ground");
            Vector3 rayOrigin = new Vector3(targetWorld.x, container.transform.position.y, targetWorld.z);
            RaycastHit hitInfo;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hitInfo, 100f, groundLayerMask))
            {
                Vector3 floorWorld = hitInfo.point;
                Vector3 floorLocal = parent != null ? parent.InverseTransformPoint(floorWorld) : floorWorld;
                floorLocalY = floorLocal.y;
            }
            else
            {
                Debug.LogWarning("[BossFloorPatternController] Ground 레이어 바닥을 찾지 못해 기본 낙하 높이를 사용합니다.");
            }

            int damage = container.CalculateDamage(GetAttackPower());
            container.StartFalling(floorLocalY, damage);

            if (effectController != null)
            {
                effectController.PlayFloorPatternVfx(targetWorld, Quaternion.identity);
                effectController.OnHitSFX_FloorPattern();
            }

            // 낙하 시간(3초) 동안 대기 - 원형 예고와 동일 시간
            yield return new WaitForSeconds(3f);

            if (circleIndicator != null)
                circleIndicator.Hide();
        }

        /// <summary>인덱스 3,4,5(중앙 3칸)인지 여부.</summary>
        private bool IsCenterTile(int index) => index >= 3 && index <= 5;

        // ── 데미지 판정 (좌표 비교) ───────────────────────────

        /// <summary>월드 좌표 사각형(centerX/Z ± size/2) 범위 내 플레이어에게 데미지.</summary>
        private void ApplyFloorDamage(Vector3 center, float sizeX, float sizeZ)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObj == null) return;

            PlayerStatus playerStatus = playerObj.GetComponent<PlayerStatus>();
            if (playerStatus == null) playerStatus = playerObj.GetComponentInParent<PlayerStatus>();
            if (playerStatus == null) playerStatus = playerObj.GetComponentInChildren<PlayerStatus>();
            if (playerStatus == null) return;

            Vector3 pos = playerObj.transform.position;

            float minX = center.x - sizeX * 0.5f;
            float maxX = center.x + sizeX * 0.5f;
            float minZ = center.z - sizeZ * 0.5f;
            float maxZ = center.z + sizeZ * 0.5f;

            if (pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ)
            {
                int damage = Mathf.RoundToInt(GetAttackPower() * floorPatternMultiplier);
                playerStatus.TakeDamage(damage);
                Debug.Log($"[BossFloorPatternController] 바닥패턴 적중 | damage={damage}");

                if (effectController != null)
                    effectController.PlayFloorPatternVfx(pos, Quaternion.identity);
            }
        }

        // ── BossData 접근 ────────────────────────────────────

        private int GetAttackPower()
        {
            BossData data = bossStatus != null ? bossStatus.BossData : null;
            return data != null ? data.attackPower : 10;
        }

        // ── Gizmo ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = GridCenterTransform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);

            for (int i = 0; i <= GridSize; i++)
            {
                float offset = -9f + i * 2f;
                Gizmos.DrawLine(
                    new Vector3(origin.x - GridHalfExtent, origin.y, origin.z + offset),
                    new Vector3(origin.x + GridHalfExtent, origin.y, origin.z + offset));
                Gizmos.DrawLine(
                    new Vector3(origin.x + offset, origin.y, origin.z - GridHalfExtent),
                    new Vector3(origin.x + offset, origin.y, origin.z + GridHalfExtent));
            }

            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            float centerExtent = 3f;
            Gizmos.DrawWireCube(origin, new Vector3(centerExtent * 2f, 0.1f, centerExtent * 2f));
        }
    }
}
