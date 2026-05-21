using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class PlayerStatus : MonoBehaviour , IDamageable
    {
        [Header("HP")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP;
        [SerializeField] private DoubleIntEventChannel HPChangeChannel;
        private bool isDead => currentHP <= 0;
        private void Awake()
        {
            SetHP();
        }

        private void SetHP()
        {
            currentHP = maxHP;
        }
        
        public void TakeDamage(int value)
        {
            currentHP = Mathf.Max(currentHP - value, 0);
            
            // 데미지 받는 애니메이션
            // 피격 효과 구현 ( 화면 주위 빨개짐 , 피격 방향 등)
            HPChangeChannel.Raise(currentHP, maxHP);
            if (isDead)
            {
                Debug.Log("Player 사망 시스템 구현 발동.");
                // 사망 애니메이션 출력
                // 사망 Event 발송 -> 게임 오버 화면 출력.
            }
        }
    }
}