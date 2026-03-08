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
    public float asphaltGrip = 15f;

    [Header("Surface: Grass (no material)")]
    public float grassAccel = 8f;
    public float grassMaxSpeed = 7f;
    public float grassTurnSpeed = 120f;
    public float grassGrip = 5f;

    [Header("General Movement")]
    public float brakeDecel = 25f;
    public float naturalDecel = 12f;

    [Header("Drift / Handbrake")]
    public float driftTurnMultiplier = 1.6f;
    public float driftGripMultiplier = 0.15f;
    public float driftDecel = 4f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.9f;
    public float groundedGravity = 25f;
    public float airGravity = 40f;

    [Header("Visuals")]
    public float tiltSpeed = 10f;
    public float driftYawMax = 20f;
    public float driftYawSpeed = 5f;

    // --- private state ---
    private Vector2 inputVector;
    private bool isHandbraking;
    private float currentSpeed = 0f;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    private float currentDriftYaw = 0f;

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

    void FixedUpdate()
    {
        CheckGroundAndMaterial();
        SteerCar();
        MoveCar();
        ApplyLateralFriction();
    }

    void Update()
    {
        AlignVisualsWithGround();
    }

    void CheckGroundAndMaterial()
    {
        if (carCollider != null) carCollider.enabled = false;

        Vector3 rayStart = transform.position + transform.up * 0.1f;
        bool hit = Physics.Raycast(rayStart, -transform.up, out RaycastHit hitInfo, groundCheckDistance);

        if (carCollider != null) carCollider.enabled = true;

        if (hit)
        {
            isGrounded = true;
            groundNormal = hitInfo.normal;

            MeshRenderer groundRenderer = hitInfo.collider.GetComponent<MeshRenderer>();
            bool onAsphalt = groundRenderer != null && groundRenderer.sharedMaterial == asphaltMaterial;

            if (onAsphalt)
            {
                currentAccel     = asphaltAccel;
                currentMaxSpeed  = asphaltMaxSpeed;
                currentTurnSpeed = asphaltTurnSpeed;
                currentGrip      = asphaltGrip;
            }
            else
            {
                currentAccel     = grassAccel;
                currentMaxSpeed  = grassMaxSpeed;
                currentTurnSpeed = grassTurnSpeed;
                currentGrip      = grassGrip;
            }
        }
        else
        {
            isGrounded   = false;
            groundNormal = Vector3.up;
        }
    }

    // Øízení je v FixedUpdate — stejný timing jako pohyb, žádný race condition
    void SteerCar()
    {
        if (!isGrounded || Mathf.Abs(currentSpeed) < 0.3f) return;

        float turnDir  = currentSpeed >= 0f ? 1f : -1f;
        float turnMult = isHandbraking ? driftTurnMultiplier : 1f;
        float rotation = inputVector.x * currentTurnSpeed * turnMult * turnDir * Time.fixedDeltaTime;
        transform.Rotate(0f, rotation, 0f);
    }

    void MoveCar()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * airGravity, ForceMode.Acceleration);
            return;
        }

        rb.AddForce(Vector3.down * groundedGravity, ForceMode.Acceleration);

        // Zachováme skuteènou rychlost pohybu — zatáèení nesmí zkrátit velocity
        float flatSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        if (flatSpeed > Mathf.Abs(currentSpeed))
            currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward) >= 0f ? flatSpeed : -flatSpeed;

        if (isHandbraking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, driftDecel * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(inputVector.y) > 0.01f)
        {
            float targetSpeed = inputVector.y * currentMaxSpeed;

            // Brzdìní = opaèný smìr vstupu od aktuálního pohybu
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

        Vector3 local = transform.InverseTransformDirection(rb.linearVelocity);
        local.x = Mathf.MoveTowards(local.x, 0f, grip * Time.fixedDeltaTime);
        rb.linearVelocity = transform.TransformDirection(local);
    }

    void AlignVisualsWithGround()
    {
        if (carBody == null) return;

        // Vizuální smyk: carBody se Y-rotaènì opozdí za smìrem zatáèení
        float speedFactor = Mathf.InverseLerp(0f, currentMaxSpeed > 0f ? currentMaxSpeed : asphaltMaxSpeed, Mathf.Abs(currentSpeed));
        float targetYaw   = -inputVector.x * driftYawMax * speedFactor;
        currentDriftYaw   = Mathf.Lerp(currentDriftYaw, targetYaw, Time.deltaTime * driftYawSpeed);

        // Náklon carBody podle normály terénu — pouze X a Z osa, Y pøebíráme z transform (root)
        // Tím se carBody otáèí spolu s autem a zároveò se naklání podle svahu
        Vector3 worldUp    = groundNormal;
        Vector3 carForward = transform.forward;
        Vector3 carRight   = Vector3.Cross(worldUp, carForward).normalized;
        carForward         = Vector3.Cross(carRight, worldUp).normalized;
        Quaternion terrainRot  = Quaternion.LookRotation(carForward, worldUp);
        Quaternion driftOffset = Quaternion.Euler(0f, currentDriftYaw, 0f);
        Quaternion targetRot   = terrainRot * driftOffset;

        carBody.rotation = Quaternion.Slerp(carBody.rotation, targetRot, Time.deltaTime * tiltSpeed);
    }
}