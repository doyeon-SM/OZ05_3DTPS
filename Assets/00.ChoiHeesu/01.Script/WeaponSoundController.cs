using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class WeaponSoundController : MonoBehaviour
    {
        private const string ClipFolder = "Assets/00.ChoiHeesu/SFX/Guns_Weapons/Guns";

        [Header("Audio Source")]
        [SerializeField] private AudioSource shotAudioSource;
        [SerializeField] private AudioSource shotTailAudioSource;
        [SerializeField] private AudioSource reloadAudioSource;
        [SerializeField, Range(0f, 1f)] private float shotVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float shotTailVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float reloadVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.35f;

        [Header("Pistol Shot")]
        [SerializeField] private AudioClip[] pistolShotClips;

        [Header("Pistol Reload")]
        [SerializeField] private AudioClip[] pistolRemoveMagazineClips;
        [SerializeField] private AudioClip[] pistolInsertMagazineClips;
        [SerializeField] private AudioClip[] pistolReloadFinishClips;
        [SerializeField, Range(0f, 1f)] private float removeMagazineNormalizedTime = 0.08f;
        [SerializeField, Range(0f, 1f)] private float insertMagazineNormalizedTime = 0.48f;
        [SerializeField, Range(0f, 1f)] private float finishReloadNormalizedTime = 0.82f;
        [SerializeField] private float minimumReloadSequenceDuration = 0.1f;

        [Header("SMG Shot")]
        [SerializeField] private AudioClip smgFirstShotClip;
        [SerializeField] private AudioClip[] smgShotClips;
        [SerializeField] private AudioClip smgTailOnlyClip;
        [SerializeField] private float smgFirstShotResetTime = 0.18f;
        [SerializeField] private float smgShotSequenceResetMultiplier = 1.5f;

        [Header("SMG Reload")]
        [SerializeField] private AudioClip[] smgMagazineUnloadClips;
        [SerializeField] private AudioClip[] smgMagazineLoadClips;
        [SerializeField] private AudioClip[] smgReloadFinishClips;
        [SerializeField, Range(0f, 1f)] private float smgMagazineUnloadNormalizedTime = 0.08f;
        [SerializeField, Range(0f, 1f)] private float smgMagazineLoadNormalizedTime = 0.48f;
        [SerializeField, Range(0f, 1f)] private float smgFinishReloadNormalizedTime = 0.82f;

        [Header("SG Shot")]
        [SerializeField] private AudioClip[] sgShotClips;

        [Header("SG Reload")]
        [SerializeField] private AudioClip[] sgMagazineUnloadClips;
        [SerializeField] private AudioClip[] sgMagazineLoadClips;
        [SerializeField] private AudioClip[] sgReloadFinishClips;
        [SerializeField, Range(0f, 1f)] private float sgMagazineUnloadNormalizedTime = 0.08f;
        [SerializeField, Range(0f, 1f)] private float sgMagazineLoadNormalizedTime = 0.48f;
        [SerializeField, Range(0f, 1f)] private float sgFinishReloadNormalizedTime = 0.82f;

        [Header("AR Shot")]
        [SerializeField] private AudioClip[] arShotClips;

        [Header("AR Reload")]
        [SerializeField] private AudioClip[] arMagazineUnloadClips;
        [SerializeField] private AudioClip[] arMagazineLoadClips;
        [SerializeField] private AudioClip[] arReloadFinishClips;
        [SerializeField, Range(0f, 1f)] private float arMagazineUnloadNormalizedTime = 0.08f;
        [SerializeField, Range(0f, 1f)] private float arMagazineLoadNormalizedTime = 0.48f;
        [SerializeField, Range(0f, 1f)] private float arFinishReloadNormalizedTime = 0.82f;

        [Header("MG Shot")]
        [SerializeField] private AudioClip mgFirstShotClip;
        [SerializeField] private AudioClip[] mgShotClips;
        [SerializeField] private AudioClip mgTailOnlyClip;
        [SerializeField] private float mgFirstShotResetTime = 0.18f;
        [SerializeField] private float mgShotSequenceResetMultiplier = 1.5f;

        [Header("MG Reload")]
        [SerializeField] private AudioClip[] mgReloadClips;

#if UNITY_EDITOR
        [Header("Editor Auto Fill")]
        [SerializeField] private bool autoAssignClipsInEditor = true;
#endif

        private Coroutine reloadCoroutine;
        private int lastPistolShotIndex = -1;
        private int lastPistolRemoveMagazineIndex = -1;
        private int lastPistolInsertMagazineIndex = -1;
        private int lastPistolReloadFinishIndex = -1;
        private int lastSmgShotIndex = -1;
        private int lastSmgMagazineUnloadIndex = -1;
        private int lastSmgMagazineLoadIndex = -1;
        private int lastSmgReloadFinishIndex = -1;
        private int lastSgShotIndex = -1;
        private int lastSgMagazineUnloadIndex = -1;
        private int lastSgMagazineLoadIndex = -1;
        private int lastSgReloadFinishIndex = -1;
        private int lastArShotIndex = -1;
        private int lastArMagazineUnloadIndex = -1;
        private int lastArMagazineLoadIndex = -1;
        private int lastArReloadFinishIndex = -1;
        private int lastMgShotIndex = -1;
        private int lastMgReloadIndex = -1;
        private float lastSmgShotTime = -999f;
        private float lastMgShotTime = -999f;

        private void Awake()
        {
            CacheAudioSources();

#if UNITY_EDITOR
            AutoAssignClipsIfNeeded();
#endif
        }

        private void Reset()
        {
            CacheAudioSources();

#if UNITY_EDITOR
            AutoAssignClipsIfNeeded();
#endif
        }

        private void OnDisable()
        {
            StopAllSounds();
        }

        private void OnValidate()
        {
            shotVolume = Mathf.Clamp01(shotVolume);
            shotTailVolume = Mathf.Clamp01(shotTailVolume);
            reloadVolume = Mathf.Clamp01(reloadVolume);
            spatialBlend = Mathf.Clamp01(spatialBlend);
            removeMagazineNormalizedTime = Mathf.Clamp01(removeMagazineNormalizedTime);
            insertMagazineNormalizedTime = Mathf.Clamp01(insertMagazineNormalizedTime);
            finishReloadNormalizedTime = Mathf.Clamp01(finishReloadNormalizedTime);
            minimumReloadSequenceDuration = Mathf.Max(minimumReloadSequenceDuration, 0.01f);
            smgFirstShotResetTime = Mathf.Max(smgFirstShotResetTime, 0f);
            smgShotSequenceResetMultiplier = Mathf.Max(smgShotSequenceResetMultiplier, 1f);
            smgMagazineUnloadNormalizedTime = Mathf.Clamp01(smgMagazineUnloadNormalizedTime);
            smgMagazineLoadNormalizedTime = Mathf.Clamp01(smgMagazineLoadNormalizedTime);
            smgFinishReloadNormalizedTime = Mathf.Clamp01(smgFinishReloadNormalizedTime);
            sgMagazineUnloadNormalizedTime = Mathf.Clamp01(sgMagazineUnloadNormalizedTime);
            sgMagazineLoadNormalizedTime = Mathf.Clamp01(sgMagazineLoadNormalizedTime);
            sgFinishReloadNormalizedTime = Mathf.Clamp01(sgFinishReloadNormalizedTime);
            arMagazineUnloadNormalizedTime = Mathf.Clamp01(arMagazineUnloadNormalizedTime);
            arMagazineLoadNormalizedTime = Mathf.Clamp01(arMagazineLoadNormalizedTime);
            arFinishReloadNormalizedTime = Mathf.Clamp01(arFinishReloadNormalizedTime);
            mgFirstShotResetTime = Mathf.Max(mgFirstShotResetTime, 0f);
            mgShotSequenceResetMultiplier = Mathf.Max(mgShotSequenceResetMultiplier, 1f);

            ConfigureAudioSource(shotAudioSource);
            ConfigureAudioSource(shotTailAudioSource);
            ConfigureAudioSource(reloadAudioSource);
        }

        public void PlayShot(WeaponData weaponData)
        {
            if (IsPistol(weaponData))
            {
                PlayPistolShot();
                return;
            }

            if (IsSmg(weaponData))
            {
                PlaySmgShot(weaponData);
                return;
            }

            if (IsSg(weaponData))
            {
                PlaySgShot();
                return;
            }

            if (IsAr(weaponData))
            {
                PlayArShot();
                return;
            }

            if (IsMg(weaponData))
                PlayMgShot(weaponData);
        }

        public void PlayReload(WeaponData weaponData, float reloadDuration)
        {
            CacheAudioSources();
            StopReload();

            if (IsPistol(weaponData))
            {
                reloadCoroutine = StartCoroutine(PistolReloadRoutine(reloadDuration));
                return;
            }

            if (IsSmg(weaponData))
            {
                reloadCoroutine = StartCoroutine(SmgReloadRoutine(reloadDuration));
                return;
            }

            if (IsSg(weaponData))
            {
                reloadCoroutine = StartCoroutine(SgReloadRoutine(reloadDuration));
                return;
            }

            if (IsAr(weaponData))
            {
                reloadCoroutine = StartCoroutine(ArReloadRoutine(reloadDuration));
                return;
            }

            if (IsMg(weaponData))
                PlayMgReload();
        }

        public void StopReload()
        {
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }

            if (reloadAudioSource != null)
                reloadAudioSource.Stop();
        }

        public void StopAllSounds()
        {
            StopReload();

            if (shotTailAudioSource != null)
                shotTailAudioSource.Stop();
        }

        public void PlayPistolReloadRemoveMagazine()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, pistolRemoveMagazineClips, ref lastPistolRemoveMagazineIndex, reloadVolume);
        }

        public void PlayPistolReloadInsertMagazine()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, pistolInsertMagazineClips, ref lastPistolInsertMagazineIndex, reloadVolume);
        }

        public void PlayPistolReloadFinish()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, pistolReloadFinishClips, ref lastPistolReloadFinishIndex, reloadVolume);
        }

        public void PlaySmgReloadMagazineUnload()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, smgMagazineUnloadClips, ref lastSmgMagazineUnloadIndex, reloadVolume);
        }

        public void PlaySmgReloadMagazineLoad()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, smgMagazineLoadClips, ref lastSmgMagazineLoadIndex, reloadVolume);
        }

        public void PlaySmgReloadFinish()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, smgReloadFinishClips, ref lastSmgReloadFinishIndex, reloadVolume);
        }

        public void PlaySgReloadMagazineUnload()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, sgMagazineUnloadClips, ref lastSgMagazineUnloadIndex, reloadVolume);
        }

        public void PlaySgReloadMagazineLoad()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, sgMagazineLoadClips, ref lastSgMagazineLoadIndex, reloadVolume);
        }

        public void PlaySgReloadFinish()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, sgReloadFinishClips, ref lastSgReloadFinishIndex, reloadVolume);
        }

        public void PlayArReloadMagazineUnload()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, arMagazineUnloadClips, ref lastArMagazineUnloadIndex, reloadVolume);
        }

        public void PlayArReloadMagazineLoad()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, arMagazineLoadClips, ref lastArMagazineLoadIndex, reloadVolume);
        }

        public void PlayArReloadFinish()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, arReloadFinishClips, ref lastArReloadFinishIndex, reloadVolume);
        }

        public void PlayMgReload()
        {
            CacheAudioSources();
            PlayRandomOneShot(reloadAudioSource, mgReloadClips, ref lastMgReloadIndex, reloadVolume);
        }

        private void PlayPistolShot()
        {
            CacheAudioSources();
            PlayRandomOneShot(shotAudioSource, pistolShotClips, ref lastPistolShotIndex, shotVolume);
        }

        private void PlaySmgShot(WeaponData weaponData)
        {
            CacheAudioSources();

            if (ShouldPlaySmgFirstShot(weaponData) && smgFirstShotClip != null)
                shotAudioSource.PlayOneShot(smgFirstShotClip, shotVolume);
            else
                PlayRandomOneShot(shotAudioSource, smgShotClips, ref lastSmgShotIndex, shotVolume);

            lastSmgShotTime = Time.time;
            PlaySmgTail();
        }

        private void PlaySgShot()
        {
            CacheAudioSources();
            PlayRandomOneShot(shotAudioSource, sgShotClips, ref lastSgShotIndex, shotVolume);
        }

        private void PlayArShot()
        {
            CacheAudioSources();
            PlayRandomOneShot(shotAudioSource, arShotClips, ref lastArShotIndex, shotVolume);
        }

        private void PlayMgShot(WeaponData weaponData)
        {
            CacheAudioSources();

            if (ShouldPlayMgFirstShot(weaponData) && mgFirstShotClip != null)
                shotAudioSource.PlayOneShot(mgFirstShotClip, shotVolume);
            else
                PlayRandomOneShot(shotAudioSource, mgShotClips, ref lastMgShotIndex, shotVolume);

            lastMgShotTime = Time.time;
            PlayMgTail();
        }

        private bool ShouldPlaySmgFirstShot(WeaponData weaponData)
        {
            float shotDelay = weaponData != null && weaponData.RPM > 0f ? 60f / weaponData.RPM : 0f;
            float resetTime = Mathf.Max(smgFirstShotResetTime, shotDelay * smgShotSequenceResetMultiplier);

            return Time.time - lastSmgShotTime > resetTime;
        }

        private bool ShouldPlayMgFirstShot(WeaponData weaponData)
        {
            float shotDelay = weaponData != null && weaponData.RPM > 0f ? 60f / weaponData.RPM : 0f;
            float resetTime = Mathf.Max(mgFirstShotResetTime, shotDelay * mgShotSequenceResetMultiplier);

            return Time.time - lastMgShotTime > resetTime;
        }

        private void PlaySmgTail()
        {
            if (shotTailAudioSource == null || smgTailOnlyClip == null)
                return;

            // Tail은 겹쳐 재생하지 않고 재시작해서 연사 중 소리가 뭉개지는 것을 줄인다.
            shotTailAudioSource.Stop();
            shotTailAudioSource.clip = smgTailOnlyClip;
            shotTailAudioSource.volume = shotTailVolume;
            shotTailAudioSource.Play();
        }

        private void PlayMgTail()
        {
            if (shotTailAudioSource == null || mgTailOnlyClip == null)
                return;

            shotTailAudioSource.Stop();
            shotTailAudioSource.clip = mgTailOnlyClip;
            shotTailAudioSource.volume = shotTailVolume;
            shotTailAudioSource.Play();
        }

        private IEnumerator PistolReloadRoutine(float reloadDuration)
        {
            float sequenceDuration = Mathf.Max(reloadDuration, minimumReloadSequenceDuration);
            float elapsed = 0f;

            float removeTime = GetStageTime(removeMagazineNormalizedTime, sequenceDuration);
            if (removeTime > elapsed)
            {
                yield return new WaitForSeconds(removeTime - elapsed);
                elapsed = removeTime;
            }

            PlayPistolReloadRemoveMagazine();

            float insertTime = GetStageTime(insertMagazineNormalizedTime, sequenceDuration);
            if (insertTime > elapsed)
            {
                yield return new WaitForSeconds(insertTime - elapsed);
                elapsed = insertTime;
            }

            PlayPistolReloadInsertMagazine();

            float finishTime = GetStageTime(finishReloadNormalizedTime, sequenceDuration);
            if (finishTime > elapsed)
            {
                yield return new WaitForSeconds(finishTime - elapsed);
            }

            PlayPistolReloadFinish();
            reloadCoroutine = null;
        }

        private IEnumerator SmgReloadRoutine(float reloadDuration)
        {
            float sequenceDuration = Mathf.Max(reloadDuration, minimumReloadSequenceDuration);
            float elapsed = 0f;

            float unloadTime = GetStageTime(smgMagazineUnloadNormalizedTime, sequenceDuration);
            if (unloadTime > elapsed)
            {
                yield return new WaitForSeconds(unloadTime - elapsed);
                elapsed = unloadTime;
            }

            PlaySmgReloadMagazineUnload();

            float loadTime = GetStageTime(smgMagazineLoadNormalizedTime, sequenceDuration);
            if (loadTime > elapsed)
            {
                yield return new WaitForSeconds(loadTime - elapsed);
                elapsed = loadTime;
            }

            PlaySmgReloadMagazineLoad();

            float finishTime = GetStageTime(smgFinishReloadNormalizedTime, sequenceDuration);
            if (finishTime > elapsed)
            {
                yield return new WaitForSeconds(finishTime - elapsed);
            }

            PlaySmgReloadFinish();
            reloadCoroutine = null;
        }

        private IEnumerator SgReloadRoutine(float reloadDuration)
        {
            float sequenceDuration = Mathf.Max(reloadDuration, minimumReloadSequenceDuration);
            float elapsed = 0f;

            float unloadTime = GetStageTime(sgMagazineUnloadNormalizedTime, sequenceDuration);
            if (unloadTime > elapsed)
            {
                yield return new WaitForSeconds(unloadTime - elapsed);
                elapsed = unloadTime;
            }

            PlaySgReloadMagazineUnload();

            float loadTime = GetStageTime(sgMagazineLoadNormalizedTime, sequenceDuration);
            if (loadTime > elapsed)
            {
                yield return new WaitForSeconds(loadTime - elapsed);
                elapsed = loadTime;
            }

            PlaySgReloadMagazineLoad();

            float finishTime = GetStageTime(sgFinishReloadNormalizedTime, sequenceDuration);
            if (finishTime > elapsed)
            {
                yield return new WaitForSeconds(finishTime - elapsed);
            }

            PlaySgReloadFinish();
            reloadCoroutine = null;
        }

        private IEnumerator ArReloadRoutine(float reloadDuration)
        {
            float sequenceDuration = Mathf.Max(reloadDuration, minimumReloadSequenceDuration);
            float elapsed = 0f;

            float unloadTime = GetStageTime(arMagazineUnloadNormalizedTime, sequenceDuration);
            if (unloadTime > elapsed)
            {
                yield return new WaitForSeconds(unloadTime - elapsed);
                elapsed = unloadTime;
            }

            PlayArReloadMagazineUnload();

            float loadTime = GetStageTime(arMagazineLoadNormalizedTime, sequenceDuration);
            if (loadTime > elapsed)
            {
                yield return new WaitForSeconds(loadTime - elapsed);
                elapsed = loadTime;
            }

            PlayArReloadMagazineLoad();

            float finishTime = GetStageTime(arFinishReloadNormalizedTime, sequenceDuration);
            if (finishTime > elapsed)
            {
                yield return new WaitForSeconds(finishTime - elapsed);
            }

            PlayArReloadFinish();
            reloadCoroutine = null;
        }

        private float GetStageTime(float normalizedTime, float sequenceDuration)
        {
            return Mathf.Clamp01(normalizedTime) * Mathf.Max(sequenceDuration, minimumReloadSequenceDuration);
        }

        private bool IsPistol(WeaponData weaponData)
        {
            return weaponData != null && weaponData.WeaponType == WeaponClass.Pistol;
        }

        private bool IsSmg(WeaponData weaponData)
        {
            return weaponData != null && weaponData.WeaponType == WeaponClass.SMG;
        }

        private bool IsSg(WeaponData weaponData)
        {
            return weaponData != null && weaponData.WeaponType == WeaponClass.SG;
        }

        private bool IsAr(WeaponData weaponData)
        {
            return weaponData != null && weaponData.WeaponType == WeaponClass.AR;
        }

        private bool IsMg(WeaponData weaponData)
        {
            return weaponData != null && weaponData.WeaponType == WeaponClass.MG;
        }

        private void CacheAudioSources()
        {
            if (shotAudioSource == null)
                shotAudioSource = GetComponent<AudioSource>();

            if (shotAudioSource == null)
                shotAudioSource = gameObject.AddComponent<AudioSource>();

            if (shotTailAudioSource == null)
            {
                AudioSource[] audioSources = GetComponents<AudioSource>();
                shotTailAudioSource = FindReusableAudioSource(audioSources, shotAudioSource, reloadAudioSource);
            }

            if (shotTailAudioSource == null)
                shotTailAudioSource = gameObject.AddComponent<AudioSource>();

            if (reloadAudioSource == null)
            {
                AudioSource[] audioSources = GetComponents<AudioSource>();
                reloadAudioSource = FindReusableAudioSource(audioSources, shotAudioSource, shotTailAudioSource);
            }

            if (reloadAudioSource == null)
                reloadAudioSource = gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(shotAudioSource);
            ConfigureAudioSource(shotTailAudioSource);
            ConfigureAudioSource(reloadAudioSource);
        }

        private AudioSource FindReusableAudioSource(AudioSource[] audioSources, params AudioSource[] excludedAudioSources)
        {
            if (audioSources == null)
                return null;

            for (int i = 0; i < audioSources.Length; i++)
            {
                AudioSource audioSource = audioSources[i];
                if (audioSource == null)
                    continue;

                bool isExcluded = false;
                for (int j = 0; j < excludedAudioSources.Length; j++)
                {
                    if (audioSource != excludedAudioSources[j])
                        continue;

                    isExcluded = true;
                    break;
                }

                if (!isExcluded)
                    return audioSource;
            }

            return null;
        }

        private void ConfigureAudioSource(AudioSource audioSource)
        {
            if (audioSource == null)
                return;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = spatialBlend;
        }

        private void PlayRandomOneShot(AudioSource audioSource, AudioClip[] clips, ref int lastIndex, float volume)
        {
            if (audioSource == null)
                return;

            AudioClip clip = GetRandomClip(clips, ref lastIndex);
            if (clip == null)
                return;

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
        {
            int validClipCount = GetValidClipCount(clips);
            if (validClipCount <= 0)
                return null;

            for (int i = 0; i < 8; i++)
            {
                int candidateIndex = Random.Range(0, clips.Length);
                AudioClip candidateClip = clips[candidateIndex];

                if (candidateClip == null)
                    continue;

                if (validClipCount > 1 && candidateIndex == lastIndex)
                    continue;

                lastIndex = candidateIndex;
                return candidateClip;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                lastIndex = i;
                return clips[i];
            }

            return null;
        }

        private int GetValidClipCount(AudioClip[] clips)
        {
            if (clips == null)
                return 0;

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            return count;
        }

        private bool HasAnyClip(AudioClip[] clips)
        {
            return GetValidClipCount(clips) > 0;
        }

#if UNITY_EDITOR
        private void AutoAssignClipsIfNeeded()
        {
            if (!autoAssignClipsInEditor)
                return;

            pistolShotClips = EnsureClips(pistolShotClips, "gun_pistol_shot_01", "gun_pistol_shot_02", "gun_pistol_shot_03", "gun_pistol_shot_04", "gun_pistol_shot_05");
            pistolRemoveMagazineClips = EnsureClips(pistolRemoveMagazineClips, "gun_pistol_remove_mag_01", "gun_pistol_remove_mag_02", "gun_pistol_remove_mag_03");
            pistolInsertMagazineClips = EnsureClips(pistolInsertMagazineClips, "gun_pistol_insert_mag_01", "gun_pistol_insert_mag_02", "gun_pistol_insert_mag_03");
            pistolReloadFinishClips = EnsureClips(pistolReloadFinishClips, "gun_pistol_slide_fast_01", "gun_pistol_slide_fast_02", "gun_pistol_slide_fast_03");

            if (smgFirstShotClip == null)
                smgFirstShotClip = LoadClip("gun_submachine_auto_shot_00_first_01");

            smgShotClips = EnsureClips(smgShotClips, "gun_submachine_auto_shot_01", "gun_submachine_auto_shot_02", "gun_submachine_auto_shot_03", "gun_submachine_auto_shot_04", "gun_submachine_auto_shot_05", "gun_submachine_auto_shot_06", "gun_submachine_auto_shot_07", "gun_submachine_auto_shot_08", "gun_submachine_auto_shot_09");

            if (smgTailOnlyClip == null)
                smgTailOnlyClip = LoadClip("gun_submachine_auto_shot_00_tail_only_01");

            smgMagazineUnloadClips = EnsureClips(smgMagazineUnloadClips, "gun_submachine_auto_magazine_unload_01", "gun_submachine_auto_magazine_unload_02", "gun_submachine_auto_magazine_unload_03");
            smgMagazineLoadClips = EnsureClips(smgMagazineLoadClips, "gun_submachine_auto_magazine_load_01", "gun_submachine_auto_magazine_load_02", "gun_submachine_auto_magazine_load_03", "gun_submachine_auto_magazine_load_04");
            smgReloadFinishClips = EnsureClips(smgReloadFinishClips, "gun_submachine_auto_cock_01", "gun_submachine_auto_cock_02", "gun_submachine_auto_cock_03", "gun_submachine_auto_cock_04");

            sgShotClips = EnsureClips(sgShotClips, "gun_shotgun_shot_01", "gun_shotgun_shot_02", "gun_shotgun_shot_03", "gun_shotgun_shot_04");
            sgMagazineUnloadClips = EnsureClips(sgMagazineUnloadClips, "gun_pistol_remove_mag_04", "gun_pistol_remove_mag_05", "gun_pistol_remove_mag_06");
            sgMagazineLoadClips = EnsureClips(sgMagazineLoadClips, "gun_pistol_insert_mag_04", "gun_pistol_insert_mag_05");
            sgReloadFinishClips = EnsureClips(sgReloadFinishClips, "gun_pistol_slide_fast_04", "gun_pistol_slide_fast_05", "gun_pistol_slide_fast_06");

            arShotClips = EnsureClips(arShotClips, "gun_rifle_shot_01", "gun_rifle_shot_02", "gun_rifle_shot_03", "gun_rifle_shot_04");
            arMagazineUnloadClips = EnsureClips(arMagazineUnloadClips, "gun_rifle_magazine_unload_01", "gun_rifle_magazine_unload_02", "gun_rifle_magazine_unload_03", "gun_rifle_magazine_unload_04", "gun_rifle_magazine_unload_05");
            arMagazineLoadClips = EnsureClips(arMagazineLoadClips, "gun_rifle_magazine_load_01", "gun_rifle_magazine_load_02", "gun_rifle_magazine_load_03", "gun_rifle_magazine_load_04");
            arReloadFinishClips = EnsureClips(arReloadFinishClips, "gun_rifle_cock_01", "gun_rifle_cock_02", "gun_rifle_cock_03", "gun_rifle_cock_04");

            if (mgFirstShotClip == null)
                mgFirstShotClip = LoadClip("gun_machinegun_auto_heavy_shot_00_first_01");

            mgShotClips = EnsureClips(mgShotClips, "gun_machinegun_auto_heavy_shot_01", "gun_machinegun_auto_heavy_shot_02", "gun_machinegun_auto_heavy_shot_03", "gun_machinegun_auto_heavy_shot_04", "gun_machinegun_auto_heavy_shot_05", "gun_machinegun_auto_heavy_shot_06", "gun_machinegun_auto_heavy_shot_07", "gun_machinegun_auto_heavy_shot_08");

            if (mgTailOnlyClip == null)
                mgTailOnlyClip = LoadClip("gun_machinegun_auto_heavy_shot_00_tail_only_01");

            mgReloadClips = EnsureClips(mgReloadClips, "gun_machinegun_auto_heavy_reload_01", "gun_machinegun_auto_heavy_reload_02");
        }

        private AudioClip[] EnsureClips(AudioClip[] currentClips, params string[] clipNames)
        {
            if (HasExactClips(currentClips, clipNames))
                return currentClips;

            return LoadClips(clipNames);
        }

        private bool HasExactClips(AudioClip[] clips, string[] clipNames)
        {
            if (clips == null || clipNames == null)
                return false;

            if (clips.Length != clipNames.Length)
                return false;

            for (int i = 0; i < clipNames.Length; i++)
            {
                if (clips[i] == null)
                    return false;

                if (clips[i].name != clipNames[i])
                    return false;
            }

            return true;
        }

        private AudioClip LoadClip(string clipName)
        {
            string assetPath = $"{ClipFolder}/{clipName}.wav";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        private AudioClip[] LoadClips(params string[] clipNames)
        {
            AudioClip[] clips = new AudioClip[clipNames.Length];

            for (int i = 0; i < clipNames.Length; i++)
            {
                clips[i] = LoadClip(clipNames[i]);
            }

            return clips;
        }
#endif
    }
}
