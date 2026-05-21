using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class EnemyStatus : MonoBehaviour , IDamageable
    {
        [SerializeField]private int currentHealth;
        [SerializeField]private int maxHealth = 100;
        private bool isDead => currentHealth <= 0;

        private void Awake()
        {
            resetHealth();
        }

        private void resetHealth()
        {
                currentHealth = maxHealth;
        }


        public void TakeDamage(int value)
        {
            currentHealth -= value;
            if (isDead)
            {
                Debug.Log($"{gameObject.name}이 죽었습니다.");
                Destroy(gameObject);
            }
        }
    }
}