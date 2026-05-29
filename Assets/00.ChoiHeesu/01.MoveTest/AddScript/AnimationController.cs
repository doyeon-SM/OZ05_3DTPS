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

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
        // private int _animIDMotionSpeed;
        private int _animIDAttack;
        private int _animIDReloading;
        private int _animIDInteractive;

        private void Awake()
        {
            _hasAnimator = TryGetComponent(out _animator);
            AssignAnimationIDs();
        }

        public void SetGrounded(bool isGrounded)
        {
            if (!_hasAnimator) return;

            _animator.SetBool(_animIDGrounded, isGrounded);
        }

        public void SetJump(bool isJumping)
        {
            if (!_hasAnimator) return;

            _animator.SetBool(_animIDJump, isJumping);
        }

        public void SetFreeFall(bool isFreeFall)
        {
            if (!_hasAnimator) return;

            _animator.SetBool(_animIDFreeFall, isFreeFall);
        }

        public void SetMove(float speed)
        {
            if (!_hasAnimator) return;

            _animator.SetFloat(_animIDSpeed, speed);
            // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
            // _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }

        public void SetAttack(bool isAttacking)
        {
            if (!_hasAnimator) return;

            _animator.SetBool(_animIDAttack, isAttacking);
        }

        public void SetReloading(bool isReloading)
        {
            if (!_hasAnimator) return;

            _animator.SetBool(_animIDReloading, isReloading);
        }

        public void SetInteractive()
        {
            if (!_hasAnimator) return;

            _animator.SetTrigger(_animIDInteractive);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
            // _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDReloading = Animator.StringToHash("Reloading");
            _animIDInteractive = Animator.StringToHash("Interactive");
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
