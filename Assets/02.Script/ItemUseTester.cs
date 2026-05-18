using UnityEngine;
using UnityEngine.InputSystem;

namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class ItemUseTester : MonoBehaviour
    {
        [SerializeField] private PlayerStatus playerStatus;
        [SerializeField] private ItemData itemToUse;
        [SerializeField] private InputAction useItemAction = new InputAction("UseItem", InputActionType.Button,"<Keyboard>/u");

        private void OnEnable()
        {
            useItemAction.performed += OnUseItemPerformed;
            useItemAction.Enable();
        }

        private void OnDisable()
        {
            useItemAction.performed -= OnUseItemPerformed;
            useItemAction.Disable();
        }

        private void OnUseItemPerformed(InputAction.CallbackContext _)
        {
            UseItem(itemToUse);
        }

        private void UseItem(ItemData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning("사용할 아이템이 없습니다.");
                return;
            }

            if (!itemData.canUse)
            {
                Debug.LogWarning($"{itemData.name}사용할 수 없는 아이템 입니다.");
                return;
            }
            Debug.Log($"아이템 사용 {itemData.name}");

            foreach (ItemEffect effect in itemData.effects)
            {
                ApplyEffect(effect);
            }
        }

        private void ApplyEffect(ItemEffect effect)
        {
            switch (effect.effectType)
            {
                case ItemEffectType.HealHP : 
                    playerStatus.HealHP(effect.value);
                    break;
                case ItemEffectType.HealMP :
                    playerStatus.HealMP(effect.value);
                    break;
                case ItemEffectType.IncreaseAttack :
                    playerStatus.IncreaseAttack(effect.value);
                    break;
                case ItemEffectType.IncreaseDefense :
                    playerStatus.IncreaseDefense(effect.value);
                    break;
                case ItemEffectType.AddGold :
                    playerStatus.AddGold(effect.value);
                    break;
            }
        }
    }
}