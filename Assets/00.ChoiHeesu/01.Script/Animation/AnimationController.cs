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
        private int _animIDOnGround;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDX;
        private int _animIDY;
        // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
        // private int _animIDMotionSpeed;
        private int _animIDAttack;
        private int _animIDReloading;
        private int _animIDAiming;
        private int _animIDDead;
        private int _animIDInteractive;
        private int _animIDStopInteract;
        private int _animIDRoll;
        private int _animIDPickup;
        private int _animIDSnapTurn;
        private int _animIDTurnDirection;

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

        public void SetOnGround(bool isOnGround)
        {
            if (!CanUseAnimator(nameof(SetOnGround))) return;

            _animator.SetBool(_animIDOnGround, isOnGround);
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

        public void SetMoveDirection(float x, float y)
        {
            if (!CanUseAnimator(nameof(SetMoveDirection))) return;

            _animator.SetFloat(_animIDX, x);
            _animator.SetFloat(_animIDY, y);
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

        public void SetAiming(bool isAiming)
        {
            if (!CanUseAnimator(nameof(SetAiming))) return;

            _animator.SetBool(_animIDAiming, isAiming);
        }

        public void SetDead(bool isDead)
        {
            if (!CanUseAnimator(nameof(SetDead))) return;

            _animator.SetBool(_animIDDead, isDead);
        }

        public void SetInteractive()
        {
            if (!CanUseAnimator(nameof(SetInteractive))) return;

            _animator.SetTrigger(_animIDInteractive);
        }

        public void SetStopInteract()
        {
            if (!CanUseAnimator(nameof(SetStopInteract))) return;

            _animator.SetTrigger(_animIDStopInteract);
        }

        public void SetRoll()
        {
            if (!CanUseAnimator(nameof(SetRoll))) return;

            _animator.SetTrigger(_animIDRoll);
        }

        public void SetPickup()
        {
            if (!CanUseAnimator(nameof(SetPickup))) return;

            _animator.SetTrigger(_animIDPickup);
        }

        public void PlaySnapTurn(float turnDirection)
        {
            if (!CanUseAnimator(nameof(PlaySnapTurn))) return;

            _animator.SetFloat(_animIDTurnDirection, Mathf.Sign(turnDirection));
            _animator.SetTrigger(_animIDSnapTurn);
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
            _animIDOnGround = Animator.StringToHash("OnGround");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            // MotionSpeed는 현재 애니메이션 블렌딩에 사용하지 않음
            // _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDX = Animator.StringToHash("X");
            _animIDY = Animator.StringToHash("Y");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDReloading = Animator.StringToHash("Reloading");
            _animIDAiming = Animator.StringToHash("Aiming");
            _animIDDead = Animator.StringToHash("Dead");
            _animIDInteractive = Animator.StringToHash("Interact");
            _animIDStopInteract = Animator.StringToHash("StopInteract");
            _animIDRoll = Animator.StringToHash("Roll");
            _animIDPickup = Animator.StringToHash("Pickup");
            _animIDSnapTurn = Animator.StringToHash("SnapTurn");
            _animIDTurnDirection = Animator.StringToHash("TurnDirection");
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
