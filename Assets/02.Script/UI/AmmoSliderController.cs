using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _02.Script.UI
{
    public class AmmoSliderController: MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel AmmoChanged;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI AmmoText;
        
        private void OnEnable()
        {
            AmmoChanged.Register(OnAmmoChanged);
        }

        private void OnDisable()
        {
            AmmoChanged.Unregister(OnAmmoChanged);
        }
        
        private void OnAmmoChanged(int currenthp,int maxHp)
        {
            if(slider == null || AmmoText == null) return;
            string ammoText = $"{currenthp} / {maxHp}";
            AmmoText.text = ammoText;
            
            float hpRatio = (float)currenthp / maxHp;
            slider.value = hpRatio;
        }
    }
}