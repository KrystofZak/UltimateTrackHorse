using UnityEngine;
using UnityEngine.InputSystem;

public class CarMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float maxSpeed = 25f;
    
    [Header("Handling Settings")]
    [SerializeField] private float turnSpeed = 150f;
    [SerializeField] private float gripStrength = 0.95f;
    [SerializeField] private float driftGripStrength = 0.4f;
    [SerializeField] private float handling = 0.8f;
    
    [Header("Brake Settings")]
    [SerializeField] private float normalBrakeForce = 20f;
    [SerializeField] private float handBrakeForce = 35f;
    [SerializeField] private float handBrakeDriftMultiplier = 0.3f;
    
    [Header("Ground Settings")]
    [SerializeField] private float hoverHeight = 0.5f;
    [SerializeField] private float hoverForce = 300f;
    [SerializeField] private LayerMask groundLayer;
    
    private Rigidbody rb;
    private float currentSpeed;
    private float currentTurnAngle;
    private bool isDrifting = false;
    
    private Vector2 moveInput;
    private bool handBrakePressed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = 1000f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = false;
        
        if (groundLayer == 0)
        {
            groundLayer = ~0;
        }
    }

    void Update()
    {
        HandleInput();
    }
    
    void FixedUpdate()
    {
        ApplyHover();
        ApplyMovement();
        ApplyGrip();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnHandBrake(InputAction.CallbackContext context)
    {
        handBrakePressed = context.performed;
    }
    
    private void HandleInput()
    {
        float verticalInput = moveInput.y;
        float horizontalInput = moveInput.x;
        
        if (verticalInput > 0)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        else if (verticalInput < 0)
        {
            currentSpeed -= acceleration * 0.6f * Time.deltaTime;
        }
        else
        {
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                currentSpeed -= Mathf.Sign(currentSpeed) * deceleration * Time.deltaTime;
            }
            else
            {
                currentSpeed = 0f;
            }
        }
        
        if (handBrakePressed)
        {
            float brakeAmount = handBrakeForce * Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeAmount);
            isDrifting = Mathf.Abs(currentSpeed) > 3f && Mathf.Abs(horizontalInput) > 0.1f;
        }
        else if (verticalInput < -0.1f && currentSpeed > 0.5f)
        {
            float brakeAmount = normalBrakeForce * Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, currentSpeed > 0 ? 0 : -maxSpeed * 0.5f, brakeAmount);
            isDrifting = false;
        }
        else
        {
            isDrifting = false;
        }
        
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);
        
        if (Mathf.Abs(currentSpeed) > 0.5f)
        {
            float turnMultiplier = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            currentTurnAngle = horizontalInput * turnSpeed * handling * turnMultiplier;
        }
        else
        {
            currentTurnAngle = 0f;
        }
    }
    
    private void ApplyHover()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 10f, groundLayer))
        {
            float currentHeight = hit.distance;
            float heightDifference = hoverHeight - currentHeight;
            
            Vector3 force = Vector3.up * heightDifference * hoverForce;
            rb.AddForce(force, ForceMode.Force);
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.9f, rb.linearVelocity.z);
        }
    }
    
    private void ApplyMovement()
    {
        Vector3 forwardVelocity = transform.forward * currentSpeed;
        Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        
        rb.linearVelocity = new Vector3(
            forwardVelocity.x + sidewaysVelocity.x, 
            rb.linearVelocity.y, 
            forwardVelocity.z + sidewaysVelocity.z
        );
        
        if (Mathf.Abs(currentTurnAngle) > 0.1f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, currentTurnAngle * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
    
    private void ApplyGrip()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        
        float gripToUse = isDrifting ? driftGripStrength * handBrakeDriftMultiplier : gripStrength;
        
        Vector3 newVelocity = forwardVelocity + sidewaysVelocity * (1f - gripToUse);
        newVelocity.y = rb.linearVelocity.y;
        
        rb.linearVelocity = newVelocity;
    }
}
