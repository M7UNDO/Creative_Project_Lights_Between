using UnityEngine;

public class PlankImpactSFX : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip impactClip;
    private bool hasPlayedImpact = false;

    public void Initialize(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        impactClip = clip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPlayedImpact) return;

        if (audioSource != null && impactClip != null)
        {
            audioSource.PlayOneShot(impactClip);
            hasPlayedImpact = true;
        }
    }
}