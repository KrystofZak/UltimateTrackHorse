using UnityEngine;
using UnityEngine.InputSystem;

public class CarMovement : MonoBehaviour
{
    [Header("Car Components")]
    public Rigidbody rb;
    public Transform carBody;
    private BoxCollider carCollider;

    [Header("Surface: Asphalt")]
    public Material asphaltMaterial;
    public float asphaltAccel = 35f;
    public float asphaltMaxSpeed = 18f;
    public float asphaltTurnSpeed = 180f;
    public float asphaltGrip = 8f;

    [Header("Surface: Grass (no material)")]
    public float grassAccel = 8f;
    public float grassMaxSpeed = 7f;
    public float grassTurnSpeed = 120f;
    public float grassGrip = 3f;

    [Header("General Movement")]
    public float brakeDecel = 25f;
    public float naturalDecel = 12f;

    [Header("Drift / Handbrake")]
    public float driftTurnMultiplier = 1.6f;
    public float driftGripMultiplier = 0.25f;
    public float driftDecel = 4f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.65f;
    public float groundedGravity = 25f;
    public float airGravity = 40f;

    [Header("Visual Tilt")]
    public float tiltSpeed = 10f;

    private Vector2 inputVector;
    private bool isHandbraking;
    private float currentSpeed = 0f;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private float currentAccel;
    private float currentMaxSpeed;
    private float currentTurnSpeed;
    private float currentGrip;

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

    void Update()
    {
        SteerCar();
        AlignVisualsWithGround();
    }

    void FixedUpdate()
    {
        CheckGroundAndMaterial();
        MoveCar();
        ApplyLateralFriction();
    }

    void CheckGroundAndMaterial()
    {
        if (carCollider != null) carCollider.enabled = false;

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        bool hit = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, groundCheckDistance);

        if (carCollider != null) carCollider.enabled = true;

        if (hit)
        {
            isGrounded = true;
            groundNormal = hitInfo.normal;

            MeshRenderer groundRenderer = hitInfo.collider.GetComponent<MeshRenderer>();
            bool onAsphalt = groundRenderer != null && groundRenderer.sharedMaterial == asphaltMaterial;

            if (onAsphalt)
            {
                currentAccel = asphaltAccel;
                currentMaxSpeed = asphaltMaxSpeed;
                currentTurnSpeed = asphaltTurnSpeed;
                currentGrip = asphaltGrip;
            }
            else
            {
                currentAccel = grassAccel;
                currentMaxSpeed = grassMaxSpeed;
                currentTurnSpeed = grassTurnSpeed;
                currentGrip = grassGrip;
            }
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }

    void SteerCar()
    {
        if (!isGrounded || Mathf.Abs(currentSpeed) < 0.3f) return;

        float turnDir = currentSpeed >= 0 ? 1f : -1f;
        float turnMult = isHandbraking ? driftTurnMultiplier : 1f;
        float rotationAmount = inputVector.x * currentTurnSpeed * turnMult * turnDir * Time.deltaTime;
        transform.Rotate(0f, rotationAmount, 0f);
    }

    void MoveCar()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * airGravity, ForceMode.Acceleration);
            return;
        }

        rb.AddForce(Vector3.down * groundedGravity, ForceMode.Acceleration);

        if (isHandbraking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, driftDecel * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(inputVector.y) > 0.01f)
        {
            float targetSpeed = inputVector.y * currentMaxSpeed;

            if (Mathf.Sign(inputVector.y) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.1f)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDecel * Time.fixedDeltaTime);
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccel * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, naturalDecel * Time.fixedDeltaTime);
        }

        Vector3 newVelocity = transform.forward * currentSpeed;
        newVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = newVelocity;
    }

    void ApplyLateralFriction()
    {
        if (!isGrounded) return;

        float grip = isHandbraking ? currentGrip * driftGripMultiplier : currentGrip;

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, grip * Time.fixedDeltaTime);
        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }

    void AlignVisualsWithGround()
    {
        if (carBody == null) return;
        Quaternion targetRot = Quaternion.FromToRotation(carBody.up, groundNormal) * carBody.rotation;
        carBody.rotation = Quaternion.Slerp(carBody.rotation, targetRot, Time.deltaTime * tiltSpeed);
    }
}