using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePositionManager : MonoBehaviour
{
    public static ScenePositionManager Instance;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool shouldReposition = false;

    private string spawnPointName = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 기존: 직접 Vector3/Quaternion 지정
    public void SetNextSpawnTransform(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
        spawnPointName = null;
        shouldReposition = true;
    }

    // 신규: SpawnPoint GameObject 이름으로 지정
    public void SetNextSpawnPoint(string pointName)
    {
        spawnPointName = pointName;
        shouldReposition = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldReposition) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[ScenePositionManager] Player 태그 오브젝트를 찾을 수 없습니다.");
            shouldReposition = false;
            return;
        }

        if (!string.IsNullOrEmpty(spawnPointName))
        {
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;
                Debug.Log($"[ScenePositionManager] SpawnPoint '{spawnPointName}' 위치로 이동 완료.");
            }
            else
            {
                Debug.LogWarning($"[ScenePositionManager] '{spawnPointName}' 오브젝트를 씬에서 찾을 수 없습니다.");
            }
        }
        else
        {
            player.transform.position = targetPosition;
            player.transform.rotation = targetRotation;
        }

        shouldReposition = false;
        spawnPointName = null;
    }
}
