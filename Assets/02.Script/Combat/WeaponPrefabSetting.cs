using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponPrefabSetting : MonoBehaviour
    {
        [Header("총구 이름을 무조건'MuzzlePoint'로 해주세요.")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private string WeaponID;
        //todo : 여기에 총기별 이펙트도 추가해서 껏다 켰다 하면 될듯?

        public string WeaponId => WeaponID;

        private void Awake()
        {
            muzzlePointFind();
        }

        private void muzzlePointFind()
        {
            if (muzzlePoint == null)
            {
                muzzlePoint = transform.Find("MuzzlePoint");
            }
        }
        public bool muzzleOut(out Transform muzzle)
        {
            muzzlePointFind();

            if (muzzlePoint == null)
            {
                muzzle = null;
                return false;
            }
            muzzle = muzzlePoint;
            return true;
        }
    }
}
