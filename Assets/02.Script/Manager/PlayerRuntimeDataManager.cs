using UnityEngine;

namespace _00.ChoiHeesu._04.MoneyPickup
{
    public class PlayerRuntimeDataManager : MonoBehaviour
    {
        #region singleton

        public static PlayerRuntimeDataManager Instance { get; private set; }

        private bool InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PlayerRuntimeDataManager] 중복 인스턴스가 생성되어 제거합니다.", this);
                Destroy(gameObject);
                return false;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        private void Awake()
        {
            InitializeSingleton();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        //이 스크립트는 RunTime내에 씬이 넘어가도 유지되어야하는 변수(아이템)을 다루는 스크립트입니다.
        [SerializeField] private int money;

        public void GetMoney(int value)
        {
            money += value;
        }
    }
}
