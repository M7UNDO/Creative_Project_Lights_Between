using UnityEngine;

public class LighterScript : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject _flameObject;
    [SerializeField] private GameObject _flameLight;
    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] private Animator _animator;

    [Header("SFX")]
    [SerializeField] private AudioSource _lighterAudioSource;
    [SerializeField] private AudioClip _lighterSpark;
    [SerializeField] private AudioClip _closeLighter;

    private static readonly int SparkTrigger = Animator.StringToHash("Spark");
    private static readonly int CloseTrigger = Animator.StringToHash("Close");

    private bool _isLit;

    public bool IsLit => _isLit;

    private void Awake()
    {
        if (_inputHandler == null)
        {
            _inputHandler = Object.FindFirstObjectByType<InputHandler>();
        }

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_flameObject != null)
        {
            _flameObject.SetActive(false);
        }

        if (_flameLight != null)
        {
            _flameLight.SetActive(false);
        }
    }

    private void Update()
    {
        if (_inputHandler == null) return;

        if (_inputHandler.toggleFlashlight)
        {
            Debug.Log("Toggle Lighter");
            ToggleFlame();
            _inputHandler.toggleFlashlight = false;
        }
    }

    public void ToggleFlame()
    {
        _isLit = !_isLit;
        HandleFlameState();
    }

    public void SetFlameState(bool state)
    {
        _isLit = state;
        HandleFlameState();
    }

    private void HandleFlameState()
    {
        if (_flameObject != null)
        {
            _flameObject.SetActive(_isLit);
        }

        if (_flameLight != null)
        {
            _flameLight.SetActive(_isLit);
        }

        if (_isLit)
        {
            if (_animator != null)
            {
                _animator.ResetTrigger(CloseTrigger);
                _animator.SetTrigger(SparkTrigger);
            }

            if (_lighterAudioSource != null && _lighterSpark != null)
            {
                _lighterAudioSource.PlayOneShot(_lighterSpark);
            }
        }
        else
        {
            if (_animator != null)
            {
                _animator.ResetTrigger(SparkTrigger);
                _animator.SetTrigger(CloseTrigger);
            }

            if (_lighterAudioSource != null && _closeLighter != null)
            {
                _lighterAudioSource.PlayOneShot(_closeLighter);
            }
        }
    }
}