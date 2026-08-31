using UnityEngine;
using UnityEngine.Audio;

public class CreatureVoiceAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Voice Clips")]
    [SerializeField] private AudioClip[] whisperClips;
    [SerializeField] private AudioClip[] lightReactionClips;
    [SerializeField] private AudioClip[] chaseClips;

    [Header("Whisper Settings")]
    [SerializeField] private float minWhisperDelay = 5f;
    [SerializeField] private float maxWhisperDelay = 12f;
    [Range(0f, 1f)]
    [SerializeField] private float whisperVolume = 0.45f;

    [Header("Reaction Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float reactionVolume = 0.8f;

    [Header("Chase Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float chaseVolume = 0.75f;

    private float whisperTimer;
    private bool whisperEnabled;

    private void Awake()
    {
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
        }

        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.spatialBlend = 1f;
        voiceAudioSource.outputAudioMixerGroup = sfxMixerGroup;

        ResetWhisperTimer();
    }

    private void Update()
    {
        if (!whisperEnabled) return;

        whisperTimer -= Time.deltaTime;

        if (whisperTimer <= 0f)
        {
            PlayRandomClip(whisperClips, whisperVolume);
            ResetWhisperTimer();
        }
    }

    public void EnableWhispers(bool enabled)
    {
        whisperEnabled = enabled;

        if (enabled)
        {
            ResetWhisperTimer();
        }
    }

    public void PlayLightReaction()
    {
        PlayRandomClip(lightReactionClips, reactionVolume);
    }

    public void PlayChaseVoice()
    {
        PlayRandomClip(chaseClips, chaseVolume);
    }

    private void PlayRandomClip(AudioClip[] clips, float volume)
    {
        if (voiceAudioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        voiceAudioSource.pitch = Random.Range(0.9f, 1.1f);
        voiceAudioSource.PlayOneShot(clips[index], volume);
    }

    private void ResetWhisperTimer()
    {
        whisperTimer = Random.Range(minWhisperDelay, maxWhisperDelay);
    }
}