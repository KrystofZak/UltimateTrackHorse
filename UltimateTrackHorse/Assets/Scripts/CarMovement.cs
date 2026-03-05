using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [Header("Non-defined Surface")]
    public float defaultAccel = 0.5f;
    public float defaultMaxSpeed = 5f;
    public float defaultGrip = 1f;

    [Header("Surface: Asphalt")]
    public PhysicsMaterial asphaltMaterial;
    public float asphaltAccel = 2.5f;
    public float asphaltMaxSpeed = 20f;
    public float asphaltGrip = 5f;

    [Header("Surface: Ice")]
    public PhysicsMaterial iceMaterial;
    public float iceAccel = 1f;
    public float iceMaxSpeed = 30f;
    public float iceGrip = 0.5f;

    [Header("General Movement")]
    public float turnSpeed = 100f;
    public float reverseSpeed = 2f;
    public float coastingDeceleration = 1f;
    public float handbrakeGrip = 0.5f;

    [Header("Physics & Raycast")]
    public float groundCheckDistance = 0.6f;
    

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;
    private bool isHandbraking;
    public bool isGrounded;

    private float currentAccel;
    private float currentMaxSpeed;
    private float currentGrip;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        isHandbraking = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckDistance);
        if (isGrounded)
        {
            DetectSurfaceMaterial(hit);
            ApplySteering();
            ApplyMovementAndDrift(hit);
        }
    }

    void DetectSurfaceMaterial(RaycastHit hit)
    {
       
        currentAccel = defaultAccel;
        currentMaxSpeed = defaultMaxSpeed;
        currentGrip = defaultGrip;

        if (hit.collider.sharedMaterial == null)
        {
            return;
        }

        if (hit.collider.sharedMaterial == asphaltMaterial)
        {
            currentAccel = asphaltAccel;
            currentMaxSpeed = asphaltMaxSpeed;
            currentGrip = asphaltGrip;
        }
        else if (hit.collider.sharedMaterial == iceMaterial)
        {
            currentAccel = iceAccel;
            currentMaxSpeed = iceMaxSpeed;
            currentGrip = iceGrip;
        }
    }

    void ApplySteering()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (flatVelocity.magnitude < 0.5f) return;
        float localVelocityZ = transform.InverseTransformDirection(rb.linearVelocity).z;
        float steerMultiplier = (localVelocityZ < -0.1f) ? -1f : 1f;
        float speedFactor = Mathf.Clamp01(flatVelocity.magnitude / 10f);
        float activeTurnSpeed = isHandbraking ? turnSpeed * 1.5f : turnSpeed;
        float rotation = steerInput * activeTurnSpeed * steerMultiplier * speedFactor * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotation, 0));

    }

    void ApplyMovementAndDrift(RaycastHit hit)
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;

        if (!isHandbraking && Mathf.Abs(moveInput) > 0.1f)
        {
            velocity += slopeForward * (moveInput * currentAccel * Time.fixedDeltaTime);
        }

        Vector3 lateralVelocity = Vector3.ProjectOnPlane(velocity, hit.normal);
        float fallSpeed = Vector3.Dot(velocity, hit.normal);

        float drag = 0f;
        if (isHandbraking) drag = 4f;
        else if (Mathf.Abs(moveInput) < 0.1f) drag = coastingDeceleration;

        lateralVelocity = Vector3.Lerp(lateralVelocity, Vector3.zero, drag * Time.fixedDeltaTime);

        float activeMaxSpeed = (moveInput < 0) ? reverseSpeed : currentMaxSpeed;
        if (lateralVelocity.magnitude > activeMaxSpeed)
        {
            lateralVelocity = lateralVelocity.normalized * activeMaxSpeed;
        }

        if (lateralVelocity.magnitude > 0.1f)
        {
            float activeGrip = isHandbraking ? handbrakeGrip : currentGrip;
            float forwardSpeed = Vector3.Dot(lateralVelocity, slopeForward);
            Vector3 optimalDirection = slopeForward * Mathf.Sign(forwardSpeed >= 0 ? 1 : -1);

            Vector3 newDirection = Vector3.Lerp(lateralVelocity.normalized, optimalDirection, activeGrip * Time.fixedDeltaTime).normalized;
            lateralVelocity = newDirection * lateralVelocity.magnitude;
        }

        rb.linearVelocity = lateralVelocity + (hit.normal * fallSpeed);
    }
}