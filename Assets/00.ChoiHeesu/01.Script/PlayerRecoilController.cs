using StarterAssets;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class PlayerRecoilController : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ThirdPersonController thirdPersonController;

        [Header("Recoil Setting")]
        [SerializeField] private float recoilUpSmoothSpeed = 60f;
        [SerializeField] private float maxPendingRecoil = 30f;

        private float pendingRecoil;
        private bool missingThirdPersonControllerLogged;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            ApplyPendingRecoil();
        }

        public void ApplyRecoil(WeaponData weaponData)
        {
            if (weaponData == null)
                return;

            AddRecoil(weaponData.Recoil);
        }

        public void AddRecoil(float recoil)
        {
            if (recoil <= 0f)
                return;

            if (thirdPersonController == null)
                CacheReferences();

            if (thirdPersonController == null)
            {
                LogMissingThirdPersonController();
                return;
            }

            float safeMaxPendingRecoil = Mathf.Max(maxPendingRecoil, 0f);
            pendingRecoil += recoil;

            if (safeMaxPendingRecoil > 0f)
                pendingRecoil = Mathf.Min(pendingRecoil, safeMaxPendingRecoil);
        }

        private void ApplyPendingRecoil()
        {
            if (pendingRecoil <= 0f)
                return;

            if (thirdPersonController == null)
            {
                CacheReferences();

                if (thirdPersonController == null)
                {
                    LogMissingThirdPersonController();
                    pendingRecoil = 0f;
                    return;
                }
            }

            float recoilStep = GetRecoilStep();
            pendingRecoil = Mathf.Max(pendingRecoil - recoilStep, 0f);

            // Negative pitch moves the camera upward in ThirdPersonController.
            thirdPersonController.AddCameraPitch(-recoilStep);
        }

        private float GetRecoilStep()
        {
            if (recoilUpSmoothSpeed <= 0f)
                return pendingRecoil;

            return Mathf.Min(pendingRecoil, recoilUpSmoothSpeed * Time.deltaTime);
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                thirdPersonController = GetComponentInParent<ThirdPersonController>();

            if (thirdPersonController == null)
                thirdPersonController = GetComponentInChildren<ThirdPersonController>(true);
        }

        private void LogMissingThirdPersonController()
        {
            if (missingThirdPersonControllerLogged)
                return;

            missingThirdPersonControllerLogged = true;
            Debug.LogError("[PlayerRecoilController] ThirdPersonController is missing. Check the player hierarchy or assign it in the Inspector.", this);
        }

        private void OnValidate()
        {
            recoilUpSmoothSpeed = Mathf.Max(recoilUpSmoothSpeed, 0f);
            maxPendingRecoil = Mathf.Max(maxPendingRecoil, 0f);
        }
    }
}
