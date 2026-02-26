using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [Header("Pohyb")]
    public float acceleration = 5f;       // Jak silně auto táhne (zkus 5-10)
    public float maxSpeed = 50f;          // Reálná rychlost v Unity jednotkách
    public float reverseSpeed = 20f;
    public float turnSpeed = 180f;        // Ve stupních za sekundu

    [Header("Drift a Přilnavost")]
    [Range(0, 1)]
    public float driftFactor = 0.95f;

    [Header("Ruční brzda")]
    public float handbrakeDeceleration = 3f;      // Jak rychle auto zpomaluje ve smyku
    [Range(0, 1)]
    public float handbrakeDriftFactor = 0.995f;   // Téměř 1.0 = auto letí bokem jako na ledě
    public float handbrakeTurnMultiplier = 1.3f;  // O kolik ostřeji zatáčí přes ručku

    [Header("Fyzika země")]
    public float groundCheckDistance = 0.5f; // Jak hluboko pod auto koukáme
    public LayerMask groundLayer;           // Nastav v Inspectoru na vrstvu podlahy
    public bool isGrounded;

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;
    private bool isHandbraking;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -1f, 0);
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        steerInput = Input.GetAxisRaw("Horizontal");

        // Detekce ruční brzdy (Mezerník)
        isHandbraking = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position, -transform.up, groundCheckDistance, groundLayer);

        if (isGrounded)
        {
            ApplyMovement();
            ApplySteering();
            ApplyLateralFriction();
        }
    }

    void ApplyMovement()
    {
        float currentY = rb.linearVelocity.y;
        Vector3 newVelocity;

        if (isHandbraking)
        {
            // Pokud držíme ručku, přestaneme přidávat plyn a postupně zpomalujeme k nule
            // (Záměrně nezastavíme hned, aby auto mohlo doklouzat)
            newVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, handbrakeDeceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Normální jízda
            float targetSpeedInput = moveInput * (moveInput > 0 ? maxSpeed : reverseSpeed);
            Vector3 targetVelocity = transform.forward * targetSpeedInput;
            newVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }

        newVelocity.y = currentY; // Aby auto neplulo, ale padalo gravitací
        rb.linearVelocity = newVelocity;
    }

    void ApplySteering()
    {
        float localVelocityZ = transform.InverseTransformDirection(rb.linearVelocity).z;

        float steerMultiplier = 1f;
        if (localVelocityZ < -0.1f) steerMultiplier = -1f;

        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 2f);

        // Pokud je ručka zatažená, vynásobíme rychlost zatáčení
        float currentTurnSpeed = isHandbraking ? turnSpeed * handbrakeTurnMultiplier : turnSpeed;

        float rotation = steerInput * currentTurnSpeed * steerMultiplier * speedFactor * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotation, 0));
    }

    void ApplyLateralFriction()
    {
        Vector3 forwardVel = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVel = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        // Pokud je ručka zatažená, auto ztrácí boční přilnavost a jde do driftu
        float currentDriftFactor = isHandbraking ? handbrakeDriftFactor : driftFactor;

        rb.linearVelocity = forwardVel + (rightVel * currentDriftFactor);
    }
}