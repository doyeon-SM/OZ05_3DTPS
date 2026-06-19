using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 사망(Die) 애니메이션 클립의 Animation Event 수신 전용 중계 컴포넌트.
    ///
    /// [왜 필요한가]
    ///  Unity의 Animation Event는 Animator 컴포넌트가 붙어있는 바로 그 GameObject에서만
    ///  메서드를 찾는다. 보스 프리팹 구조상 Animator는 루트(BossTest)가 아니라
    ///  자식인 Mesh_0(스킨드 메시 오브젝트)에 있고, BossStatus/BossEffectController는
    ///  루트에 있어서 Animation Event가 "no receiver"로 실패한다.
    ///
    ///  이 컴포넌트를 Animator와 같은 GameObject(Mesh_0)에 붙이면, Animation Event는
    ///  정상적으로 이 컴포넌트의 메서드를 호출하고, 이 메서드는 GetComponentInParent로
    ///  루트의 실제 로직을 호출(중계)한다.
    ///
    /// [설치 방법]
    ///  보스 프리팹의 Animator가 붙어있는 GameObject(Mesh_0)에 이 컴포넌트를 추가하세요.
    ///  (BossStatus/BossEffectController가 있는 루트가 아닙니다.)
    /// </summary>
    public class BossAnimationEventRelay : MonoBehaviour
    {
        private BossStatus _bossStatus;
        private BossEffectController _effectController;

        private void Awake()
        {
            _bossStatus = GetComponentInParent<BossStatus>();
            _effectController = GetComponentInParent<BossEffectController>();

            if (_bossStatus == null)
                Debug.LogWarning("[BossAnimationEventRelay] 부모 계층에서 BossStatus를 찾지 못했습니다.");
            if (_effectController == null)
                Debug.LogWarning("[BossAnimationEventRelay] 부모 계층에서 BossEffectController를 찾지 못했습니다.");
        }

        /// <summary>[Animation Event 전용] Boss_Die 클립에서 호출 — 폭발 VFX 생성을 BossEffectController로 중계.</summary>
        public void OnDeathExplosionVfx()
        {
            _effectController?.OnDeathExplosionVfx();
        }

        /// <summary>[Animation Event 전용] Boss_Die 클립 마지막 프레임에서 호출 — 사망 완료 처리를 BossStatus로 중계.</summary>
        public void AnimEvent_OnDeathComplete()
        {
            _bossStatus?.AnimEvent_OnDeathComplete();
        }
    }
}
