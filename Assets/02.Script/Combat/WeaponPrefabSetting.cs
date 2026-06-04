using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponPrefabSetting : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;
        //todo : 여기에 총기별 이펙트도 추가해서 껏다 켰다 하면 될듯?

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
