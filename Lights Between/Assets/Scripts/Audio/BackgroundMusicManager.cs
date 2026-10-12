using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MusicState
{
    CalmAmbience,
    Tension,
    DoorKnockTension,
    HorrorStinger,
    Riser,
    Action
}

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [System.Serializable]
    public struct MusicTrack
    {
        public MusicState state;
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume;
    }

    [Header("Audio Sources (Dual Setup)")]
    [SerializeField] private AudioSource audioSourceA;
    [SerializeField] private AudioSource audioSourceB;

    [Header("Pause Music Settings")]
    [SerializeField] private AudioClip pauseMenuClip;
    [Range(0f, 1f)][SerializeField] private float pauseMenuBaseVolume = 1f;

    [Header("Track Library")]
    [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();

    [Header("Global Crossfade Settings")]
    [SerializeField] private float defaultFadeDuration = 1.5f;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine fadeCoroutine;
    private MusicState currentMusicState;

    private AudioSource pausedGameplaySource;
    private float pausedGameplayTime;
    private bool isPaused;

    private Dictionary<MusicState, MusicTrack> trackLookup = new Dictionary<MusicState, MusicTrack>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSources();
        BuildLookupDictionary();
    }

    public void Start()
    {
        TransitionToState(MusicState.CalmAmbience, 1f);
    }

    private void InitializeSources()
    {
        if (audioSourceA == null) audioSourceA = gameObject.AddComponent<AudioSource>();
        if (audioSourceB == null) audioSourceB = gameObject.AddComponent<AudioSource>();

        audioSourceA.loop = true;
        audioSourceB.loop = true;

        audioSourceA.playOnAwake = false;
        audioSourceB.playOnAwake = false;

        activeSource = audioSourceA;
        inactiveSource = audioSourceB;
    }

    public void SetStateCalmAmbience() => TransitionToState(MusicState.CalmAmbience);
    public void SetStateTension() => TransitionToState(MusicState.Tension);
    public void SetStateDoorKnockTension() => TransitionToState(MusicState.DoorKnockTension);
    public void SetStateHorrorStinger() => TransitionToState(MusicState.HorrorStinger);
    public void SetStateRiser() => TransitionToState(MusicState.Riser);
    public void SetStateAction() => TransitionToState(MusicState.Action);

    public void SetStateByName(string stateName)
    {
        if (System.Enum.TryParse(stateName, out MusicState parsedState))
        {
            TransitionToState(parsedState);
        }
        else
        {
            Debug.LogWarning($"[MusicManager] Invalid state name: {stateName}");
        }
    }

    private void BuildLookupDictionary()
    {
        trackLookup.Clear();
        foreach (var track in musicTracks)
        {
            if (!trackLookup.ContainsKey(track.state))
            {
                trackLookup.Add(track.state, track);
            }
        }
    }

    public void PauseGameMusic(float fadeDuration = 0.5f)
    {
        if (isPaused) return;
        isPaused = true;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (pauseMenuClip != null)
        {
            pausedGameplaySource = activeSource;

            if (pausedGameplaySource != null && pausedGameplaySource.clip != null)
            {
                pausedGameplayTime = pausedGameplaySource.time;
                pausedGameplaySource.Pause();
            }

            MusicTrack pauseTrack = new MusicTrack
            {
                clip = pauseMenuClip,
                baseVolume = pauseMenuBaseVolume
            };

            fadeCoroutine = StartCoroutine(CrossfadeToPauseMusic(pauseTrack, fadeDuration));
        }
        else
        {
            if (activeSource != null) activeSource.Pause();
            if (inactiveSource != null) inactiveSource.Pause();
        }
    }

    public void ResumeGameMusic(float fadeDuration = 0.5f)
    {
        if (!isPaused) return;
        isPaused = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (pauseMenuClip != null && pausedGameplaySource != null)
        {
            fadeCoroutine = StartCoroutine(CrossfadeFromPauseMusic(fadeDuration));
        }
        else
        {
            audioSourceA.UnPause();
            audioSourceB.UnPause();
        }
    }

    private IEnumerator CrossfadeToPauseMusic(MusicTrack pauseTrack, float duration)
    {
        inactiveSource.clip = pauseTrack.clip;
        inactiveSource.time = 0f;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            inactiveSource.volume = Mathf.Lerp(0f, pauseTrack.baseVolume, t);
            yield return null;
        }

        inactiveSource.volume = pauseTrack.baseVolume;

        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        fadeCoroutine = null;
    }

    private IEnumerator CrossfadeFromPauseMusic(float duration)
    {
        AudioSource pauseMenuSource = activeSource;
        AudioSource gameplaySource = pausedGameplaySource;

        float pauseStartVolume = pauseMenuSource.volume;

        if (!trackLookup.TryGetValue(currentMusicState, out MusicTrack targetTrack))
        {
            targetTrack.baseVolume = 1f;
        }

        gameplaySource.UnPause();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            pauseMenuSource.volume = Mathf.Lerp(pauseStartVolume, 0f, t);
            gameplaySource.volume = Mathf.Lerp(0f, targetTrack.baseVolume, t);

            yield return null;
        }

        pauseMenuSource.volume = 0f;
        pauseMenuSource.Stop();
        pauseMenuSource.clip = null;

        gameplaySource.volume = targetTrack.baseVolume;

        activeSource = gameplaySource;
        inactiveSource = pauseMenuSource;
        pausedGameplaySource = null;

        fadeCoroutine = null;
    }

    public void TransitionToState(MusicState newState, float fadeDuration = -1f)
    {
        if (currentMusicState == newState && activeSource.isPlaying) return;

        if (!trackLookup.TryGetValue(newState, out MusicTrack targetTrack))
        {
            Debug.LogWarning($"[MusicManager] No track defined for state: {newState}");
            return;
        }

        currentMusicState = newState;
        float actualFadeDuration = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (isPaused)
        {
            if (pausedGameplaySource != null)
            {
                pausedGameplaySource.Stop();
                pausedGameplaySource.clip = targetTrack.clip;
                pausedGameplaySource.time = 0f;
            }
            return;
        }

        fadeCoroutine = StartCoroutine(CrossfadeRoutine(targetTrack, actualFadeDuration));
    }

    private IEnumerator CrossfadeRoutine(MusicTrack targetTrack, float duration)
    {
        inactiveSource.clip = targetTrack.clip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float elapsed = 0f;
        float activeStartVolume = activeSource.volume;
        float targetMaxVolume = targetTrack.baseVolume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            activeSource.volume = Mathf.Lerp(activeStartVolume, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, targetMaxVolume, t);

            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();

        inactiveSource.volume = targetMaxVolume;

        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        fadeCoroutine = null;
    }

    public void PlayStinger(AudioClip stingerClip, float stingerVolume = 1f)
    {
        if (stingerClip == null) return;
        activeSource.PlayOneShot(stingerClip, stingerVolume);
    }

    public void StopAllMusic(float fadeDuration = 1f)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        float startVolA = audioSourceA.volume;
        float startVolB = audioSourceB.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            audioSourceA.volume = Mathf.Lerp(startVolA, 0f, t);
            audioSourceB.volume = Mathf.Lerp(startVolB, 0f, t);

            yield return null;
        }

        audioSourceA.Stop();
        audioSourceB.Stop();
        fadeCoroutine = null;
    }
}