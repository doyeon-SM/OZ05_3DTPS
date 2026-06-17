using System;
using ProjectSpedex;
using StarterAssets;
using UnityEngine;

namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class PlayerStatus : MonoBehaviour, IDamageable
    {
        [Header("HP")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP;
        [SerializeField] private DoubleIntEventChannel HPChangeChannel;

        [Header("Death")]
        [SerializeField] private GameOverUI gameOverUI;

        private StarterAssets.AnimationController animationController;
        private StarterAssetsInputs starterAssetsInputs;
        private bool deathHandled;

        public bool IsDead => deathHandled;
        public event Action<PlayerStatus> Died;

        private void Awake()
        {
            CacheReferences();
            SetHP();
        }

        private void OnValidate()
        {
            maxHP = Mathf.Max(maxHP, 1);
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        }

        private void SetHP()
        {
            currentHP = maxHP;
            deathHandled = false;
        }

        public void TakeDamage(int value)
        {
            if (deathHandled || value <= 0)
                return;

            currentHP = Mathf.Max(currentHP - value, 0);
            RaiseHPChanged();

            if (currentHP <= 0)
                HandleDeath();
        }

        public void Die()
        {
            if (deathHandled)
                return;

            currentHP = 0;
            RaiseHPChanged();
            HandleDeath();
        }

        private void HandleDeath()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            CacheReferences();

            Debug.Log("Player 사망 시스템 구현 발동.", this);

            if (animationController != null)
            {
                animationController.SetAttack(false);
                animationController.SetAiming(false);
                animationController.SetMove(0f);
                animationController.SetMoveDirection(0f, 0f);
                animationController.SetDead(true);
            }

            if (starterAssetsInputs != null)
                starterAssetsInputs.SetGameplayInputBlocked(true);

            Died?.Invoke(this);

            if (gameOverUI == null)
                gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

            if (gameOverUI != null)
                gameOverUI.Show();
            else
                Debug.LogWarning("[PlayerStatus] GameOverUI를 찾을 수 없어 게임 오버 UI를 출력하지 못했습니다.", this);
        }

        private void RaiseHPChanged()
        {
            if (HPChangeChannel != null)
                HPChangeChannel.Raise(currentHP, maxHP);
        }

        private void CacheReferences()
        {
            if (animationController == null)
                TryGetComponent(out animationController);

            if (starterAssetsInputs == null)
                TryGetComponent(out starterAssetsInputs);

            if (gameOverUI == null)
                gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        }
    }
}
