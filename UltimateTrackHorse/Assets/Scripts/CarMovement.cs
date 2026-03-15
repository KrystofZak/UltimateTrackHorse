using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [Header("Car Components")]
    public Rigidbody rb;
    public Transform carBody;
    private BoxCollider carCollider;

    [Header("Surface: Asphalt")]
    public Material asphaltMaterial;
    public float asphaltAccel = 2.5f;
    public float asphaltMaxSpeed = 18f;
    public float asphaltTurnSpeed = 100f;
    public float asphaltGrip = 5f;

    [Header("Surface: Grass")]
    public float grassAccel = 8f;
    public float grassMaxSpeed = 7f;
    public float grassTurnSpeed = 100f;
    public float grassGrip = 8f;

    [Header("General Movement")]
    public float brakeDecel = 30f;
    public float naturalDecel = 14f;

    [Header("Drift Settings")]
    public float turnSlipGrip = 2f;
    public float handbrakeGrip = 0.5f;
    public float driftTurnMultiplier = 2f;
    public float driftDecel = 5f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 1.5f;
    public float groundedGravity = 30f;
    public float airGravity = 45f;

    [Header("Visuals")]
    public float tiltSpeed = 12f;

    private Vector2 inputVector;
    private bool isHandbraking;
    private float currentSpeed;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private float currentAccel;
    private float currentMaxSpeed;
    private float currentTurnSpeed;
    private float currentGrip;

    [Header("Debug")]
    public bool showDebug = false;
    private string debugHitObjectName = "---";
    private string debugHitMaterialName = "---";
    private string debugSurfaceType = "---";
    private float debugGrip = 0f;
    private float debugAngleDiff = 0f;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        carCollider = GetComponent<BoxCollider>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>();
    }

    public void OnHandBrake(InputAction.CallbackContext context)
    {
        isHandbraking = context.ReadValue<float>() > 0.5f;
    }

    void FixedUpdate()
    {
        CheckGroundAndMaterial();
        SteerCar();
        MoveCar();

        Debug.DrawRay(transform.position, transform.forward * 4f, Color.blue);
        Debug.DrawRay(transform.position, rb.linearVelocity, Color.yellow);
    }

    void Update()
    {
        AlignVisualsWithGround();
    }

    void CheckGroundAndMaterial()
    {
        if (carCollider != null) carCollider.enabled = false;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool hit = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hitInfo, groundCheckDistance);
        if (carCollider != null) carCollider.enabled = true;

        if (showDebug)
        {
            Color rayColor = hit ? Color.green : Color.red;
            Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, rayColor);
        }

        if (hit)
        {
            isGrounded = true;
            groundNormal = hitInfo.normal;
            MeshRenderer r = hitInfo.collider.GetComponent<MeshRenderer>();

            if (showDebug)
            {
                debugHitObjectName = hitInfo.collider.gameObject.name;
                debugHitMaterialName = (r != null && r.sharedMaterial != null) ? r.sharedMaterial.name : "NULL";
            }

            bool onAsphalt = r != null && asphaltMaterial != null && r.sharedMaterial != null
                && r.sharedMaterial.name.Replace(" (Instance)", "") == asphaltMaterial.name.Replace(" (Instance)", "");

            if (showDebug)
                debugSurfaceType = onAsphalt ? "ASPHALT" : "GRASS";

            if (onAsphalt) { currentAccel = asphaltAccel; currentMaxSpeed = asphaltMaxSpeed; currentTurnSpeed = asphaltTurnSpeed; currentGrip = asphaltGrip; }
            else { currentAccel = grassAccel; currentMaxSpeed = grassMaxSpeed; currentTurnSpeed = grassTurnSpeed; currentGrip = grassGrip; }
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
            if (showDebug) { debugHitObjectName = "NOTHING"; debugHitMaterialName = "---"; debugSurfaceType = "AIR"; }
        }
    }

    void SteerCar()
    {
        if (!isGrounded || Mathf.Abs(currentSpeed) < 0.1f) return;

        float turnDir = currentSpeed >= 0f ? 1f : -1f;

        bool isDrifting = isHandbraking || Mathf.Abs(inputVector.x) > 0.1f;
        float turnMult = isDrifting ? driftTurnMultiplier : 1f;

        float rotation = inputVector.x * currentTurnSpeed * turnMult * turnDir * Time.fixedDeltaTime;
        transform.Rotate(0f, rotation, 0f);
    }

    void MoveCar()
    {
        rb.AddForce(Vector3.down * (isGrounded ? groundedGravity : airGravity), ForceMode.Acceleration);

        if (!isGrounded) return;

        if (isHandbraking) currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, driftDecel * Time.fixedDeltaTime);
        else if (Mathf.Abs(inputVector.y) > 0.01f)
        {
            float target = inputVector.y * currentMaxSpeed;
            bool braking = Mathf.Sign(inputVector.y) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.1f;
            float decel = braking ? brakeDecel : currentAccel;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, decel * Time.fixedDeltaTime);
        }
        else currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, naturalDecel * Time.fixedDeltaTime);

        Vector3 currentVel = rb.linearVelocity;
        currentVel.y = 0f;

        Vector3 moveDir = currentVel.magnitude > 0.1f ? currentVel.normalized : (transform.forward * Mathf.Sign(currentSpeed >= 0 ? 1 : -1));
        Vector3 targetDir = transform.forward * (currentSpeed >= 0 ? 1 : -1);

        float grip;
        if (isHandbraking) grip = handbrakeGrip;
        else if (Mathf.Abs(inputVector.x) > 0.01f) grip = turnSlipGrip;
        else grip = currentGrip;

        if (showDebug)
        {
            debugGrip = grip;
            debugAngleDiff = Vector3.Angle(moveDir, targetDir);
        }

        moveDir = Vector3.RotateTowards(moveDir, targetDir, grip * Time.fixedDeltaTime, 0f).normalized;

        Vector3 finalVel = moveDir * Mathf.Abs(currentSpeed);
        finalVel.y = rb.linearVelocity.y;

        rb.linearVelocity = finalVel;
    }

    void AlignVisualsWithGround()
    {
        if (carBody == null) return;
        Vector3 up = groundNormal;
        Vector3 fwd = transform.forward;
        Vector3 right = Vector3.Cross(up, fwd).normalized;
        fwd = Vector3.Cross(right, up).normalized;
        Quaternion targetRot = Quaternion.LookRotation(fwd, up);
        carBody.rotation = Quaternion.Slerp(carBody.rotation, targetRot, Time.deltaTime * tiltSpeed);
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;

        string asphaltMatName = asphaltMaterial != null ? asphaltMaterial.name : "NEPŘIŘAZEN!";

        string info =
            $"=== CAR DEBUG ===\n" +
            $"Povrch:      {debugSurfaceType}\n" +
            $"Hit objekt:  {debugHitObjectName}\n" +
            $"Hit mat:     {debugHitMaterialName}\n" +
            $"Asphalt mat: {asphaltMatName}\n" +
            $"Shoda jmen:  {debugHitMaterialName.Replace(" (Instance)", "") == asphaltMatName.Replace(" (Instance)", "")}\n" +
            $"---\n" +
            $"isGrounded:  {isGrounded}\n" +
            $"Speed:       {currentSpeed:F2}\n" +
            $"Input:       {inputVector}\n" +
            $"Handbrake:   {isHandbraking}\n" +
            $"Grip:        {debugGrip:F3}\n" +
            $"Vel angle:   {debugAngleDiff:F1}° (drift = >{(180f * turnSlipGrip * Time.fixedDeltaTime):F1}°/frame)";

        GUI.Box(new Rect(10, 10, 340, 230), info, style);
    }
}