using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponPrefabSetting : MonoBehaviour
    {
        [Header("총구 이름을 무조건'MuzzlePoint'로 해주세요.")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private string WeaponID;

        [Header("총구 이펙트")]
        [SerializeField] private Transform muzzleEffectRoot;
        [SerializeField] private string muzzleEffectRootName = "vfx_Muzzle_BulletOrange";
        [SerializeField] private bool disableMuzzleEffectPlayOnAwake = true;
        [SerializeField] private bool stopMuzzleEffectOnEnable = true;
        [SerializeField] private bool restartMuzzleEffectOnPlay = true;

        private ParticleSystem[] muzzleEffects;

        public string WeaponId => WeaponID;
        public Transform MuzzleEffectRoot => muzzleEffectRoot;
        public ParticleSystem MuzzleEffect
        {
            get
            {
                muzzleEffectFind();
                return muzzleEffects != null && muzzleEffects.Length > 0 ? muzzleEffects[0] : null;
            }
        }

        private void Awake()
        {
            muzzlePointFind();
            muzzleEffectFind();
            PrepareMuzzleEffectsForManualPlay();
            StopMuzzleEffect();
        }

        private void OnEnable()
        {
            if (!stopMuzzleEffectOnEnable)
                return;

            PrepareMuzzleEffectsForManualPlay();
            StopMuzzleEffect();
        }

        private void muzzlePointFind()
        {
            if (muzzlePoint == null)
            {
                muzzlePoint = transform.Find("MuzzlePoint");
            }
        }

        private void muzzleEffectFind()
        {
            if (muzzleEffects != null && muzzleEffects.Length > 0)
                return;

            if (muzzleEffectRoot == null)
                muzzleEffectRoot = FindMuzzleEffectRoot();

            if (muzzleEffectRoot == null)
                return;

            muzzleEffects = muzzleEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
        }

        private void PrepareMuzzleEffectsForManualPlay()
        {
            muzzleEffectFind();

            if (!disableMuzzleEffectPlayOnAwake)
                return;

            if (muzzleEffects == null)
                return;

            for (int i = 0; i < muzzleEffects.Length; i++)
            {
                ParticleSystem muzzleEffect = muzzleEffects[i];
                if (muzzleEffect == null)
                    continue;

                ParticleSystem.MainModule main = muzzleEffect.main;
                main.playOnAwake = false;
            }
        }

        private Transform FindMuzzleEffectRoot()
        {
            if (string.IsNullOrWhiteSpace(muzzleEffectRootName))
                return null;

            Transform effectRoot = transform.Find(muzzleEffectRootName);
            if (effectRoot != null)
                return effectRoot;

            muzzlePointFind();

            if (muzzlePoint != null)
            {
                effectRoot = muzzlePoint.Find(muzzleEffectRootName);
                if (effectRoot != null)
                    return effectRoot;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == transform)
                    continue;

                if (children[i].name == muzzleEffectRootName)
                    return children[i];
            }

            return null;
        }

        public bool PlayMuzzleEffect()
        {
            muzzleEffectFind();

            if (muzzleEffects == null || muzzleEffects.Length == 0)
            {
                Debug.LogWarning("[WeaponPrefabSetting] Muzzle Effect가 연결되어 있지 않습니다.", this);
                return false;
            }

            bool played = false;
            for (int i = 0; i < muzzleEffects.Length; i++)
            {
                ParticleSystem muzzleEffect = muzzleEffects[i];
                if (muzzleEffect == null)
                    continue;

                if (restartMuzzleEffectOnPlay)
                    muzzleEffect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

                muzzleEffect.Play(false);
                played = true;
            }

            return played;
        }

        public void StopMuzzleEffect()
        {
            muzzleEffectFind();

            if (muzzleEffects == null)
                return;

            for (int i = 0; i < muzzleEffects.Length; i++)
            {
                ParticleSystem muzzleEffect = muzzleEffects[i];
                if (muzzleEffect == null)
                    continue;

                muzzleEffect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
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
