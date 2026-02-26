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

    [Header("Fyzika země")]
    public float groundCheckDistance = 0.5f; // Jak hluboko pod auto koukáme
    public LayerMask groundLayer;           // Nastav v Inspectoru na vrstvu podlahy
    public bool isGrounded;

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -1f, 0);
    }

    void Update()
    {
        // GetAxisRaw dává okamžitou odezvu (0 nebo 1), což je pro Trackmanii lepší
        moveInput = Input.GetAxisRaw("Vertical");
        steerInput = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position, -transform.up, groundCheckDistance, groundLayer);

        // 2. Pohyb a zatáčení povolíme jen, když jsme na zemi
        if (isGrounded)
        {
            ApplyMovement();
            ApplySteering();
            ApplyLateralFriction();
        }
    }

    void ApplyMovement()
    {
        // VÝPOČET CÍLOVÉ RYCHLOSTI
        float targetSpeedInput = moveInput * (moveInput > 0 ? maxSpeed : reverseSpeed);
        Vector3 targetVelocity = transform.forward * targetSpeedInput;

        // Klíč k rychlosti: Lerpujeme celou velocity k cíli, ale zachováváme vertikální pohyb (gravitaci)
        float currentY = rb.linearVelocity.y;
        Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        newVelocity.y = currentY; // Aby auto neplulo, ale padalo gravitací
        
        rb.linearVelocity = newVelocity;
    }

    void ApplySteering()
    {
        // Detekce pohybu vpřed/vzad pro správné zatáčení
        // Používáme Local Velocity pro přesnost
        float localVelocityZ = transform.InverseTransformDirection(rb.linearVelocity).z;
        
        // Pokud stojíš, nezatáčíš. Pokud couváš, invertuje se to.
        float steerMultiplier = 1f;
        if (localVelocityZ < -0.1f) steerMultiplier = -1f;

        // Omezení zatáčení v nízkých rychlostech (volitelné, pro arkádu příjemné)
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 2f);

        float rotation = steerInput * turnSpeed * steerMultiplier * speedFactor * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotation, 0));
    }

    void ApplyLateralFriction()
    {
        // Tato část zabraňuje klouzání bokem
        Vector3 forwardVel = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVel = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        // Zde se děje drift: zachováme dopřednou rychlost a jen část boční
        rb.linearVelocity = forwardVel + (rightVel * driftFactor);
    }
}