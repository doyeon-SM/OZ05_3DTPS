using System.Collections;
using _01.Scenes.PhaseValidation._26._05._14;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSpedex
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Image gameOverImage;
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;

        [Header("Timing")]
        [SerializeField] private float deathAnimationDelayTime = 1.5f;
        [SerializeField] private float fadeInTime = 1f;

        private Coroutine showRoutine;

        private void Awake()
        {
            CacheReferences();
            HideImmediately();
        }

        public void Show()
        {
            if (showRoutine != null)
                return;

            gameObject.SetActive(true);
            CacheReferences();
            showRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            HideImmediately();

            float delay = Mathf.Max(deathAnimationDelayTime, 0f);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (gameOverImage != null)
            {
                gameOverImage.gameObject.SetActive(true);
                yield return FadeInImage();
            }

            if (returnToLobbyButton != null)
                returnToLobbyButton.gameObject.SetActive(true);

            UnlockCursorForGameOver();
            showRoutine = null;
        }

        private IEnumerator FadeInImage()
        {
            float duration = Mathf.Max(fadeInTime, 0f);
            if (duration <= 0f)
            {
                SetImageAlpha(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetImageAlpha(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            SetImageAlpha(1f);
        }

        private void HideImmediately()
        {
            if (gameOverImage != null)
            {
                SetImageAlpha(0f);
                gameOverImage.gameObject.SetActive(false);
            }

            if (returnToLobbyButton != null)
                returnToLobbyButton.gameObject.SetActive(false);
        }

        private void SetImageAlpha(float alpha)
        {
            if (gameOverImage == null)
                return;

            Color color = gameOverImage.color;
            color.a = Mathf.Clamp01(alpha);
            gameOverImage.color = color;
        }

        private void UnlockCursorForGameOver()
        {
            if (starterAssetsInputs == null)
                starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>(FindObjectsInactive.Include);

            if (starterAssetsInputs != null)
                starterAssetsInputs.cursorLocked = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CacheReferences()
        {
            if (gameOverImage == null)
            {
                Transform imageTransform = transform.Find("Image");
                if (imageTransform != null)
                    gameOverImage = imageTransform.GetComponent<Image>();
            }

            if (gameOverImage == null)
                gameOverImage = FindGameOverImage();

            if (returnToLobbyButton == null)
            {
                Transform buttonTransform = transform.Find("LobyButton");
                if (buttonTransform != null)
                    returnToLobbyButton = buttonTransform.GetComponent<Button>();
            }

            if (returnToLobbyButton == null)
                returnToLobbyButton = GetComponentInChildren<Button>(true);

            if (starterAssetsInputs == null)
                starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>(FindObjectsInactive.Include);
        }

        private Image FindGameOverImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].GetComponent<Button>() == null)
                    return images[i];
            }

            return null;
        }
    }
}
