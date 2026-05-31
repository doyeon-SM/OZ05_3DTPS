using UnityEngine;

namespace StarterAssets
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioFootsteps;
        [SerializeField] private AudioSource landingAudio;
        [SerializeField] private AudioSource audioFoley;

        private Animator _animator;
        private bool _hasAnimator;
        private bool _missingAnimatorLogged;
        private bool _animationIDsAssigned;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
        // private int _animIDMotionSpeed;
        private int _animIDAttack;
        private int _animIDReloading;
        private int _animIDInteractive;
        private int _animIDPickup;

        private void Awake()
        {
            TryCacheAnimator();
            AssignAnimationIDs();

            if (!_hasAnimator)
                ReportMissingAnimator(nameof(Awake));
        }

        public void SetGrounded(bool isGrounded)
        {
            if (!CanUseAnimator(nameof(SetGrounded))) return;

            _animator.SetBool(_animIDGrounded, isGrounded);
        }

        public void SetJump(bool isJumping)
        {
            if (!CanUseAnimator(nameof(SetJump))) return;

            _animator.SetBool(_animIDJump, isJumping);
        }

        public void SetFreeFall(bool isFreeFall)
        {
            if (!CanUseAnimator(nameof(SetFreeFall))) return;

            _animator.SetBool(_animIDFreeFall, isFreeFall);
        }

        public void SetMove(float speed)
        {
            if (!CanUseAnimator(nameof(SetMove))) return;

            _animator.SetFloat(_animIDSpeed, speed);
            // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
            // _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }

        public void SetAttack(bool isAttacking)
        {
            if (!CanUseAnimator(nameof(SetAttack))) return;

            _animator.SetBool(_animIDAttack, isAttacking);
        }

        public void SetReloading(bool isReloading)
        {
            if (!CanUseAnimator(nameof(SetReloading))) return;

            _animator.SetBool(_animIDReloading, isReloading);
        }

        public void SetInteractive()
        {
            if (!CanUseAnimator(nameof(SetInteractive))) return;

            _animator.SetTrigger(_animIDInteractive);
        }

        public void SetPickup()
        {
            if (!CanUseAnimator(nameof(SetPickup))) return;

            _animator.SetTrigger(_animIDPickup);
        }

        private bool CanUseAnimator(string callerName)
        {
            AssignAnimationIDs();

            if (TryCacheAnimator())
                return true;

            ReportMissingAnimator(callerName);
            return false;
        }

        private bool TryCacheAnimator()
        {
            if (_animator != null)
            {
                _hasAnimator = true;
                return true;
            }

            _hasAnimator = TryGetComponent(out _animator);
            return _hasAnimator;
        }

        private void ReportMissingAnimator(string callerName)
        {
            if (_missingAnimatorLogged)
                return;

            Debug.LogError($"[AnimationController] {callerName}에서 Animator를 찾을 수 없습니다. {gameObject.name} 오브젝트에 Animator 컴포넌트를 추가하거나 _animator 필드에 연결해주세요.", this);
            _missingAnimatorLogged = true;
        }

        private void AssignAnimationIDs()
        {
            if (_animationIDsAssigned)
                return;

            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
            // _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDReloading = Animator.StringToHash("Reloading");
            _animIDInteractive = Animator.StringToHash("Interact");
            _animIDPickup = Animator.StringToHash("Pickup");
            _animationIDsAssigned = true;
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

            if (audioFootsteps != null)
            {
                audioFootsteps.Play();
            }

            if (audioFoley != null)
            {
                audioFoley.Play();
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

            if (landingAudio != null)
            {
                landingAudio.Play();
            }
        }
    }
}
