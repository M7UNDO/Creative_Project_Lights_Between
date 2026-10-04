using System.Collections;
using UnityEngine;

public class LighterScript : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject _flameObject;
    [SerializeField] private GameObject _flameLight;
    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] private Animator _animator;

    [Header("Timing")]
    [SerializeField] private float _sparkDelay = 0.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource _lighterAudioSource;
    [SerializeField] private AudioClip _lighterSpark;
    [SerializeField] private AudioClip _closeLighter;

    private static readonly int SparkTrigger = Animator.StringToHash("Spark");
    private static readonly int CloseTrigger = Animator.StringToHash("Close");

    private bool _isLit;
    private Coroutine _igniteCoroutine;

    public bool IsLit => _isLit;

    private void Awake()
    {
        if (_inputHandler == null)
        {
            _inputHandler = UnityEngine.Object.FindFirstObjectByType<InputHandler>();
        }

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        ResetLighterStateSilently();
    }

    private void OnDisable()
    {
        ResetLighterStateSilently();
    }

    private void Update()
    {
        if (_inputHandler == null) return;

        if (_inputHandler.toggleFlashlight)
        {
            ToggleFlame();
            _inputHandler.toggleFlashlight = false;
        }
    }

    public void OnEquip()
    {
        SetFlameState(true);
    }

    public void OnUnequip()
    {
        SetFlameState(false);
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
        if (_igniteCoroutine != null)
        {
            StopCoroutine(_igniteCoroutine);
            _igniteCoroutine = null;
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

            _igniteCoroutine = StartCoroutine(IgniteRoutine());
        }
        else
        {
            if (_flameObject != null)
            {
                _flameObject.SetActive(false);
            }

            if (_flameLight != null)
            {
                _flameLight.SetActive(false);
            }

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

    private IEnumerator IgniteRoutine()
    {
        yield return new WaitForSeconds(_sparkDelay);

        if (_flameObject != null)
        {
            _flameObject.SetActive(true);
        }

        if (_flameLight != null)
        {
            _flameLight.SetActive(true);
        }

        _igniteCoroutine = null;
    }

    private void ResetLighterStateSilently()
    {
        _isLit = false;

        if (_igniteCoroutine != null)
        {
            StopCoroutine(_igniteCoroutine);
            _igniteCoroutine = null;
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
}