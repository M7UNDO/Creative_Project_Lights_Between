using System.Collections;
using TMPro;
using UnityEngine;

public class IntroSequenceController : MonoBehaviour
{
    [Header("Header UI Setup")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private CanvasGroup headerCanvasGroup;

    [Header("Header Content")]
    [SerializeField] private string dateText = "11 August 2004";
    [SerializeField] private string locationText = "Rural Countryside, Oakhaven";
    [SerializeField] private string propertyText = "The Morrow Farmhouse";

    [Header("Header Timing & Fading")]
    [SerializeField] private float headerFadeDuration = 1.0f;
    [SerializeField] private float headerHoldDuration = 2.0f;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueSO introDialogueSO;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip introMusicClip;
    [SerializeField] private float musicFadeInDuration = 1.5f;

    [Header("Level Transition")]
    [SerializeField] private AsyncLoader asyncLoader;
    [SerializeField] private int targetSceneIndex = 1;

    private void Start()
    {
        InitializeUI();
        StartCoroutine(PlaySequenceRoutine());
    }

    private void InitializeUI()
    {
        if (headerText != null)
        {
            headerText.text = string.Empty;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator PlaySequenceRoutine()
    {
        yield return DisplayHeaderRoutine(dateText);
        yield return DisplayHeaderRoutine(locationText);
        yield return DisplayHeaderRoutine(propertyText);

        PlayIntroMusic();

        bool isDialogueFinished = false;

        if (DialogueManager.Instance != null && introDialogueSO != null)
        {
            DialogueManager.Instance.StartDialogue(introDialogueSO);

            void HandleDialogueEnded()
            {
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
                isDialogueFinished = true;
            }

            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            yield return new WaitUntil(() => isDialogueFinished);
        }

        TransitionToGameLevel();
    }

    private IEnumerator DisplayHeaderRoutine(string content)
    {
        if (headerText == null || headerCanvasGroup == null) yield break;

        headerText.text = content;

        yield return FadeCanvasGroup(headerCanvasGroup, 0f, 1f, headerFadeDuration);
        yield return new WaitForSeconds(headerHoldDuration);
        yield return FadeCanvasGroup(headerCanvasGroup, 1f, 0f, headerFadeDuration);

        headerText.text = string.Empty;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null) yield break;

        float timer = 0f;
        group.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    private void PlayIntroMusic()
    {
        if (audioSource == null || introMusicClip == null) return;

        audioSource.clip = introMusicClip;
        audioSource.volume = 0f;
        audioSource.Play();

        StartCoroutine(FadeAudioRoutine(audioSource, 1f, musicFadeInDuration));
    }

    private IEnumerator FadeAudioRoutine(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    private void TransitionToGameLevel()
    {
        if (asyncLoader != null)
        {
            asyncLoader.LoadLevelButton(targetSceneIndex);
        }
        else
        {
            Debug.LogError("[IntroSequenceController] AsyncLoader reference is missing.");
        }
    }
}