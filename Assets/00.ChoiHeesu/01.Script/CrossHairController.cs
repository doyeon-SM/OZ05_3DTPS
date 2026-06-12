using _00.ChoiHeesu._03.WeaponChangeSystem;
using _02.Script.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class CrossHairController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Transform crossHairRoot;

        [Header("Gap")]
        [SerializeField] private float baseGap;
        [SerializeField] private float spreadGapMultiplier = 8f;
        [SerializeField] private float maxGap = 120f;
        [SerializeField] private float smoothSpeed = 12f;

        [Header("Runtime")]
        [SerializeField] private List<CrossHairData> crossHairs = new List<CrossHairData>();
        [SerializeField] private CrossHairData currentCrossHair;

        private readonly Dictionary<GameObject, CrossHairData> crossHairByPrefab = new Dictionary<GameObject, CrossHairData>();
        private WeaponController subscribedWeaponController;
        private WeaponData currentWeaponData;
        private float currentGap;
        private float targetGap;
        private bool hasBuiltCrossHairs;
        private bool missingWeaponRuntimeManagerLogged;
        private bool missingWeaponControllerLogged;
        private bool missingCrossHairRootLogged;

        private void Awake()
        {
            currentGap = Mathf.Max(baseGap, 0f);
            targetGap = currentGap;

            CacheReferences();
            BuildCrossHairs();
        }

        private void OnEnable()
        {
            SubscribeToWeaponController();
            RefreshCurrentWeapon();
        }

        private void Start()
        {
            CacheReferences();
            BuildCrossHairs();
            SubscribeToWeaponController();
            RefreshCurrentWeapon();
        }

        private void OnDisable()
        {
            UnsubscribeFromWeaponController();
        }

        private void Update()
        {
            SyncWeaponControllerFromRuntimeManager();
            UpdateCurrentWeaponIfChanged();
            UpdateGap();
        }

        private void CacheReferences()
        {
            if (crossHairRoot == null)
                crossHairRoot = transform;

            if (weaponRuntimeManager == null)
                weaponRuntimeManager = WeaponRuntimeManager.Instance;

            if (weaponRuntimeManager == null)
                weaponRuntimeManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);

            if (weaponController == null && weaponRuntimeManager != null)
                weaponController = weaponRuntimeManager.WeaponController;

            if (weaponController == null)
                weaponController = FindFirstObjectByType<WeaponController>(FindObjectsInactive.Include);
        }

        private void BuildCrossHairs()
        {
            if (hasBuiltCrossHairs)
                return;

            if (!HasRequiredReferencesForBuild())
                return;

            WeaponData[] weaponDatas = weaponRuntimeManager.WeaponDataArray;
            if (weaponDatas == null || weaponDatas.Length == 0)
            {
                Debug.LogWarning("[CrossHairController] WeaponRuntimeManager의 WeaponDataArray가 비어 있어 CrossHair를 생성할 수 없습니다.", this);
                return;
            }

            for (int i = 0; i < weaponDatas.Length; i++)
            {
                WeaponData weaponData = weaponDatas[i];
                if (weaponData == null)
                    continue;

                RegisterCrossHairPrefab(weaponData);
            }

            hasBuiltCrossHairs = true;
        }

        private bool HasRequiredReferencesForBuild()
        {
            bool hasReferences = true;

            if (weaponRuntimeManager == null)
            {
                LogOnce(ref missingWeaponRuntimeManagerLogged,
                    "[CrossHairController] weaponRuntimeManager가 null입니다. WeaponRuntimeManager를 Inspector에 연결하거나 씬에 배치해주세요.");
                hasReferences = false;
            }

            if (crossHairRoot == null)
            {
                LogOnce(ref missingCrossHairRootLogged,
                    "[CrossHairController] crossHairRoot가 null입니다. CombatUI - CrossHair 오브젝트 Transform을 연결해주세요.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private void RegisterCrossHairPrefab(WeaponData weaponData)
        {
            GameObject crossHairPrefab = weaponData.CrossHairstylePrefab;
            if (crossHairPrefab == null)
            {
                Debug.LogWarning($"[CrossHairController] {weaponData.name}의 CrossHairstyle Prefab이 비어 있어 건너뜁니다.", weaponData);
                return;
            }

            if (crossHairByPrefab.ContainsKey(crossHairPrefab))
                return;

            GameObject crossHairObject = Instantiate(crossHairPrefab, crossHairRoot, false);
            crossHairObject.name = crossHairPrefab.name;

            CrossHairData crossHairData = crossHairObject.GetComponent<CrossHairData>();
            if (crossHairData == null)
                crossHairData = crossHairObject.GetComponentInChildren<CrossHairData>(true);

            if (crossHairData == null)
            {
                Debug.LogError($"[CrossHairController] {crossHairPrefab.name} 프리팹에 CrossHairData 컴포넌트가 없습니다.", crossHairPrefab);
                Destroy(crossHairObject);
                return;
            }

            crossHairData.Initialize(crossHairPrefab);
            crossHairData.SetVisible(false);

            crossHairByPrefab.Add(crossHairPrefab, crossHairData);
            crossHairs.Add(crossHairData);
        }

        private void SubscribeToWeaponController()
        {
            if (subscribedWeaponController == weaponController)
                return;

            UnsubscribeFromWeaponController();

            if (weaponController == null)
                return;

            weaponController.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            subscribedWeaponController = weaponController;
        }

        private void UnsubscribeFromWeaponController()
        {
            if (subscribedWeaponController == null)
                return;

            subscribedWeaponController.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            subscribedWeaponController = null;
        }

        private void HandleCurrentWeaponChanged(WeaponRuntime weaponRuntime)
        {
            SetCurrentWeaponData(weaponRuntime != null ? weaponRuntime.data : null);
        }

        private void SyncWeaponControllerFromRuntimeManager()
        {
            if (weaponRuntimeManager == null || weaponRuntimeManager.WeaponController == null)
                return;

            if (weaponController == weaponRuntimeManager.WeaponController)
                return;

            weaponController = weaponRuntimeManager.WeaponController;
            SubscribeToWeaponController();
            RefreshCurrentWeapon();
        }

        private void RefreshCurrentWeapon()
        {
            if (weaponController == null)
            {
                LogOnce(ref missingWeaponControllerLogged,
                    "[CrossHairController] weaponController가 null입니다. 현재 무기와 Spread 값을 읽으려면 WeaponController가 필요합니다.");
                SetCurrentWeaponData(null);
                return;
            }

            SetCurrentWeaponData(weaponController.CurrentWeaponData);
        }

        private void UpdateCurrentWeaponIfChanged()
        {
            if (weaponController == null)
                return;

            WeaponData nextWeaponData = weaponController.CurrentWeaponData;
            if (nextWeaponData == currentWeaponData)
                return;

            SetCurrentWeaponData(nextWeaponData);
        }

        private void SetCurrentWeaponData(WeaponData weaponData)
        {
            currentWeaponData = weaponData;

            GameObject crossHairPrefab = weaponData != null ? weaponData.CrossHairstylePrefab : null;
            CrossHairData nextCrossHair = null;

            if (crossHairPrefab != null && !crossHairByPrefab.TryGetValue(crossHairPrefab, out nextCrossHair))
            {
                Debug.LogWarning($"[CrossHairController] {weaponData.name}의 CrossHairstyle Prefab으로 생성된 CrossHairData를 찾지 못했습니다.", this);
            }

            SetCurrentCrossHair(nextCrossHair);
        }

        private void SetCurrentCrossHair(CrossHairData nextCrossHair)
        {
            if (currentCrossHair == nextCrossHair)
                return;

            if (currentCrossHair != null)
                currentCrossHair.SetVisible(false);

            currentCrossHair = nextCrossHair;

            if (currentCrossHair == null)
                return;

            currentCrossHair.SetVisible(true);
            currentCrossHair.SetGap(currentGap);
        }

        private void UpdateGap()
        {
            if (currentCrossHair == null)
                return;

            targetGap = CalculateTargetGap(GetCurrentSpreadAngle());

            if (smoothSpeed <= 0f)
            {
                currentGap = targetGap;
            }
            else
            {
                float lerpRate = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                currentGap = Mathf.Lerp(currentGap, targetGap, lerpRate);
            }

            currentCrossHair.SetGap(currentGap);
        }

        private float GetCurrentSpreadAngle()
        {
            if (weaponController != null)
                return weaponController.CurrentSpreadAngle;

            return currentWeaponData != null ? Mathf.Max(currentWeaponData.basicSpreadAngle, 0f) : 0f;
        }

        private float CalculateTargetGap(float spreadAngle)
        {
            float gap = Mathf.Max(baseGap, 0f) + Mathf.Max(spreadAngle, 0f) * Mathf.Max(spreadGapMultiplier, 0f);
            return maxGap > 0f ? Mathf.Min(gap, maxGap) : gap;
        }

        private void LogOnce(ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError(message, this);
            alreadyLogged = true;
        }

        private void OnValidate()
        {
            baseGap = Mathf.Max(baseGap, 0f);
            spreadGapMultiplier = Mathf.Max(spreadGapMultiplier, 0f);
            maxGap = Mathf.Max(maxGap, 0f);
            smoothSpeed = Mathf.Max(smoothSpeed, 0f);
        }
    }
}
