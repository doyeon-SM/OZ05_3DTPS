using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    public class BossFloorPatternController : MonoBehaviour
    {
        private const string PlayerTag    = "Player";
        private const string BossSpawnName = "BossSpawn";
        private const int   GridSize      = 9;
        private const float TileSize      = 2f;
        private const float GridHalfExtent = 9f;

        // ── Animator Trigger 상수 ────────────────────────────
        private const string TriggerFloorHash      = "SA_attack";
        private const string TriggerFloorWave      = "SB_attack";
        private const string TriggerFloorContainer = "SC_attack";

        [Header("보스 데이터")]
        [SerializeField] private BossStatus bossStatus;

        [Tooltip("공격 VFX/SFX 처리 컴포넌트 (비워두면 자동으로 GetComponentInParent 시도)")]
        [SerializeField] private BossEffectController effectController;

        [Header("애니메이션")]
        [Tooltip("보스 Animator. 비워두면 GetComponentInParent로 자동 탐색.")]
        [SerializeField] private Animator animator;

        [Tooltip("격자 중심으로 사용할 Transform (보통 보스 본체). 비워두면 this.transform 사용.")]
        [SerializeField] private Transform gridCenter;

        [Header("격자 예고 풀 (최대 9개)")]
        [SerializeField] private List<BossPatternFloorIndicator> floorIndicatorPool;

        [Header("컨테이너 패턴")]
        [SerializeField] private BossPatternContainer container;
        [SerializeField] private BossPatternCircleIndicator circleIndicator;

        [Header("패턴1/2 설정")]
        [SerializeField] private float lineTelegraphDuration = 2f;
        [SerializeField] private float waveStepInterval = 0.2f;
        [SerializeField] private float attackShowDuration = 0.3f;

        [Header("데미지 배율")]
        [SerializeField] private float floorPatternMultiplier = 1f;

        private void Awake()
        {
            if (effectController == null) effectController = GetComponentInParent<BossEffectController>();
            if (animator == null)         animator         = GetComponentInParent<Animator>();

            if (gridCenter == null)
            {
                GameObject spawn = GameObject.Find(BossSpawnName);
                if (spawn != null)
                    gridCenter = spawn.transform;
                else
                    Debug.LogWarning($"[BossFloorPatternController] '{BossSpawnName}' 오브젝트를 찾지 못해 gridCenter를 자기 자신으로 설정합니다.");
            }

            ReparentToGridCenter();
        }

        private void ReparentToGridCenter()
        {
            if (gridCenter == null) return;

            foreach (var indicator in floorIndicatorPool)
            {
                if (indicator != null) ReparentKeepWorld(indicator.transform, gridCenter);
            }
            if (circleIndicator != null) ReparentKeepWorld(circleIndicator.transform, gridCenter);
            if (container != null)       ReparentKeepWorld(container.transform, gridCenter);
        }

        private void ReparentKeepWorld(Transform t, Transform newParent)
        {
            if (t.parent == newParent) return;
            Vector3    worldPos   = t.position;
            Quaternion worldRot   = t.rotation;
            t.SetParent(newParent, true);
            t.position = worldPos;
            t.rotation = worldRot;
        }

        private Transform GridCenterTransform => gridCenter != null ? gridCenter : transform;

        // ── 애니메이션 헬퍼 ─────────────────────────────────

        private void SetAttackTrigger(string triggerName)
        {
            if (animator != null)
                animator.SetTrigger(triggerName);
            else
                Debug.LogWarning($"[BossFloorPatternController] Animator가 없어 Trigger '{triggerName}'을 발동할 수 없습니다.");
        }

        // ── 외부 진입점 ─────────────────────────────────────

        public IEnumerator PlaySpecialPattern()
        {
            bool containerAvailable = container == null || !container.IsActive;

            int choice = containerAvailable
                ? Random.Range(0, 3)
                : Random.Range(0, 2);

            switch (choice)
            {
                case 0: yield return StartCoroutine(PlayHashPattern());      break;
                case 1: yield return StartCoroutine(PlayWavePattern());      break;
                case 2: yield return StartCoroutine(PlayContainerPattern()); break;
            }
        }

        // ── 격자 좌표 헬퍼 ───────────────────────────────────

        private float TileCenterOffset(int index) => -8f + index * 2f;

        private (Vector3 center, float sizeX, float sizeZ) GetRowRect(int zIndex)
        {
            Vector3 origin = GridCenterTransform.position;
            return (new Vector3(origin.x, origin.y, origin.z + TileCenterOffset(zIndex)),
                    GridHalfExtent * 2f, TileSize);
        }

        private (Vector3 center, float sizeX, float sizeZ) GetColRect(int xIndex)
        {
            Vector3 origin = GridCenterTransform.position;
            return (new Vector3(origin.x + TileCenterOffset(xIndex), origin.y, origin.z),
                    TileSize, GridHalfExtent * 2f);
        }

        // ── 패턴1: "#자" ─────────────────────────────────────

        private IEnumerator PlayHashPattern()
        {
            // 애니메이션: SA_attack
            SetAttackTrigger(TriggerFloorHash);

            bool useOdd = Random.value < 0.5f;
            var indices = new List<int>();
            int start = useOdd ? 0 : 1;
            for (int i = start; i < GridSize; i += 2)
                indices.Add(i);

            yield return StartCoroutine(PlayLinesSimultaneous(indices, isRow: true));
            yield return StartCoroutine(PlayLinesSimultaneous(indices, isRow: false));
        }

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

            foreach (var idx in indices)
            {
                var rect = isRow ? GetRowRect(idx) : GetColRect(idx);
                ApplyFloorDamage(rect.center, rect.sizeX, rect.sizeZ);
            }

            if (effectController != null)
                effectController.OnHitSFX_FloorPattern();

            foreach (var indicator in used) indicator.ShowAttack();

            yield return new WaitForSeconds(attackShowDuration);

            foreach (var indicator in used) indicator.Hide();
        }

        // ── 패턴2: "파도" ─────────────────────────────────────

        private IEnumerator PlayWavePattern()
        {
            // 애니메이션: SB_attack
            SetAttackTrigger(TriggerFloorWave);

            int direction = Random.Range(0, 4);
            bool isRow = direction == 2 || direction == 3;

            var order = new List<int>();
            for (int i = 0; i < GridSize; i++) order.Add(i);

            if (direction == 0 || direction == 3) order.Reverse();

            Coroutine lastRoutine = null;
            for (int n = 0; n < order.Count; n++)
            {
                if (n >= floorIndicatorPool.Count)
                {
                    Debug.LogWarning("[BossFloorPatternController] 예고 풀이 부족합니다.");
                    break;
                }
                lastRoutine = StartCoroutine(PlaySingleLine(order[n], isRow, floorIndicatorPool[n]));
                if (n < order.Count - 1)
                    yield return new WaitForSeconds(waveStepInterval);
            }

            if (lastRoutine != null) yield return lastRoutine;
        }

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
            // 애니메이션: SC_attack
            SetAttackTrigger(TriggerFloorContainer);

            if (container == null)
            {
                Debug.LogWarning("[BossFloorPatternController] container가 설정되지 않았습니다.");
                yield break;
            }

            int xIndex, zIndex;
            do
            {
                xIndex = Random.Range(0, GridSize);
                zIndex = Random.Range(0, GridSize);
            } while (IsCenterTile(xIndex) && IsCenterTile(zIndex));

            Vector3 origin = GridCenterTransform.position;
            Vector3 targetWorld = new Vector3(
                origin.x + TileCenterOffset(xIndex),
                origin.y,
                origin.z + TileCenterOffset(zIndex));

            if (circleIndicator != null) circleIndicator.Show(targetWorld);
            if (effectController != null) effectController.OnTelegraphSFX_FloorPattern();

            Transform parent = container.transform.parent;
            Vector3 targetLocal = parent != null ? parent.InverseTransformPoint(targetWorld) : targetWorld;
            float startLocalY = container.IdleLocalY;
            container.transform.localPosition = new Vector3(targetLocal.x, startLocalY, targetLocal.z);

            float floorLocalY = startLocalY - 4f;
            int groundLayerMask = LayerMask.GetMask("Ground");
            Vector3 rayOrigin = new Vector3(targetWorld.x, container.transform.position.y, targetWorld.z);
            RaycastHit hitInfo;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hitInfo, 100f, groundLayerMask))
            {
                Vector3 floorLocal = parent != null ? parent.InverseTransformPoint(hitInfo.point) : hitInfo.point;
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

            yield return new WaitForSeconds(3f);

            if (circleIndicator != null) circleIndicator.Hide();
        }

        private bool IsCenterTile(int index) => index >= 3 && index <= 5;

        // ── 데미지 판정 ─────────────────────────────────────

        private void ApplyFloorDamage(Vector3 center, float sizeX, float sizeZ)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObj == null) return;

            PlayerStatus playerStatus = playerObj.GetComponent<PlayerStatus>()
                ?? playerObj.GetComponentInParent<PlayerStatus>()
                ?? playerObj.GetComponentInChildren<PlayerStatus>();
            if (playerStatus == null) return;

            Vector3 pos = playerObj.transform.position;
            if (pos.x >= center.x - sizeX * 0.5f && pos.x <= center.x + sizeX * 0.5f &&
                pos.z >= center.z - sizeZ * 0.5f && pos.z <= center.z + sizeZ * 0.5f)
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
                Gizmos.DrawLine(new Vector3(origin.x - GridHalfExtent, origin.y, origin.z + offset),
                                new Vector3(origin.x + GridHalfExtent, origin.y, origin.z + offset));
                Gizmos.DrawLine(new Vector3(origin.x + offset, origin.y, origin.z - GridHalfExtent),
                                new Vector3(origin.x + offset, origin.y, origin.z + GridHalfExtent));
            }
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawWireCube(origin, new Vector3(6f, 0.1f, 6f));
        }
    }
}
