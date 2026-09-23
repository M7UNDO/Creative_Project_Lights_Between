using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToBeContinuedSequence : MonoBehaviour
{
    [Header("UI Canvas & Timing")]
    [SerializeField] private CanvasGroup toBeContinuedCanvasGroup;
    [SerializeField] private float delayBeforeScreen = 3.0f;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Optional Scene Loading")]
    [SerializeField] private bool loadMainMenuAfterSequence = false;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float displayDuration = 4.0f;

    private FPSController fpsController;

    private void Awake()
    {
        fpsController = FindFirstObjectByType<FPSController>();

        if (toBeContinuedCanvasGroup != null)
        {
            toBeContinuedCanvasGroup.alpha = 0f;
            toBeContinuedCanvasGroup.gameObject.SetActive(false);
        }
    }

    public void StartSequence()
    {
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        yield return new WaitForSeconds(delayBeforeScreen);

        Time.timeScale = 0f;

        if (fpsController != null)
        {
            fpsController.SetMiniGameMode(true);
        }

        if (toBeContinuedCanvasGroup != null)
        {
            toBeContinuedCanvasGroup.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                toBeContinuedCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            toBeContinuedCanvasGroup.alpha = 1f;
        }

        if (loadMainMenuAfterSequence)
        {
            yield return new WaitForSecondsRealtime(displayDuration);
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}