using StarterAssets;
using UnityEngine;

namespace _02.Script.Combat
{
    [DisallowMultipleComponent]
    public class PlayerSpreadProvider : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private WeaponController weaponController;

        [Header("Move Spread")]
        [SerializeField] private float speedForMaxMoveSpread;
        [SerializeField] private float maxMoveSpreadAngle = 2f;

        [Header("Air Spread")]
        [SerializeField] private float airSpreadAngle = 4f;

        [Header("State Spread")]
        [SerializeField] private float normalSpreadAngle = 0.5f;
        [SerializeField] private float normalFireSpreadAngle = 0.5f;
        [SerializeField] private float aimHoldSpreadAngle = 0.2f;
        [SerializeField] private float aimingSpreadAngle;

        [Header("Fire Time Spread")]
        [SerializeField] private AnimationCurve fireTimeSpreadCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float maxFireTimeSpread;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            speedForMaxMoveSpread = Mathf.Max(speedForMaxMoveSpread, 0f);
            maxMoveSpreadAngle = Mathf.Max(maxMoveSpreadAngle, 0f);
            airSpreadAngle = Mathf.Max(airSpreadAngle, 0f);
            normalSpreadAngle = Mathf.Max(normalSpreadAngle, 0f);
            normalFireSpreadAngle = Mathf.Max(normalFireSpreadAngle, 0f);
            aimHoldSpreadAngle = Mathf.Max(aimHoldSpreadAngle, 0f);
            aimingSpreadAngle = Mathf.Max(aimingSpreadAngle, 0f);
            maxFireTimeSpread = Mathf.Max(maxFireTimeSpread, 0f);

            if (fireTimeSpreadCurve == null || fireTimeSpreadCurve.length == 0)
                fireTimeSpreadCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        public float GetTotalSpreadAngle(float weaponBaseSpreadAngle)
        {
            float totalSpreadAngle = Mathf.Max(weaponBaseSpreadAngle, 0f) + GetAdditionalSpreadAngle();
            return Mathf.Max(totalSpreadAngle, 0f);
        }

        public float GetAdditionalSpreadAngle()
        {
            CacheReferences();
            return GetMoveSpreadAngle() + GetAirSpreadAngle() + GetStateSpreadAngle() + GetFireTimeSpreadAngle();
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                thirdPersonController = GetComponentInParent<ThirdPersonController>();

            if (characterController == null)
                characterController = GetComponentInParent<CharacterController>();

            if (weaponController == null)
                weaponController = GetComponentInParent<WeaponController>();
        }

        private float GetMoveSpreadAngle()
        {
            float maxSpeed = GetSpeedForMaxMoveSpread();
            if (maxSpeed <= 0f)
                return 0f;

            float speedRate = Mathf.Clamp01(GetHorizontalSpeed() / maxSpeed);
            return maxMoveSpreadAngle * speedRate;
        }

        private float GetAirSpreadAngle()
        {
            return IsGrounded() ? 0f : airSpreadAngle;
        }

        private float GetStateSpreadAngle()
        {
            if (thirdPersonController == null)
                return 0f;

            switch (thirdPersonController.CurrentActionState)
            {
                case PlayerActionState.Normal:
                    return normalSpreadAngle;
                case PlayerActionState.Normal_Fire:
                    return normalFireSpreadAngle;
                case PlayerActionState.AimHold:
                    return aimHoldSpreadAngle;
                case PlayerActionState.Aiming:
                    return aimingSpreadAngle;
                default:
                    return 0f;
            }
        }

        private float GetFireTimeSpreadAngle()
        {
            if (weaponController == null || fireTimeSpreadCurve == null || maxFireTimeSpread <= 0f)
                return 0f;

            float fireTime = Mathf.Max(weaponController.CurrentFireTime, 0f);
            if (fireTime <= 0f)
                return 0f;

            float curveTime = Mathf.Min(fireTime, GetFireTimeSpreadCurveMaxTime());
            float curveValue = Mathf.Clamp01(fireTimeSpreadCurve.Evaluate(curveTime));
            return maxFireTimeSpread * curveValue;
        }

        private float GetFireTimeSpreadCurveMaxTime()
        {
            if (fireTimeSpreadCurve == null || fireTimeSpreadCurve.length == 0)
                return 0f;

            return Mathf.Max(fireTimeSpreadCurve.keys[fireTimeSpreadCurve.length - 1].time, 0f);
        }

        private float GetHorizontalSpeed()
        {
            if (characterController == null)
                return 0f;

            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        private float GetSpeedForMaxMoveSpread()
        {
            if (speedForMaxMoveSpread > 0f)
                return speedForMaxMoveSpread;

            if (thirdPersonController != null)
                return Mathf.Max(thirdPersonController.MoveSpeed, thirdPersonController.SprintSpeed);

            return 0f;
        }

        private bool IsGrounded()
        {
            if (thirdPersonController != null)
                return thirdPersonController.Grounded;

            if (characterController != null)
                return characterController.isGrounded;

            return true;
        }
    }
}
