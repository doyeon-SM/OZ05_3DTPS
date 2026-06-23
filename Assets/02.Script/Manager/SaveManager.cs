using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class AudioSaveData
{
    public float masterVolume = 100f;
    public float playerVolume = 100f;
    public float enemyVolume = 100f;
    public float sfxVolume = 100f;
    public float bgmVolume = 100f;
}

[Serializable]
public class MouseSensitivitySaveData
{
    public float xAxis = 0f;
    public float yAxis = 0f;
}

[Serializable]
public class StageClearEntry
{
    public string stageId;
    public bool isCleared;
}

[Serializable]
public class WeaponUnlockEntry
{
    /// <summary>WeaponData.WeaponId 와 동일한 키</summary>
    public string weaponId;
    public bool isUnlocked;
}

[Serializable]
public class SaveData
{
    public AudioSaveData audio = new AudioSaveData();
    public MouseSensitivitySaveData mouseSensitivity = new MouseSensitivitySaveData();
    public List<StageClearEntry> stageClears = new List<StageClearEntry>();
    public List<WeaponUnlockEntry> weaponUnlocks = new List<WeaponUnlockEntry>();
}

/// <summary>
/// 게임 저장/불러오기를 관리하는 싱글톤.
///
/// [저장 항목] (하나의 JSON 파일에 함께 저장)
///  - 오디오 설정 (Master/Player/Enemy/SFX/BGM, 0~100%)
///  - 스테이지 클리어 여부 (stageId(씬 이름) -> bool)
///  - 무기 해금 여부 (weaponId(WeaponData.WeaponId) -> bool)
///
/// [동작]
///  - 게임 시작 시 [RuntimeInitializeOnLoadMethod]로 자동 생성되어 저장 파일을 불러온다.
///    UIController와 달리 씬에 직접 배치할 필요가 없다 — 어떤 씬에서 Play를 눌러도 동작한다.
///  - 게임 종료(OnApplicationQuit) 시 자동으로 파일에 저장한다. (그 전까지는 메모리에만 반영)
///
/// [주의] Unity 기본 JsonUtility는 Dictionary를 직렬화하지 못하므로
///        스테이지 클리어 데이터는 List&lt;StageClearEntry&gt;로 관리한다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public const string MouseSensitivityXKey = "MouseSensitivityX";
    public const string MouseSensitivityYKey = "MouseSensitivityY";

    private static SaveManager _instance;

    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
                CreateInstance();
            return _instance;
        }
    }

    public SaveData Data { get; private set; }

    private const string SaveFileName = "savedata.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 게임 시작 시점에 미리 생성 + 로드해서, 어떤 씬을 먼저 띄우든 항상 준비되어 있게 한다.
        if (_instance == null)
            CreateInstance();
    }

    private static void CreateInstance()
    {
        var go = new GameObject("SaveManager");
        _instance = go.AddComponent<SaveManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromDisk();
    }

    private void OnApplicationQuit()
    {
        SaveToDisk();
    }

    // ── 파일 입출력 ───────────────────────────────────────────

    public void LoadFromDisk()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveManager] 저장 파일을 불러왔습니다: {SavePath}");
            }
            else
            {
                Data = new SaveData();
                Debug.Log("[SaveManager] 저장 파일이 없어 기본값으로 시작합니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 불러오기 실패: {e.Message}");
            Data = new SaveData();
        }

        if (Data == null) Data = new SaveData();
        if (Data.audio == null) Data.audio = new AudioSaveData();
        if (Data.mouseSensitivity == null) Data.mouseSensitivity = new MouseSensitivitySaveData();
        if (Data.stageClears == null) Data.stageClears = new List<StageClearEntry>();
        if (Data.weaponUnlocks == null) Data.weaponUnlocks = new List<WeaponUnlockEntry>();
    }

    public void SaveToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] 저장 완료: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    // ── 오디오 설정 ───────────────────────────────────────────

    public float GetAudioVolume(string key)
    {
        switch (key)
        {
            case "MasterVolume": return Data.audio.masterVolume;
            case "PlayerVolume": return Data.audio.playerVolume;
            case "EnemyVolume":  return Data.audio.enemyVolume;
            case "SFXVolume":    return Data.audio.sfxVolume;
            case "BGMVolume":    return Data.audio.bgmVolume;
            default:
                Debug.LogWarning($"[SaveManager] 알 수 없는 오디오 키: {key}");
                return 100f;
        }
    }

    public void SetAudioVolume(string key, float percent)
    {
        switch (key)
        {
            case "MasterVolume": Data.audio.masterVolume = percent; break;
            case "PlayerVolume": Data.audio.playerVolume = percent; break;
            case "EnemyVolume":  Data.audio.enemyVolume = percent; break;
            case "SFXVolume":    Data.audio.sfxVolume = percent; break;
            case "BGMVolume":    Data.audio.bgmVolume = percent; break;
            default:
                Debug.LogWarning($"[SaveManager] 알 수 없는 오디오 키: {key}");
                break;
        }
    }

    // ── 마우스 감도 설정 ─────────────────────────────────────

    public float GetMouseSensitivity(string key)
    {
        switch (key)
        {
            case MouseSensitivityXKey: return Data.mouseSensitivity.xAxis;
            case MouseSensitivityYKey: return Data.mouseSensitivity.yAxis;
            default:
                Debug.LogWarning($"[SaveManager] 알 수 없는 마우스 감도 키: {key}");
                return 0f;
        }
    }

    public void SetMouseSensitivity(string key, float sliderValue)
    {
        float clampedValue = Mathf.Clamp(sliderValue, -1f, 1f);

        switch (key)
        {
            case MouseSensitivityXKey:
                Data.mouseSensitivity.xAxis = clampedValue;
                break;
            case MouseSensitivityYKey:
                Data.mouseSensitivity.yAxis = clampedValue;
                break;
            default:
                Debug.LogWarning($"[SaveManager] 알 수 없는 마우스 감도 키: {key}");
                break;
        }
    }

    public void SetMouseSensitivity(float xAxisValue, float yAxisValue)
    {
        Data.mouseSensitivity.xAxis = Mathf.Clamp(xAxisValue, -1f, 1f);
        Data.mouseSensitivity.yAxis = Mathf.Clamp(yAxisValue, -1f, 1f);
    }

    // ── 스테이지 클리어 ───────────────────────────────────────

    /// <summary>stageId는 보통 스테이지 씬 이름(StageInfoSO.sceneName)을 사용한다.</summary>
    public bool IsStageCleared(string stageId)
    {
        foreach (var entry in Data.stageClears)
            if (entry.stageId == stageId) return entry.isCleared;
        return false;
    }

    public void SetStageCleared(string stageId, bool isCleared)
    {
        foreach (var entry in Data.stageClears)
        {
            if (entry.stageId == stageId)
            {
                entry.isCleared = isCleared;
                return;
            }
        }
        Data.stageClears.Add(new StageClearEntry { stageId = stageId, isCleared = isCleared });
    }

    // ── 무기 해금 ───────────────────────────────────────────

    /// <summary>weaponId 는 WeaponData.WeaponId 와 동일한 값을 사용한다.</summary>
    public bool IsWeaponUnlocked(string weaponId)
    {
        foreach (var entry in Data.weaponUnlocks)
            if (entry.weaponId == weaponId) return entry.isUnlocked;
        return false;
    }

    public void SetWeaponUnlocked(string weaponId, bool isUnlocked)
    {
        foreach (var entry in Data.weaponUnlocks)
        {
            if (entry.weaponId == weaponId)
            {
                entry.isUnlocked = isUnlocked;
                return;
            }
        }
        Data.weaponUnlocks.Add(new WeaponUnlockEntry { weaponId = weaponId, isUnlocked = isUnlocked });
    }
}
