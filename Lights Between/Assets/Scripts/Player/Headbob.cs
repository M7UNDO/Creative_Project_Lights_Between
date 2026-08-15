using UnityEngine;

public class Headbob : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private bool _enable = true;
    [SerializeField] private Transform _cameraTarget = null;

    [Header("Walking Headbob")]
    [SerializeField, Range(0, 0.1f)] private float _walkAmplitude = 0.015f;
    [SerializeField, Range(0, 30)] private float _walkFrequency = 10.0f;

    [Header("Sprinting Headbob")]
    [SerializeField, Range(0, 0.2f)] private float _sprintAmplitude = 0.03f;
    [SerializeField, Range(0, 40)] private float _sprintFrequency = 14.0f;

    [Header("Smoothing")]
    [SerializeField] private float _toggleSpeed = 0.1f;
    [SerializeField] private float _resetSpeed = 10.0f;

    private Vector3 _startPos;
    private CharacterController _controller;
    private FPSController _fpsController;
    private float _timer;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _fpsController = GetComponent<FPSController>();

        if (_cameraTarget != null)
        {
            _startPos = _cameraTarget.localPosition;
        }
    }

    private void Update()
    {
        if (!_enable || _cameraTarget == null || _controller == null) return;

        float horizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
        bool isGrounded = _fpsController != null ? _fpsController.Grounded : _controller.isGrounded;

        if (isGrounded && horizontalSpeed > _toggleSpeed)
        {
            bool isSprinting = _fpsController != null && horizontalSpeed > (_fpsController.MoveSpeed + 0.1f);

            float currentFrequency = isSprinting ? _sprintFrequency : _walkFrequency;
            float currentAmplitude = isSprinting ? _sprintAmplitude : _walkAmplitude;

            _timer += Time.deltaTime * currentFrequency;

            Vector3 targetOffset = CalculateHeadbobOffset(currentAmplitude);
            _cameraTarget.localPosition = _startPos + targetOffset;
        }
        else
        {
            _timer = 0f;
            _cameraTarget.localPosition = Vector3.Lerp(_cameraTarget.localPosition, _startPos, Time.deltaTime * _resetSpeed);
        }
    }

    private Vector3 CalculateHeadbobOffset(float amplitude)
    {
        float targetY = Mathf.Sin(_timer) * amplitude;
        float targetX = Mathf.Cos(_timer * 0.5f) * (amplitude * 2.0f);
        return new Vector3(targetX, targetY, 0f);
    }
}