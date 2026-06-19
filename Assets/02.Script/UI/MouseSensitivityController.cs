using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// P_Game 패널 - 마우스 X/Y 감도를 슬라이더로 조절한다.
///
/// [슬라이더 값]
///  - Slider 원본값은 -1~1로 저장한다.
///  - -1 = -50% = 0.5배, 0 = 0% = 1배, 1 = +100% = 2배로 적용한다.
///
/// [저장]
///  - SaveManager(JSON 단일 파일)를 통해 저장/로드한다.
///  - 슬라이더 조작 시 SaveManager의 메모리 상 값만 갱신되고, 실제 파일 저장은
///    게임 종료 시 SaveManager.OnApplicationQuit()에서 한 번에 처리된다.
/// </summary>
public class MouseSensitivityController : MonoBehaviour
{
    [System.Serializable]
    private class SensitivityAxis
    {
        [Tooltip("SaveManager에 저장할 키")]
        public string prefsKey;

        [Tooltip("설정창에 표시할 축 이름")]
        public string displayName;

        public Slider slider;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI valueText;
    }

    [Header("마우스 감도 축 (Slider / 텍스트는 Inspector에서 연결)")]
    [SerializeField]
    private SensitivityAxis xAxis = new SensitivityAxis
    {
        prefsKey = SaveManager.MouseSensitivityXKey,
        displayName = "X 감도"
    };

    [SerializeField]
    private SensitivityAxis yAxis = new SensitivityAxis
    {
        prefsKey = SaveManager.MouseSensitivityYKey,
        displayName = "Y 감도"
    };

    [Header("실시간 적용 대상")]
    [SerializeField] private ThirdPersonController thirdPersonController;

    private const float SliderMinValue = -1f;
    private const float SliderMaxValue = 1f;
    private const string XSliderObjectName = "S_X_Axis";
    private const string YSliderObjectName = "S_Y_Axis";
    private const string NameTextObjectName = "SliderName";
    private const string ValueTextObjectName = "CurrentValue";

    private bool missingThirdPersonControllerLogged;

    private void Awake()
    {
        SetupAxis(xAxis, XSliderObjectName, SaveManager.MouseSensitivityXKey, "X 감도");
        SetupAxis(yAxis, YSliderObjectName, SaveManager.MouseSensitivityYKey, "Y 감도");

        LoadAxisValue(xAxis);
        LoadAxisValue(yAxis);

        AddSliderListener(xAxis);
        AddSliderListener(yAxis);

        ApplyCurrentSensitivityToPlayer();
    }

    private void OnEnable()
    {
        ApplyCurrentSensitivityToPlayer();
    }

    private void SetupAxis(SensitivityAxis axis, string sliderObjectName, string defaultKey, string defaultDisplayName)
    {
        if (axis == null)
            return;

        if (string.IsNullOrEmpty(axis.prefsKey))
            axis.prefsKey = defaultKey;

        if (string.IsNullOrEmpty(axis.displayName))
            axis.displayName = defaultDisplayName;

        if (axis.slider == null)
            axis.slider = FindSlider(sliderObjectName);

        if (axis.slider == null)
        {
            Debug.LogWarning($"[MouseSensitivityController] '{sliderObjectName}' Slider를 찾을 수 없습니다.", this);
            return;
        }

        axis.slider.minValue = SliderMinValue;
        axis.slider.maxValue = SliderMaxValue;
        axis.slider.wholeNumbers = false;

        if (axis.nameText == null)
            axis.nameText = FindTextUnder(axis.slider.transform, NameTextObjectName);

        if (axis.valueText == null)
            axis.valueText = FindTextUnder(axis.slider.transform, ValueTextObjectName);

        if (axis.nameText != null)
            axis.nameText.text = axis.displayName;
    }

    private void LoadAxisValue(SensitivityAxis axis)
    {
        if (axis == null || axis.slider == null)
            return;

        float savedValue = LoadSliderValue(axis.prefsKey);
        axis.slider.SetValueWithoutNotify(savedValue);
        UpdateValueText(axis, savedValue);
    }

    private void AddSliderListener(SensitivityAxis axis)
    {
        if (axis == null || axis.slider == null)
            return;

        axis.slider.onValueChanged.AddListener(value => OnSliderChanged(axis, value));
    }

    private void OnSliderChanged(SensitivityAxis axis, float sliderValue)
    {
        UpdateValueText(axis, sliderValue);
        SaveSliderValue(axis.prefsKey, sliderValue);
        ApplyCurrentSensitivityToPlayer();
    }

    private void ApplyCurrentSensitivityToPlayer()
    {
        float xValue = GetCurrentSliderValue(xAxis, SaveManager.MouseSensitivityXKey);
        float yValue = GetCurrentSliderValue(yAxis, SaveManager.MouseSensitivityYKey);

        CacheThirdPersonController();

        if (thirdPersonController != null)
        {
            thirdPersonController.SetMouseSensitivityFromSliderValue(xValue, yValue);
            return;
        }

        if (!missingThirdPersonControllerLogged)
        {
            Debug.LogWarning("[MouseSensitivityController] ThirdPersonController를 찾을 수 없어 마우스 감도를 즉시 적용하지 못했습니다.", this);
            missingThirdPersonControllerLogged = true;
        }
    }

    private void CacheThirdPersonController()
    {
        if (thirdPersonController != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null && playerObject.TryGetComponent(out thirdPersonController))
            return;

        thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
    }

    private float GetCurrentSliderValue(SensitivityAxis axis, string fallbackKey)
    {
        if (axis != null && axis.slider != null)
            return Mathf.Clamp(axis.slider.value, SliderMinValue, SliderMaxValue);

        return LoadSliderValue(fallbackKey);
    }

    private float LoadSliderValue(string key)
    {
        return Mathf.Clamp(SaveManager.Instance.GetMouseSensitivity(key), SliderMinValue, SliderMaxValue);
    }

    private void SaveSliderValue(string key, float sliderValue)
    {
        SaveManager.Instance.SetMouseSensitivity(key, sliderValue);
    }

    private void UpdateValueText(SensitivityAxis axis, float sliderValue)
    {
        if (axis == null || axis.valueText == null)
            return;

        axis.valueText.text = FormatDisplayPercent(sliderValue);
    }

    private static string FormatDisplayPercent(float sliderValue)
    {
        int percent = Mathf.RoundToInt(ConvertSliderValueToDisplayPercent(sliderValue));
        return percent > 0 ? $"+{percent}%" : $"{percent}%";
    }

    private static float ConvertSliderValueToDisplayPercent(float sliderValue)
    {
        float clampedValue = Mathf.Clamp(sliderValue, SliderMinValue, SliderMaxValue);
        return clampedValue < 0f ? clampedValue * 50f : clampedValue * 100f;
    }

    private Slider FindSlider(string objectName)
    {
        Transform sliderTransform = FindChildRecursive(transform, objectName);
        if (sliderTransform != null && sliderTransform.TryGetComponent(out Slider slider))
            return slider;

        return null;
    }

    private TextMeshProUGUI FindTextUnder(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform textTransform = FindChildRecursive(root, objectName);
        if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI text))
            return text;

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
