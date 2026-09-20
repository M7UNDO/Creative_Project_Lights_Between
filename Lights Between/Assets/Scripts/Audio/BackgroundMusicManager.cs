using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MusicState
{
    CalmAmbience,
    Tension,
    HorrorStinger,
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

    [Header("Track Library")]
    [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();

    [Header("Global Crossfade Settings")]
    [SerializeField] private float defaultFadeDuration = 1.5f;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine fadeCoroutine;
    private MusicState currentMusicState;

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
            elapsed += Time.deltaTime;
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
            elapsed += Time.deltaTime;
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