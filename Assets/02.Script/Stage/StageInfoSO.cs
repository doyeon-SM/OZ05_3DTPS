using UnityEngine;

/// <summary>
/// 스테이지 하나의 기본 정보를 담는 ScriptableObject.
/// Create > Stage > StageInfoSO 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "NewStageInfo", menuName = "Stage/StageInfoSO")]
public class StageInfoSO : ScriptableObject
{
    [Header("표시 정보")]
    [Tooltip("UI에 표시될 스테이지 이름")]
    public string stageName;
    public string stageLevel;
    public string stageInfoTitle;
    [TextArea(3, 6)]
    public string stageInfo;

    [Header("씬 이동 정보")]
    [Tooltip("이동할 씬 이름 (Build Settings에 등록된 이름)")]
    public string sceneName;

    [Tooltip("도착 씬의 스폰 포인트 이름")]
    public string spawnPointName = "SpawnPoint";
}
