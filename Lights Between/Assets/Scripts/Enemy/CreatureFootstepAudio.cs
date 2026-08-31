using UnityEngine;
using UnityEngine.Audio;

public class CreatureFootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundRayDistance = 1.5f;
    [SerializeField] private float groundCheckHeightOffset = 0.5f;

    [Header("Wood Footsteps")]
    [SerializeField] private AudioClip[] woodWalkFootstepClips;
    [SerializeField] private AudioClip[] woodRunFootstepClips;

    [Header("Tile Footsteps")]
    [SerializeField] private AudioClip[] tileWalkFootstepClips;
    [SerializeField] private AudioClip[] tileRunFootstepClips;

    [Header("Carpet Footsteps")]
    [SerializeField] private AudioClip[] carpetWalkFootstepClips;
    [SerializeField] private AudioClip[] carpetRunFootstepClips;

    [Header("Default Footsteps")]
    [SerializeField] private AudioClip[] defaultWalkFootstepClips;
    [SerializeField] private AudioClip[] defaultRunFootstepClips;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.6f;

    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.15f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    // Animation Event on walk animation
    public void OnCreatureWalkFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

        PlayFootstep(false);
    }

    // Animation Event on run animation
    public void OnCreatureRunFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

        PlayFootstep(true);
    }

    private void PlayFootstep(bool isRunning)
    {
        if (audioSource == null) return;

        AudioClip[] chosenClips = GetFootstepClips(isRunning);

        if (chosenClips == null || chosenClips.Length == 0)
            return;

        int index = Random.Range(0, chosenClips.Length);

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(chosenClips[index], footstepVolume);
    }

    private AudioClip[] GetFootstepClips(bool isRunning)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeightOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayers))
        {
            if (hit.collider.CompareTag("Wood"))
            {
                return isRunning ? woodRunFootstepClips : woodWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Tile"))
            {
                return isRunning ? tileRunFootstepClips : tileWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Carpet"))
            {
                return isRunning ? carpetRunFootstepClips : carpetWalkFootstepClips;
            }
        }

        return isRunning ? defaultRunFootstepClips : defaultWalkFootstepClips;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeightOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundRayDistance);
    }
}