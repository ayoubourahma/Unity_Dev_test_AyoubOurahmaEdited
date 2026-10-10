using UnityEngine;
using UnityEngine.InputSystem;

public class Tank_Inputs : MonoBehaviour
{
    public Camera camera1;
    [SerializeField] private LayerMask aimingLayerMask = 1 << 8;

    public Vector3 reticlePosition;
    public Vector3 ReticlePosition
    {
        get { return reticlePosition; }
    }

    private Vector3 reticleNormal;
    public Vector3 ReticleNormal
    {
        get { return reticleNormal; }
    }

    private float forwardInput;
    public float ForwardInput
    {
        get { return forwardInput; }
    }

    private float rotationInput;
    public float RotationInput
    {
        get { return rotationInput; }
    }

    private InputAction driveAction;
    private InputAction mouseAimAction;
    private InputAction gamepadAimAction;
    private InputAction fireAction;
    private Vector2 previousMousePosition;
    private bool hasPreviousMousePosition;
    private bool gamepadAiming;

    public bool FirePressedThisFrame
    {
        get { return fireAction != null && fireAction.WasPressedThisFrame(); }
    }

    private void Awake()
    {
        driveAction = new InputAction("Drive", InputActionType.Value, expectedControlType: "Vector2");
        driveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        driveAction.AddBinding("<Gamepad>/leftStick");

        mouseAimAction = new InputAction("Mouse Aim", InputActionType.Value, "<Mouse>/position");
        gamepadAimAction = new InputAction("Gamepad Aim", InputActionType.Value, "<Gamepad>/rightStick");

        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/rightTrigger");
        fireAction.AddBinding("<Gamepad>/rightShoulder");
    }

    private void OnEnable()
    {
        driveAction.Enable();
        mouseAimAction.Enable();
        gamepadAimAction.Enable();
        fireAction.Enable();
    }

    private void OnDisable()
    {
        driveAction.Disable();
        mouseAimAction.Disable();
        gamepadAimAction.Disable();
        fireAction.Disable();
    }

    private void OnDestroy()
    {
        driveAction.Dispose();
        mouseAimAction.Dispose();
        gamepadAimAction.Dispose();
        fireAction.Dispose();
    }

    private void Update()
    {
        Vector2 drive = driveAction.ReadValue<Vector2>();
        forwardInput = drive.y;
        rotationInput = drive.x;

        if (!camera1)
        {
            return;
        }

        HandleAim();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(reticlePosition, 0.5f);
    }

    private void HandleAim()
    {
        Vector2 mousePosition = mouseAimAction.ReadValue<Vector2>();
        Vector2 gamepadAim = gamepadAimAction.ReadValue<Vector2>();

        if (hasPreviousMousePosition && (mousePosition - previousMousePosition).sqrMagnitude > 0.01f)
        {
            gamepadAiming = false;
        }

        previousMousePosition = mousePosition;
        hasPreviousMousePosition = true;

        if (gamepadAim.sqrMagnitude > 0.04f)
        {
            gamepadAiming = true;
        }

        if (gamepadAiming)
        {
            float screenRadius = Mathf.Min(Screen.width, Screen.height) * 0.45f;
            Vector2 aimScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                + Vector2.ClampMagnitude(gamepadAim, 1f) * screenRadius;
            UpdateReticle(aimScreenPosition);
        }
        else
        {
            UpdateReticle(mousePosition);
        }
    }

    private void UpdateReticle(Vector2 screenPosition)
    {
        Ray screenRay = camera1.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(
                screenRay,
                out RaycastHit hit,
                Mathf.Infinity,
                aimingLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            reticlePosition = hit.point;
            reticleNormal = hit.normal;
        }
    }
}
