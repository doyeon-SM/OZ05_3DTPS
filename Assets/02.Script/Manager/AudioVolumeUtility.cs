using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 0~100% 퍼센트 값을 AudioMixer의 dB 값으로 변환해 적용하는 공용 유틸리티.
///
/// OptionAudioController(슬라이더 변경 시)와 AudioSettingsManager(게임 시작 시 저장값 적용)
/// 양쪽에서 동일한 변환 공식을 사용하도록 공유한다. 두 곳에 각자 구현하면 수치가
/// 미묘하게 어긋나거나, 한쪽만 수정되어 결과가 달라지는 문제가 생길 수 있다.
/// </summary>
public static class AudioVolumeUtility
{
    private const float MinLinearVolume = 0.0001f;   // 0% 근처를 -80dB(완전 무음)로 처리하기 위한 최소값

    /// <summary>percent(0~100)를 dB로 변환해 mixer의 exposedParameterName에 적용한다.</summary>
    public static void ApplyVolume(AudioMixer mixer, string exposedParameterName, float percent)
    {
        if (mixer == null || string.IsNullOrEmpty(exposedParameterName))
            return;

        float dB = PercentToDecibel(percent);

        bool applied = mixer.SetFloat(exposedParameterName, dB);
        if (!applied)
            Debug.LogWarning($"[AudioVolumeUtility] '{exposedParameterName}' 파라미터를 찾을 수 없습니다. AudioMixer에서 Expose 했는지 확인하세요.");
    }

    /// <summary>percent(0~100)를 AudioMixer dB 값으로 변환한다.</summary>
    public static float PercentToDecibel(float percent)
    {
        float linear = Mathf.Clamp01(percent / 100f);
        return linear <= 0f ? -80f : Mathf.Log10(Mathf.Max(linear, MinLinearVolume)) * 20f;
    }
}
