using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 입장/사망 시네머신 컷씬을 재생하는 컴포넌트.
    ///
    /// [카메라가 보스 프리팹이 아니라 씬에 배치되는 이유]
    ///  introCamera/deathCamera는 보스 프리팹의 자식이 아니라 EasyStageScene에 직접 배치된 오브젝트다.
    ///  보스 프리팹 안에 두면 보스가 회전할 때 카메라도 같이 돌아가는 문제(바닥패턴 인디케이터를
    ///  BossSpawn 기준으로 고정한 것과 동일한 이유)와, 구도(위치/앵글)를 씬 컨텍스트 안에서 직접
    ///  잡아야 하는 문제가 있다. 보스는 런타임에 Instantiate되므로 카메라의 LookAt 타겟은
    ///  BossSector가 스폰 직후 BindCutsceneCameras()를 통해 코드에서 연결한다.
    ///
    /// [우선순위(Priority) 기반 전환 — CameraSet과 충돌하지 않는 이유]
    ///  기존 게임플레이 카메라 전환 시스템(CameraSet)은 activePriority=20을 사용하고,
    ///  PlayerCameraController가 매 프레임 그 카메라 3개(3인칭/조준/ADS)의 우선순위만 관리한다.
    ///  컷씬 카메라는 그보다 높은 cutsceneActivePriority(기본 50)를 사용해 항상 우선권을 가지며,
    ///  평소에는 cutsceneInactivePriority(기본 0)로 낮춰둬서 절대 선택되지 않는다.
    ///  CameraSet/PlayerCameraController는 이 두 카메라의 존재 자체를 모르기 때문에 서로 간섭하지 않는다.
    /// </summary>
    public class BossCinematicController : MonoBehaviour
    {
        [Header("입장 컷씬")]
        [Tooltip("입장 카메라가 보스를 응시하는 시간(초)")]
        [SerializeField] private float introHoldDuration = 2f;

        [Tooltip("입장 카메라 → 플레이어 카메라로 복귀할 때 블렌드 시간(초)")]
        [SerializeField] private float introBlendOutDuration = 0.5f;

        [Tooltip("게임플레이 카메라 → 입장 카메라로 들어갈 때 블렌드 시간(초). 0이면 즉시 전환(하드컷).")]
        [SerializeField] private float introBlendInDuration = 0f;

        [Header("사망 컷씬")]
        [Tooltip("사망 카메라가 보스를 응시하는 시간(초)")]
        [SerializeField] private float deathHoldDuration = 1f;

        [Tooltip("사망 카메라 → 플레이어 카메라로 복귀할 때 블렌드 시간(초)")]
        [SerializeField] private float deathBlendOutDuration = 0.5f;

        [Tooltip("게임플레이 카메라 → 사망 카메라로 들어갈 때 블렌드 시간(초). 0이면 즉시 전환(하드컷).")]
        [SerializeField] private float deathBlendInDuration = 0f;

        [Header("우선순위")]
        [Tooltip("컷씬 카메라가 화면을 가져갈 때의 Priority. CameraSet의 activePriority(기본 20)보다 충분히 높아야 한다.")]
        [SerializeField] private int cutsceneActivePriority = 50;

        [Tooltip("컷씬 카메라가 사용되지 않을 때의 Priority. CameraSet의 inactivePriority(기본 10)보다 낮아야 한다.")]
        [SerializeField] private int cutsceneInactivePriority = 0;

        [Tooltip("블렌드 스타일")]
        [SerializeField] private CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

        private CinemachineCamera _introCamera;
        private CinemachineCamera _deathCamera;
        private CinemachineBrain _brain;

        /// <summary>입장 컷씬의 총 소요 시간(응시 + 복귀 블렌드). BossController의 플레이어 정지 시간과 동기화하는 데 사용.</summary>
        public float TotalIntroDuration => introHoldDuration + introBlendOutDuration;

        /// <summary>
        /// BossSector가 보스 스폰 직후 호출 — 씬에 배치된 컷씬 카메라를 이 보스에 연결한다.
        /// </summary>
        public void BindCutsceneCameras(CinemachineCamera introCamera, CinemachineCamera deathCamera)
        {
            _introCamera = introCamera;
            _deathCamera = deathCamera;

            if (_introCamera != null)
            {
                _introCamera.LookAt = transform;
                SetPriority(_introCamera, cutsceneInactivePriority);
            }

            if (_deathCamera != null)
            {
                _deathCamera.LookAt = transform;
                SetPriority(_deathCamera, cutsceneInactivePriority);
            }

            if (_brain == null && Camera.main != null)
                Camera.main.TryGetComponent(out _brain);
        }

        /// <summary>
        /// 입장 컷씬 재생: introCamera로 introHoldDuration(초)간 보스를 응시한 뒤,
        /// introBlendOutDuration(초)에 걸쳐 플레이어 카메라로 복귀한다.
        /// BossController.PlayEntranceFreeze()가 이 코루틴이 끝날 때까지 대기한다(플레이어 정지 시간과 동기화).
        /// </summary>
        public IEnumerator PlayIntroCutscene()
        {
            if (_introCamera == null)
            {
                // 카메라가 연결되지 않았다면(테스트 중 등) 정지 시간만큼만 대기하고 끝낸다 — 폴백.
                yield return new WaitForSeconds(TotalIntroDuration);
                yield break;
            }

            SetBrainBlend(introBlendInDuration);
            SetPriority(_introCamera, cutsceneActivePriority);

            yield return new WaitForSeconds(introHoldDuration);

            SetBrainBlend(introBlendOutDuration);
            SetPriority(_introCamera, cutsceneInactivePriority);

            yield return new WaitForSeconds(introBlendOutDuration);
        }

        /// <summary>
        /// 사망 컷씬을 시작한다 (fire-and-forget — 사망 애니메이션/폭발 VFX와 병렬로 진행).
        /// BossStatus.Die()에서 호출.
        /// </summary>
        public void TriggerDeathCutscene()
        {
            StartCoroutine(PlayDeathCutscene());
        }

        private IEnumerator PlayDeathCutscene()
        {
            if (_deathCamera == null) yield break;

            SetBrainBlend(deathBlendInDuration);
            SetPriority(_deathCamera, cutsceneActivePriority);

            yield return new WaitForSeconds(deathHoldDuration);

            SetBrainBlend(deathBlendOutDuration);
            SetPriority(_deathCamera, cutsceneInactivePriority);
        }

        private void SetBrainBlend(float duration)
        {
            if (_brain == null) return;
            _brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, Mathf.Max(duration, 0f));
        }

        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null) return;
            camera.Priority.Value = priority;
        }

        private void OnDestroy()
        {
            // 씬에 남아있는 카메라가 파괴되는 보스를 계속 바라보지 않도록 정리한다.
            if (_introCamera != null)
            {
                _introCamera.LookAt = null;
                SetPriority(_introCamera, cutsceneInactivePriority);
            }

            if (_deathCamera != null)
            {
                _deathCamera.LookAt = null;
                SetPriority(_deathCamera, cutsceneInactivePriority);
            }
        }
    }
}
