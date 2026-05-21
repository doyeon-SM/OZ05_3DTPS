using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation.UI
{
    public class UIController : MonoBehaviour
    {
        //각 UI가 겹치지 않도록 하는게 목표입니다.
        [SerializeField] private GameObject combatUI;
        [SerializeField] private GameObject itemInfoUI;

        // 각 UI가 Active 되면 해당 Stack에 담습니다.
        // ESC를 누르면 열린 UI 순서대로 닫기게끔 구현하기 위함.
        private Stack<GameObject> activeUiList;

        private void combatUI_Active()
        {
            
        }
        
        
    }
}