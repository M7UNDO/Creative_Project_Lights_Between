using System;
using UnityEngine;

public class CandleScript : MonoBehaviour
{
    [Header("Candle Settings")]
    [SerializeField] private bool isIgnited = false;

    [Header("Prompt Messages")]
    [SerializeField] private string pickupPromptMessage = "Pick Up Candle";
    [SerializeField] private string ignitePromptMessage = "Light Candle";
    [SerializeField] private string extinguishPromptMessage = "Extinguish Candle";

    [Header("Visuals & Lighting")]
    [SerializeField] private GameObject flameObject;
    [SerializeField] private Light candleLight;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip igniteSFX;
    [SerializeField] private AudioClip extinguishSFX;

    public bool IsIgnited => isIgnited;
    public string PickupPromptMessage => pickupPromptMessage;
    public string CurrentIgnitePromptMessage => isIgnited ? extinguishPromptMessage : ignitePromptMessage;

    private void Start()
    {
        UpdateVisualState();
    }

    public void ToggleIgnite()
    {
        if (isIgnited)
        {
            Extinguish();
        }
        else
        {
            Ignite();
        }
    }

    public void Ignite()
    {
        isIgnited = true;
        UpdateVisualState();

        if (audioSource != null && igniteSFX != null)
        {
            audioSource.PlayOneShot(igniteSFX);
        }
    }

    public void Extinguish()
    {
        isIgnited = false;
        UpdateVisualState();

        if (audioSource != null && extinguishSFX != null)
        {
            audioSource.PlayOneShot(extinguishSFX);
        }
    }

    public void SetIgnitedState(bool ignited)
    {
        isIgnited = ignited;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (flameObject != null)
        {
            flameObject.SetActive(isIgnited);
        }

        if (candleLight != null)
        {
            candleLight.enabled = isIgnited;
        }
    }
}