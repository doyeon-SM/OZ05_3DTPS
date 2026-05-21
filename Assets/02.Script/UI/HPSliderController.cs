using UnityEngine;
using UnityEngine.UI;

namespace _01.Scenes.PhaseValidation.UI
{
    public class HPSliderController : MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel hpChanged;
        [SerializeField] private Slider slider;

        private void OnEnable()
        {
            hpChanged.Register(OnHpChanged);
        }

        private void OnDisable()
        {
            hpChanged.Unregister(OnHpChanged);
        }

        private void OnHpChanged(int currenthp,int maxHp)
        {   
            float hpRatio = (float)currenthp / maxHp;
            slider.value = hpRatio;
        }
    }
}