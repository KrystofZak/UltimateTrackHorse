using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [Header("Surface: Asphalt")]
    public float asphaltAccel = 25f;
    public float asphaltMaxSpeed = 40f;
    public float asphaltGrip = 5f;

    [Header("Surface: Ice")]
    public PhysicsMaterial iceMaterial;
    public float iceAccel = 8f;
    public float iceMaxSpeed = 45f;
    public float iceGrip = 0.5f;


    [Header("Control & Handbrake")]
    public float reverseSpeed = 15f;
    public float turnSpeed = 250f;
    public float coastingDeceleration = 1.5f;
    public float handbrakeGrip = 1f;

    [Header("Physics & Raycast")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;
    private bool isHandbraking;
    private bool isGrounded;

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
        rb.angularVelocity = Vector3.zero;

        isGrounded = Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, groundCheckDistance, groundLayer);

        if (isGrounded)
        {
            DetectSurfaceMaterial(hit);
            ApplySteering();
            ApplyMovementAndDrift();
        }
    }

    void DetectSurfaceMaterial(RaycastHit hit)
    {
        currentAccel = asphaltAccel;
        currentMaxSpeed = asphaltMaxSpeed;
        currentGrip = asphaltGrip;

        if (hit.collider != null && hit.collider.sharedMaterial != null)
        {
            PhysicsMaterial surfaceMat = hit.collider.sharedMaterial;

            if (iceMaterial != null && surfaceMat == iceMaterial || surfaceMat.name.ToUpper().Contains("ICE"))
            {
                currentAccel = iceAccel;
                currentMaxSpeed = iceMaxSpeed;
                currentGrip = iceGrip;
            }
            
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

    void ApplyMovementAndDrift()
    {
        Vector3 velocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // 1. MOTOR (Tlačíme auto dopředu/dozadu)
        if (!isHandbraking && Mathf.Abs(moveInput) > 0.1f)
        {
            velocity += transform.forward * (moveInput * currentAccel * Time.fixedDeltaTime);
        }

        // 2. BRZDY A ODPOR (Dojíždění)
        float drag = 0f;
        if (isHandbraking) drag = 4f;
        else if (Mathf.Abs(moveInput) < 0.1f) drag = coastingDeceleration;

        velocity = Vector3.Lerp(velocity, Vector3.zero, drag * Time.fixedDeltaTime);

        // Omezení maximální rychlosti
        float activeMaxSpeed = (moveInput < 0) ? reverseSpeed : currentMaxSpeed;
        if (velocity.magnitude > activeMaxSpeed)
        {
            velocity = velocity.normalized * activeMaxSpeed;
        }

        // 3. KOUZLO DOC HUDSONA (Srovnání letu auta s čumákem)
        if (velocity.magnitude > 0.1f)
        {
            float activeGrip = isHandbraking ? handbrakeGrip : currentGrip;

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            Vector3 optimalDirection = transform.forward * Mathf.Sign(forwardSpeed >= 0 ? 1 : -1);

            // OPRAVA: Přidáno .normalized! Tím se vektor po prolnutí nesmrskne 
            // a auto si zachová přesně tu rychlost, jakou mělo před smykem.
            Vector3 newDirection = Vector3.Lerp(velocity.normalized, optimalDirection, activeGrip * Time.fixedDeltaTime).normalized;
            velocity = newDirection * velocity.magnitude;
        }

        velocity.y = rb.linearVelocity.y; // Vrátíme autu gravitaci
        rb.linearVelocity = velocity;
    }
}