using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// P_Audio 패널 — AudioMixer 볼륨을 슬라이더로 조절한다.
///
/// [그룹]
///  NewAudioMixer.mixer 기준 Master(부모) - Player / Enemy / SFX / BGM(자식 4개)
///  Master를 조절하면 라우팅 구조상 자식 그룹에도 자동으로 영향을 준다.
///
/// [전제]
///  - AudioMixer에 아래 5개 파라미터가 "Expose to script"로 노출되어 있어야 한다.
///    MasterVolume, PlayerVolume, EnemyVolume, SFXVolume, BGMVolume
///  - 각 Slider는 Min=0, Max=1 (Unity 기본값) 기준이며, 0~100%로 환산해 표시한다.
///
/// [저장]
///  - SaveManager(JSON 단일 파일)를 통해 저장/로드한다.
///    슬라이더 조작 시 SaveManager의 메모리 상 값만 갱신되고, 실제 파일 저장은
///    게임 종료 시 SaveManager.OnApplicationQuit()에서 한 번에 처리된다.
///
/// [참고]
///  - 게임 시작 시점(이 패널을 열기 전)의 초기 적용은 AudioSettingsManager가 담당한다.
///  - dB 변환 공식은 AudioVolumeUtility를 공유하여 두 곳의 결과가 항상 일치한다.
/// </summary>
public class OptionAudioController : MonoBehaviour
{
    [System.Serializable]
    private class AudioChannel
    {
        [Tooltip("AudioMixer에 노출된 파라미터 이름 (예: MasterVolume)")]
        public string exposedParameterName;

        [Tooltip("SaveManager에 저장할 키 (기본적으로 exposedParameterName과 동일하게 사용)")]
        public string prefsKey;

        public Slider slider;
        public TextMeshProUGUI percentText;
    }

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("채널 (Slider / 퍼센트 텍스트는 Inspector에서 연결)")]
    [SerializeField]
    private AudioChannel[] channels = new AudioChannel[]
    {
        new AudioChannel { exposedParameterName = "MasterVolume", prefsKey = "MasterVolume" },
        new AudioChannel { exposedParameterName = "PlayerVolume", prefsKey = "PlayerVolume" },
        new AudioChannel { exposedParameterName = "EnemyVolume",  prefsKey = "EnemyVolume" },
        new AudioChannel { exposedParameterName = "SFXVolume",    prefsKey = "SFXVolume" },
        new AudioChannel { exposedParameterName = "BGMVolume",    prefsKey = "BGMVolume" },
    };

    private void Awake()
    {
        if (audioMixer == null)
            Debug.LogWarning("[OptionAudioController] audioMixer가 연결되지 않았습니다.");

        for (int i = 0; i < channels.Length; i++)
        {
            var channel = channels[i];
            if (channel == null || channel.slider == null)
            {
                Debug.LogWarning($"[OptionAudioController] channels[{i}]의 slider가 연결되지 않았습니다.");
                continue;
            }

            float savedPercent = LoadVolumePercent(channel.prefsKey);
            channel.slider.SetValueWithoutNotify(savedPercent / 100f);
            ApplyVolume(channel, savedPercent);

            channel.slider.onValueChanged.AddListener(normalizedValue => OnSliderChanged(channel, normalizedValue));
        }
    }

    private void OnSliderChanged(AudioChannel channel, float normalizedValue)
    {
        float percent = normalizedValue * 100f;
        ApplyVolume(channel, percent);
        SaveVolumePercent(channel.prefsKey, percent);
    }

    private void ApplyVolume(AudioChannel channel, float percent)
    {
        if (audioMixer != null)
            AudioVolumeUtility.ApplyVolume(audioMixer, channel.exposedParameterName, percent);

        if (channel.percentText != null)
            channel.percentText.text = Mathf.RoundToInt(percent) + "%";
    }

    // ── 저장/로드 (SaveManager 경유 — 메모리만 갱신, 실제 파일 저장은 종료 시 일괄 처리) ──

    private float LoadVolumePercent(string key)
    {
        return SaveManager.Instance.GetAudioVolume(key);
    }

    private void SaveVolumePercent(string key, float percent)
    {
        SaveManager.Instance.SetAudioVolume(key, percent);
    }
}
