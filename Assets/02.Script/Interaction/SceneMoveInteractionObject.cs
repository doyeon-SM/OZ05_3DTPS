using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMoveInteractionObject : MonoBehaviour, IInteraction
{
    [SerializeField] private string targetSceneName = "EasyStageScene";
    [SerializeField] private string spawnPointName = "SpawnPoint";

    public void Interaction()
    {
        if (ScenePositionManager.Instance == null)
        {
            Debug.LogError("[SceneMoveInteractionObject] ScenePositionManager 인스턴스가 없습니다.");
            return;
        }

        ScenePositionManager.Instance.SetNextSpawnPoint(spawnPointName);
        SceneManager.LoadScene(targetSceneName);

        Debug.Log($"[SceneMoveInteractionObject] '{targetSceneName}' 씬으로 이동, SpawnPoint: '{spawnPointName}'");
    }
}
