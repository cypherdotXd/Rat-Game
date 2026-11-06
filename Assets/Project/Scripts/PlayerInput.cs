using UnityEngine;
using InputAction = UnityEngine.InputSystem.InputAction;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

public class PlayerInput : MonoBehaviour, IRatInput
{
    [SerializeField] private Transform _cameraRig;
    [SerializeField] private float _jumpBufferDuration = 0.2f;

    private InputAction _jumpAction;
    private InputAction _moveAction;
    private InputAction _sprintAction;

    private bool _jumpRequested;
    private float _jumpTimer;

    public Vector3 MoveDir { get; private set; }

    public bool Jump
    {
        get => _jumpRequested;
        set => _jumpRequested = value;
    }

    public bool Sprint => _sprintAction != null && _sprintAction.IsPressed();
    public bool IsJumpHeld => _jumpAction != null && _jumpAction.IsPressed();

    public void SetCameraRig(Transform cameraRig) => _cameraRig = cameraRig;

    private void Awake()
    {
        if (!_cameraRig && TryGetComponent<MovementController>(out var controller))
            _cameraRig = controller.CameraRig;
    }

    private void OnEnable()
    {
        var actions = TouchInputManager.InputMain;
        if (!actions.enabled)
        {
            TouchInputManager.InputMain = new PlayerControls().ratControls;
            TouchInputManager.InputMain.Enable();
            actions = TouchInputManager.InputMain;
        }

        _jumpAction = actions.jump;
        _moveAction = actions.move;
        _sprintAction = actions.sprint;

        if (_jumpAction != null)
            _jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        if (_jumpAction != null)
            _jumpAction.performed -= OnJumpPerformed;
    }

    private void OnJumpPerformed(CallbackContext _)
    {
        _jumpRequested = true;
        _jumpTimer = _jumpBufferDuration;
    }

    private void Update()
    {
        if (_jumpRequested)
        {
            _jumpTimer -= Time.deltaTime;
            if (_jumpTimer <= 0f)
                _jumpRequested = false;
        }

        var input = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (input.sqrMagnitude > 0.001f)
        {
            if (_cameraRig != null)
            {
                var camRotation = Quaternion.Euler(0f, _cameraRig.eulerAngles.y, 0f);
                var camForward = camRotation * Vector3.forward;
                var camRight = camRotation * Vector3.right;
                var dir = camForward * input.y + camRight * input.x;
                MoveDir = Vector3.ClampMagnitude(dir, 1f);
            }
            else
            {
                MoveDir = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
            }
        }
        else
        {
            MoveDir = Vector3.zero;
        }
    }
}
