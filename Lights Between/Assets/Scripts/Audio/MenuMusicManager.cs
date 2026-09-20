using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager Instance { get; private set; }

    [Header("Audio Tracks")]
    [SerializeField] private AudioClip[] menuTracks;
    [SerializeField] private bool playRandomly = false;
    [SerializeField] private bool loopPlaylist = true;

    [Header("Volume & Fade Settings")]
    [Range(0f, 1f)][SerializeField] private float targetVolume = 0.8f;
    [SerializeField] private float fadeDuration = 1.0f;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;
    private Coroutine fadeCoroutine;
    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (!isPlaying || audioSource == null) return;

        if (!audioSource.isPlaying && loopPlaylist && Time.timeScale > 0f)
        {
            PlayNextTrack();
        }
    }

    public void StartMusic()
    {
        if (menuTracks == null || menuTracks.Length == 0) return;

        isPlaying = true;
        currentTrackIndex = playRandomly ? Random.Range(0, menuTracks.Length) : 0;

        PlayTrack(currentTrackIndex);
    }

    private void PlayTrack(int index)
    {
        if (index < 0 || index >= menuTracks.Length) return;

        audioSource.clip = menuTracks[index];
        audioSource.volume = targetVolume;
        audioSource.Play();
    }

    public void PlayNextTrack()
    {
        if (menuTracks == null || menuTracks.Length == 0) return;

        if (playRandomly)
        {
            int randomIndex = Random.Range(0, menuTracks.Length);
            while (randomIndex == currentTrackIndex && menuTracks.Length > 1)
            {
                randomIndex = Random.Range(0, menuTracks.Length);
            }
            currentTrackIndex = randomIndex;
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % menuTracks.Length;
        }

        PlayTrack(currentTrackIndex);
    }

    public void FadeOutAndStop(float duration = -1f)
    {
        float actualDuration = duration > 0f ? duration : fadeDuration;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutRoutine(actualDuration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        isPlaying = false;
        fadeCoroutine = null;
    }
}