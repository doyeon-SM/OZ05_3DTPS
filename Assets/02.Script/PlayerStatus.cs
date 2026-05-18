using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class PlayerStatus : MonoBehaviour
    {
        [Header("HP")]
        public int maxHP = 100;
        public int currentHP = 50;
        
        [Header("MP")]
        public int maxMP = 50;
        public int currentMP = 10;
        
        [Header("Combat")]
        public int attack = 10;
        public int defence = 5;
        
        [Header("Currency")]
        public int gold;
        
        public void HealHP(int amount)
        {
            currentHP = Mathf.Min(currentHP + amount, maxHP);
            Debug.Log($" HP 회복 : +{amount}, 현재 HP : {currentHP}");
        }

        public void HealMP(int amount)
        {
            currentMP = Mathf.Min(currentMP + amount, maxMP);
            Debug.Log($" MP 회복 : +{amount}, 현재 MP : {currentMP}");
        }

        public void IncreaseAttack(int amount)
        {
            attack += amount;
            Debug.Log($" 공격력 증가 : +{amount}, 현재 HP : {attack}");
        }
        public void IncreaseDefense(int amount)
        {
            defence += amount;
            Debug.Log($" 방어력 증가 : +{amount}, 현재 HP : {defence}");
        }
        
        public void AddGold(int amount)
        {
            gold += amount;
            Debug.Log($" 골드 흭득 : +{amount}, 현재 HP : {gold}");
        }
        
        
        
        
        
        
        
        
        
        
        
        
    }
}