using UnityEngine;

public class ItemPulseHighlight : MonoBehaviour
{
    [Header("Target Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Proximity & Pulse Settings")]
    [SerializeField] private float proximityDistance = 3.0f;
    [ColorUsage(true, true)]
    [SerializeField] private Color highlightColor = new Color(2.0f, 2.0f, 2.0f, 1.0f);
    [SerializeField] private float pulseSpeed = 4.0f;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private Transform _mainCameraTransform;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        if (targetRenderer == null) return;

        if (_mainCameraTransform == null)
        {
            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        float currentDistance = Vector3.Distance(transform.position, _mainCameraTransform.position);

        if (currentDistance <= proximityDistance)
        {
            float sineWave = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) * 0.5f;
            Color currentEmission = highlightColor * sineWave;

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorID, currentEmission);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
        else
        {
            ResetHighlight();
        }
    }

    private void ResetHighlight()
    {
        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(EmissionColorID, Color.black);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void OnDisable()
    {
        ResetHighlight();
    }
}