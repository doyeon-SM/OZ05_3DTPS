using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [CreateAssetMenu(fileName ="BossData", menuName ="Stage/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("기본 정보")]
        public GameObject prefab;

        [Header("스탯")]
        public int maxHealth = 100;
        public int attackPower = 10;
    }
}
