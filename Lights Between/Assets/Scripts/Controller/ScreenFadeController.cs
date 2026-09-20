using System;
using System.Collections;
using UnityEngine;

public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void DoBlinkTransition(Action onBlackoutAction, float duration = -1f)
    {
        StartCoroutine(BlinkRoutine(onBlackoutAction, duration > 0 ? duration : defaultFadeDuration));
    }

    private IEnumerator BlinkRoutine(Action onBlackoutAction, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            onBlackoutAction?.Invoke();
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;

        // Fade to Black
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Execute teleportation or state change while dark
        onBlackoutAction?.Invoke();

        // Hold briefly at full black
        yield return new WaitForSeconds(0.1f);

        // Fade to Clear
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}