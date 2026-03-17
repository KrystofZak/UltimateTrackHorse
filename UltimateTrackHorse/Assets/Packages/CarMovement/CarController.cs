using System;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Serializable]
    public struct SurfaceSettings
    {
        public string name;
        public LayerMask layer;
        [Tooltip("Multiplier applied to acceleration on this surface (e.g. 0.6 for grass)")]
        public float accelerationMultiplier;
        [Tooltip("Multiplier applied to maxSpeed on this surface")]
        public float maxSpeedMultiplier;
        [Tooltip("Multiplier applied to dragCoefficient on this surface (higher = more sideways grip loss)")]
        public float dragCoefficientMultiplier;

        public static SurfaceSettings Default => new SurfaceSettings
        {
            name = "Default",
            accelerationMultiplier = 1f,
            maxSpeedMultiplier = 1f,
            dragCoefficientMultiplier = 1f
        };
    }

    [Header("Refrences")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivable;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private GameObject[] tires = new GameObject[4];
    [SerializeField] private TrailRenderer[] skidMarks = new TrailRenderer[2];
    [SerializeField] private ParticleSystem[] skidSmokes = new ParticleSystem[2];

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float damperStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    [Header("Input")]
    private float moveInput = 0;
    private float steerInput = 0;
    private bool isBraking = false;
    private bool preventReverse = false;
    public float reverseSpeedThreshold = 1f;

    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve steerCurve;
    [SerializeField] private float dragCoefficient = 1f;

    [Header("Surface Settings")]
    [Tooltip("Override car behaviour per surface layer. Layers not listed here use default multipliers (1.0).")]
    [SerializeField] private SurfaceSettings[] surfaceSettings = new SurfaceSettings[0];

    [Header("Visuals")]
    [SerializeField] private float tireRotationSpeed = 3000f;
    [SerializeField] private float minSkidVelocity = 10f;


    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio = 0f;

    private int[] wheelIsGrounded = new int[4];
    private bool isGrounded = false;

    private SurfaceSettings activeSurface = SurfaceSettings.Default;


    #region Unity Methods
    private void Start()
    {
        carRB = GetComponent<Rigidbody>();

        carRB.interpolation = RigidbodyInterpolation.Interpolate;
        carRB.collisionDetectionMode = CollisionDetectionMode.Continuous;

        carRB.linearDamping = 0.5f;
        carRB.angularDamping = 2f;

        carRB.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        Visuals();
        Vfx();
    }

    private void Update()
    {
        GetInput();
    }
    #endregion

    #region Movement

    private void Movement()
    {
        if (isGrounded)
        {
            HandleMotor();
            //Acceleration();
            //Deceleration();
            Steer();
            SidewaysDrag();
            LongitudinalDrag();
        }
    }
    private void Acceleration()
    {
        float effectiveAcceleration = acceleration * activeSurface.accelerationMultiplier;
        carRB.AddForceAtPosition(transform.forward * moveInput * effectiveAcceleration, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        float effectiveDeceleration = deceleration * activeSurface.accelerationMultiplier;
        carRB.AddForceAtPosition(-transform.forward * moveInput * effectiveDeceleration, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void HandleMotor()
    {
        float forwardSpeed = currentCarLocalVelocity.z;
        float effectiveAcceleration = acceleration * activeSurface.accelerationMultiplier;
        float effectiveDeceleration = deceleration * activeSurface.accelerationMultiplier;

        if (moveInput > 0.1f)
        {
            // Jízda vpøed
            if (forwardSpeed < -0.5f)
            {
                carRB.AddForceAtPosition(transform.forward * moveInput * effectiveDeceleration, accelerationPoint.position, ForceMode.Acceleration);
            }
            else
            {
                carRB.AddForceAtPosition(transform.forward * moveInput * effectiveAcceleration, accelerationPoint.position, ForceMode.Acceleration);
            }
        }
        else if (moveInput < -0.1f)
        {
            if (preventReverse)
            {
                // Pokud jsme se zamkli v módu brždìní, tak jen brzdíme nebo držíme na místì
                if (forwardSpeed > 0.5f)
                {
                    carRB.AddForceAtPosition(transform.forward * moveInput * effectiveDeceleration, accelerationPoint.position, ForceMode.Acceleration);
                }
                else
                {
                    BrakeToStop();
                }
            }
            else
            {
                // Zde jsme stiskli klávesu pøi malé rychlosti = mùžeme couvat jako s normálním plynem
                carRB.AddForceAtPosition(transform.forward * moveInput * effectiveAcceleration, accelerationPoint.position, ForceMode.Acceleration);
            }
        }
    
    }

    private void BrakeToStop()
    {
        
        float stoppingForce = -currentCarLocalVelocity.z * deceleration;
        carRB.AddForceAtPosition(transform.forward * stoppingForce, accelerationPoint.position, ForceMode.Acceleration);
    }
    private void LongitudinalDrag()
    {
        
        if (Mathf.Abs(moveInput) < 0.1f)
        {
            float dragForce = -currentCarLocalVelocity.z * (deceleration * 0.5f);
            carRB.AddForceAtPosition(transform.forward * dragForce, accelerationPoint.position, ForceMode.Acceleration);
        }
    }





    private void Steer()
    {
        
        float speedRatioAbs = Mathf.Abs(carVelocityRatio);

        float direction = currentCarLocalVelocity.z >= -0.1f ? 1f : -1f;

        carRB.AddTorque(steerStrength * steerInput * steerCurve.Evaluate(speedRatioAbs) * direction * transform.up, ForceMode.Acceleration);
    }

    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        float effectiveDrag = dragCoefficient * activeSurface.dragCoefficientMultiplier;
        float dragForceMagnitude = -currentSidewaysSpeed * effectiveDrag;

        Vector3 dragForce = transform.right * dragForceMagnitude;

        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }

    #endregion

    #region Visuals
    private void Visuals()
    {
        RotateTires();
    }
    private void RotateTires()
    {
        for (int i = 0; i < tires.Length; i++)
        {
            tires[i].transform.Rotate(Vector3.right, tireRotationSpeed * carVelocityRatio * Time.deltaTime, Space.Self);
        }
    }

    private void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        tire.transform.position = targetPosition;
    }

    private void Vfx()
    {
        if(isGrounded && currentCarLocalVelocity.x > minSkidVelocity)
        {
            ToggleSkidMarks(true);
            ToggleSkidSmokes(true);

        }
        else
        {
            ToggleSkidMarks(false);
            ToggleSkidSmokes(false);

        }
    }
    private void ToggleSkidMarks(bool toggle) 
    {
        foreach (TrailRenderer skid in skidMarks)
        {
            skid.emitting = toggle;
        }
    }
    private void ToggleSkidSmokes(bool toggle)
    {
        foreach (ParticleSystem skid in skidSmokes)
        {
            if (toggle && !skid.isPlaying) 
            {
                skid.Play();
            }
            else
            {
                skid.Stop();
            }
        }
    }

    #endregion

    #region Car Status Check
    private void GroundCheck()
    {
        int tempGroundedWheels = 0;

        for (int i = 0; i < wheelIsGrounded.Length; i++)
        {
            tempGroundedWheels += wheelIsGrounded[i];
        }

        if (tempGroundedWheels > 1)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        float effectiveMaxSpeed = maxSpeed * activeSurface.maxSpeedMultiplier;
        carVelocityRatio = currentCarLocalVelocity.z / effectiveMaxSpeed;
    }

    #endregion

    #region Input Handeling
    private void GetInput()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxisRaw("Horizontal");
        bool pressingBrake = moveInput < -0.1f;

        if (pressingBrake && !isBraking)
        {
            
            isBraking = true;
            if (currentCarLocalVelocity.z > reverseSpeedThreshold)
            {
                preventReverse = true;
            }
            else
            {
                preventReverse = false;
            }
        }
        else if (!pressingBrake)
        {
            
            isBraking = false;
            preventReverse = false;
        }
    }
    #endregion

    #region Surface Detection
    private SurfaceSettings GetSurfaceForLayer(int layer)
    {
        for (int i = 0; i < surfaceSettings.Length; i++)
        {
            if ((surfaceSettings[i].layer.value & (1 << layer)) != 0)
                return surfaceSettings[i];
        }
        return SurfaceSettings.Default;
    }
    #endregion

    #region Suspension methods
    private void Suspension()
    {
        int dominantLayer = -1;
        int mostGroundedIndex = -1;

        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;

            float maxDistance = restLength;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxDistance + wheelRadius))
            {
                int hitLayer = hit.collider.gameObject.layer;
                bool isDrivable = (drivable.value & (1 << hitLayer)) != 0;

                wheelIsGrounded[i] = 1;

                if (mostGroundedIndex == -1 || isDrivable)
                {
                    dominantLayer = hitLayer;
                    mostGroundedIndex = i;
                }

                float currentSpringLength = hit.distance - wheelRadius;

                float springCompression = (restLength - currentSpringLength) / springTravel;

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float damperForce = springVelocity * damperStiffness;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - damperForce;

                carRB.AddForceAtPosition(rayPoints[i].up * netForce, rayPoints[i].position);

                SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius);

                Debug.DrawLine(rayPoints[i].position, hit.point, isDrivable ? Color.red : Color.yellow);
            }
            else
            {
                wheelIsGrounded[i] = 0;

                SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * maxDistance);

                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position - rayPoints[i].up * maxDistance, Color.green);
            }
        }

        activeSurface = dominantLayer >= 0 ? GetSurfaceForLayer(dominantLayer) : SurfaceSettings.Default;
    }
    #endregion
}
