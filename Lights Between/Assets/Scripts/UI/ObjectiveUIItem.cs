using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ObjectiveUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip objectiveSFX;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private float animateInDuration = 0.5f;
    [SerializeField] private float displayDuration = 3.0f;
    [SerializeField] private float animateOutDuration = 0.5f;

    private static readonly int ShowTrigger = Animator.StringToHash("Show");
    private static readonly int HideTrigger = Animator.StringToHash("Hide");

    private CanvasGroup canvasGroup;
    private Color originalColor;
    private Coroutine currentSequence;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (titleText != null)
        {
            originalColor = titleText.color;
        }

        canvasGroup.alpha = 0f;
    }

    public void Setup(ObjectiveSO objectiveDef)
    {
        if (objectiveDef == null) return;

        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        if (titleText != null)
        {
            titleText.text = objectiveDef.Title;
            titleText.color = originalColor;
        }

        ResetAnimation();

        currentSequence = StartCoroutine(IntroduceRoutine());
    }

    public void SetCompleted(bool isCompleted)
    {
        if (!isCompleted) return;

        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        ResetAnimation();

        currentSequence = StartCoroutine(CompleteRoutine());
    }

    private void ResetAnimation()
    {
        canvasGroup.alpha = 0f;

        if (animator == null) return;

        animator.ResetTrigger(ShowTrigger);
        animator.ResetTrigger(HideTrigger);

        // Return to the normal visible position before playing the entrance.
        animator.Play("Idle", 0, 0f);
    }

    private void PlaySFX()
    {
        if (objectiveSFX == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(objectiveSFX);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                objectiveSFX,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero
            );
        }
    }

    private IEnumerator IntroduceRoutine()
    {
        TriggerAnimator(ShowTrigger);
        PlaySFX();

        yield return StartCoroutine(
            FadeCanvasGroup(0f, 1f, animateInDuration)
        );

        yield return new WaitForSeconds(displayDuration);

        // Play the slide-out animation.
        TriggerAnimator(HideTrigger);

        yield return StartCoroutine(
            FadeCanvasGroup(1f, 0f, animateOutDuration)
        );

        currentSequence = null;
    }

    private IEnumerator CompleteRoutine()
    {
        if (titleText != null)
        {
            titleText.color = completedColor;
        }

        PlaySFX();

        TriggerAnimator(ShowTrigger);

        yield return StartCoroutine(
            FadeCanvasGroup(0f, 1f, animateInDuration)
        );

        yield return new WaitForSeconds(displayDuration);

        TriggerAnimator(HideTrigger);

        yield return StartCoroutine(
            FadeCanvasGroup(1f, 0f, animateOutDuration)
        );

        if (titleText != null)
        {
            titleText.color = originalColor;
        }

        currentSequence = null;
    }

    private IEnumerator FadeCanvasGroup(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        float elapsedTime = 0f;

        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                t
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void TriggerAnimator(int triggerHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerHash);
        }
    }
}