using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM 씬별 매핑 엔트리 — Inspector에서 씬 이름과 클립을 쌍으로 등록한다.
/// </summary>
[Serializable]
public class SceneBGMEntry
{
    [Tooltip("Build Settings의 씬 이름과 정확히 일치해야 합니다.")]
    public string sceneName;

    [Tooltip("해당 씬에서 재생할 BGM 클립")]
    public AudioClip bgmClip;
}

/// <summary>
/// 씬 전환에 따라 BGM을 자동으로 교체하는 싱글톤 매니저.
///
/// [동작 요약]
///  - MainScene에 배치된 GameObject에 부착, DontDestroyOnLoad로 전 씬에서 유지된다.
///  - SceneManager.sceneLoaded 이벤트를 구독하여 씬 전환 시 sceneBGMEntries에서
///    해당 씬 이름과 일치하는 클립을 찾아 크로스페이드로 교체한다.
///  - 매핑에 없는 씬으로 전환되면 현재 BGM을 페이드아웃 후 정지한다.
///  - AudioSource 2개를 교대로 사용해 부드러운 크로스페이드를 구현한다.
///  - AudioMixer BGM 그룹을 출력으로 지정해 OptionAudioController의 볼륨 제어와 연동된다.
///
/// [Inspector 설정]
///  1. sceneBGMEntries : 씬 이름 / AudioClip 쌍을 원하는 수만큼 추가
///  2. bgmMixerGroup   : NewAudioMixer의 BGM 그룹 할당
///  3. crossFadeDuration : 크로스페이드 길이 (초)
/// </summary>
public class BGMManager : MonoBehaviour
{
    // ── 싱글톤 ─────────────────────────────────────────────────────────────
    private static BGMManager _instance;

    public static BGMManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogWarning("[BGMManager] 아직 인스턴스가 없습니다. MainScene에 BGMManager가 배치되어 있는지 확인하세요.");
            return _instance;
        }
    }

    // ── Inspector 노출 필드 ────────────────────────────────────────────────
    [Header("씬별 BGM 매핑 (씬 이름 + AudioClip)")]
    [SerializeField] private SceneBGMEntry[] sceneBGMEntries;

    [Header("오디오 믹서")]
    [Tooltip("NewAudioMixer의 BGM 그룹을 할당합니다.")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    [Header("크로스페이드")]
    [Tooltip("이전 BGM → 새 BGM으로 교체할 때 걸리는 시간 (초)")]
    [SerializeField] [Range(0f, 5f)] private float crossFadeDuration = 1f;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    private AudioSource[] _sources;   // [0], [1] 교대 사용 (크로스페이드)
    private int _activeIndex = 0;
    private Coroutine _fadeCoroutine;

    // ── 초기화 ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitAudioSources();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_instance == this)
            _instance = null;
    }

    private void InitAudioSources()
    {
        _sources = new AudioSource[2];

        for (int i = 0; i < 2; i++)
        {
            _sources[i] = gameObject.AddComponent<AudioSource>();
            _sources[i].loop         = true;
            _sources[i].playOnAwake  = false;
            _sources[i].volume       = 0f;

            if (bgmMixerGroup != null)
                _sources[i].outputAudioMixerGroup = bgmMixerGroup;
            else
                Debug.LogWarning("[BGMManager] bgmMixerGroup이 연결되지 않았습니다. OptionAudioController의 BGM 볼륨 슬라이더가 적용되지 않습니다.");
        }
    }

    // ── 씬 전환 감지 ────────────────────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Additive 로드는 메인 씬 BGM을 유지
        if (mode == LoadSceneMode.Additive)
            return;

        AudioClip clip = FindClipForScene(scene.name);
        PlayBGM(clip);
    }

    private AudioClip FindClipForScene(string sceneName)
    {
        if (sceneBGMEntries == null || sceneBGMEntries.Length == 0)
            return null;

        foreach (var entry in sceneBGMEntries)
        {
            if (entry != null && entry.sceneName == sceneName)
                return entry.bgmClip;
        }

        return null; // 매핑 없음 → 정지
    }

    // ── 공개 재생 API ───────────────────────────────────────────────────────
    /// <summary>
    /// 지정한 클립을 크로스페이드로 재생한다.
    /// null을 넘기면 현재 BGM을 페이드아웃 후 정지한다.
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        // 이미 같은 클립이 재생 중이면 아무것도 하지 않는다
        if (_sources[_activeIndex].clip == clip &&
            _sources[_activeIndex].isPlaying &&
            clip != null)
            return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = clip != null
            ? StartCoroutine(CrossFade(clip))
            : StartCoroutine(FadeOut(_sources[_activeIndex]));
    }

    /// <summary>현재 BGM을 페이드아웃 후 정지한다.</summary>
    public void StopBGM()
    {
        PlayBGM(null);
    }

    /// <summary>씬 이름으로 직접 BGM을 바꾼다. (스크립트에서 수동 호출용)</summary>
    public void PlayBGMForScene(string sceneName)
    {
        AudioClip clip = FindClipForScene(sceneName);
        PlayBGM(clip);
    }

    // ── 내부 페이드 코루틴 ──────────────────────────────────────────────────
    private IEnumerator CrossFade(AudioClip newClip)
    {
        AudioSource outgoing = _sources[_activeIndex];
        int nextIndex        = (_activeIndex + 1) % 2;
        AudioSource incoming = _sources[nextIndex];

        float startVolume = outgoing.volume;
        float elapsed     = 0f;

        // 새 소스 준비 및 재생 시작
        incoming.clip   = newClip;
        incoming.volume = 0f;
        incoming.Play();

        // 페이드
        if (crossFadeDuration > 0f)
        {
            while (elapsed < crossFadeDuration)
            {
                elapsed           += Time.unscaledDeltaTime;
                float t            = Mathf.Clamp01(elapsed / crossFadeDuration);
                outgoing.volume    = Mathf.Lerp(startVolume, 0f, t);
                incoming.volume    = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
        }

        // 정리
        outgoing.Stop();
        outgoing.clip   = null;
        outgoing.volume = 0f;

        incoming.volume = 1f;
        _activeIndex    = nextIndex;
        _fadeCoroutine  = null;
    }

    private IEnumerator FadeOut(AudioSource source)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        if (crossFadeDuration > 0f)
        {
            while (elapsed < crossFadeDuration)
            {
                elapsed        += Time.unscaledDeltaTime;
                source.volume   = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / crossFadeDuration));
                yield return null;
            }
        }

        source.Stop();
        source.clip   = null;
        source.volume = 0f;
        _fadeCoroutine = null;
    }

    // ── 에디터 디버그 헬퍼 (빌드에 포함되지 않음) ────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("현재 씬 BGM 재생 (테스트)")]
    private void DebugPlayCurrentScene()
    {
        PlayBGMForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    [ContextMenu("BGM 정지 (테스트)")]
    private void DebugStop()
    {
        StopBGM();
    }
#endif
}
