using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class HitSoundController : MonoBehaviour
    {
        [Header("Hit Filter")]
        [SerializeField] private LayerMask hitLayerMask;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] hitClips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0f)] private float minPlayInterval = 0.05f;
        [SerializeField] private bool randomizeClip = true;
        [SerializeField] private bool randomizePitch;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        private int lastClipIndex = -1;
        private float lastPlayTime = -999f;
        private bool missingClipLogged;
        private bool missingAudioSourceLogged;

        private void Reset()
        {
            hitLayerMask = LayerMask.GetMask("Enemy");
            CacheAudioSource(true);
        }

        private void Awake()
        {
            CacheAudioSource(true);
        }

        private void OnEnable()
        {
            CacheAudioSource(true);
            HitFeedbackEvents.Hit += OnHit;
        }

        private void OnDisable()
        {
            HitFeedbackEvents.Hit -= OnHit;
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            minPlayInterval = Mathf.Max(minPlayInterval, 0f);

            if (pitchRange.x > pitchRange.y)
            {
                float minPitch = pitchRange.y;
                pitchRange.y = pitchRange.x;
                pitchRange.x = minPitch;
            }

            CacheAudioSource(false);
        }

        private void OnHit(HitFeedbackEventData hitData)
        {
            if (!hitData.IsInLayerMask(hitLayerMask))
                return;

            if (Time.unscaledTime < lastPlayTime + minPlayInterval)
                return;

            AudioClip clip = GetClip();
            if (clip == null)
            {
                ReportMissingClip();
                return;
            }

            CacheAudioSource(true);
            if (audioSource == null)
                return;

            audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
            audioSource.PlayOneShot(clip, volume);
            lastPlayTime = Time.unscaledTime;
        }

        private AudioClip GetClip()
        {
            if (hitClips == null || hitClips.Length == 0)
                return null;

            if (!randomizeClip || hitClips.Length == 1)
                return hitClips[0];

            int nextIndex = Random.Range(0, hitClips.Length);
            if (hitClips.Length > 1 && nextIndex == lastClipIndex)
                nextIndex = (nextIndex + 1) % hitClips.Length;

            lastClipIndex = nextIndex;
            return hitClips[nextIndex];
        }

        private void CacheAudioSource(bool logIfMissing)
        {
            if (audioSource == null)
                TryGetComponent(out audioSource);

            if (audioSource == null)
                audioSource = GetComponentInChildren<AudioSource>(true);

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>();

            if (audioSource == null)
            {
                if (logIfMissing)
                    ReportMissingAudioSource();

                return;
            }

            audioSource.playOnAwake = false;
        }

        private void ReportMissingAudioSource()
        {
            if (missingAudioSourceLogged)
                return;

            Debug.LogWarning("[HitSoundController] AudioSource를 찾을 수 없습니다. Player_Soldier/Components/Audio 오브젝트에 AudioSource를 추가하거나 Inspector에 직접 연결해주세요.", this);
            missingAudioSourceLogged = true;
        }

        private void ReportMissingClip()
        {
            if (missingClipLogged)
                return;

            Debug.LogWarning("[HitSoundController] hitClips가 비어 있어 Hit Sound를 재생할 수 없습니다. Inspector에 AudioClip을 연결해주세요.", this);
            missingClipLogged = true;
        }
    }
}
