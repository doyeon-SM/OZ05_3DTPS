using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 시작 시 SaveManager에 저장된 오디오 설정(Master/Player/Enemy/SFX/BGM)을
/// AudioMixer에 즉시 적용하는 부트스트랩 매니저.
///
/// [왜 필요한가]
///  OptionAudioController는 옵션 메뉴(P_Audio 패널)가 실제로 Instantiate될 때만 Awake가 실행되어
///  저장된 값을 AudioMixer에 적용한다. 옵션 메뉴를 한 번도 열지 않으면 AudioMixer는 믹서 에셋의
///  기본값(0dB)을 그대로 사용하게 되어, 저장된 설정이 "한 번 갱신되기 전까지" 반영되지 않는
///  문제가 있었다. 이 매니저는 옵션 메뉴를 열지 않아도 게임 시작 즉시 저장값을 적용한다.
///
/// [동작]
///  - MainScene에 배치, BGMManager/UIController와 동일하게 DontDestroyOnLoad로 전 씬에서 유지된다.
///  - Awake() 시점에 SaveManager.Instance에 저장된 5개 채널 값을 AudioMixer에 곧바로 적용한다.
///  - dB 변환 공식은 OptionAudioController와 AudioVolumeUtility를 공유하므로 두 곳의 결과가 항상 일치한다.
///
/// [Inspector 설정]
///  audioMixer : NewAudioMixer.mixer 할당
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    // SaveManager.GetAudioVolume()의 키 이름과 AudioMixer Expose 파라미터 이름이 동일하다.
    private static readonly string[] AudioParameterNames =
    {
        "MasterVolume",
        "PlayerVolume",
        "EnemyVolume",
        "SFXVolume",
        "BGMVolume",
    };

    [Header("오디오 믹서")]
    [Tooltip("NewAudioMixer를 할당합니다.")]
    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        ApplySavedSettings();
    }

    private void ApplySavedSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioSettingsManager] audioMixer가 연결되지 않았습니다. 저장된 오디오 설정을 적용할 수 없습니다.");
            return;
        }

        foreach (string parameterName in AudioParameterNames)
        {
            float percent = SaveManager.Instance.GetAudioVolume(parameterName);
            AudioVolumeUtility.ApplyVolume(audioMixer, parameterName, percent);
        }

        Debug.Log("[AudioSettingsManager] 저장된 오디오 설정을 게임 시작 시점에 적용했습니다.");
    }
}
