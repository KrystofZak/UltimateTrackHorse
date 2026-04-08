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
        public bool killMomentum;
        public float lingerTime;



        public static SurfaceSettings Default => new SurfaceSettings
        {
            name = "Default",
            accelerationMultiplier = 1f,
            maxSpeedMultiplier = 1f,
            dragCoefficientMultiplier = 1f,
            killMomentum = false,
            lingerTime = 0f
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
    [SerializeField] private AudioSource engineSound, skidSound;

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
    public bool isInputEnabled = true;
    public bool isPlaying = false;
    private bool wasPlaying = true;


    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve steerCurve;
    [SerializeField] private float dragCoefficient = 1f;


    [Header("Audio")]
    [SerializeField]
    [Range(0f, 1f)] private float minPitch = 1f;
    [SerializeField]
    [Range(1, 5)] private float maxPitch = 5f;
    private float pitchSmoothSpeed = 3f;
    [SerializeField] private float minSkidPitch = 0.5f;
    [SerializeField] private float maxSkidPitch = 1.5f;
    [SerializeField] private float skidIntensityRange = 15f;
    [SerializeField] private float skidSmoothSpeed = 10f;

    [Header("Surface Settings")]
    [Tooltip("Override car behaviour per surface layer. Layers not listed here use default multipliers (1.0).")]
    [SerializeField] private SurfaceSettings[] surfaceSettings = new SurfaceSettings[0];

    [Header("Acid Settings")]
    [SerializeField] private LayerMask acidLayer;
    [SerializeField] private float flatTireRadiusMultiplier = 0.5f;
    [SerializeField] private float steerPullPerFlatTire = 0.6f;
    [SerializeField] private float steerLossPerFrontFlat = 0.4f;
    [SerializeField] private float speedLossPerRearFlat = 0.35f;
    [SerializeField] private ParticleSystem[] acidVapors = new ParticleSystem[4];

    private bool[] isTireFlat = new bool[4];

    [Header("Visuals")]
    [SerializeField] private float tireRotationSpeed = 3000f;
    [SerializeField] private float minSkidVelocity = 10f;
    [SerializeField] private float maxVisualSteerAngle = 35f;
    [SerializeField] private float visualSteerSpeed = 10f;

    private float activeLingerTimer = 0f;
    private SurfaceSettings lingeringSurface;

    private float currentTireSpinAngle = 0f;
    private float currentVisualSteerAngle = 0f;

    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio = 0f;

    private int[] wheelIsGrounded = new int[4];
    private bool isGrounded = false;

    private SurfaceSettings activeSurface = SurfaceSettings.Default;


    private bool isStopping = false;
    private float lastMoveInput = 0;

    #region Unity Methods
    private void Start()
    {
        carRB = GetComponent<Rigidbody>();

        carRB.interpolation = RigidbodyInterpolation.Interpolate;
        carRB.collisionDetectionMode = CollisionDetectionMode.Continuous;

        carRB.linearDamping = 0.5f;
        carRB.angularDamping = 2f;

        carRB.centerOfMass = new Vector3(0, -0.5f, 0);
        if (engineSound != null)
        {
            engineSound.Play();
            engineSound.Pause();
        }
    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        Visuals();
        Vfx();
        if (isPlaying)
        {
            Audio();
        }

    }

    private void Update()
    {
        if (isInputEnabled)
        {
            GetInput();
        }
        else
        {
            moveInput = 0;
            steerInput = 0;
            isBraking = false;
        }

        if (isPlaying != wasPlaying)
        {
            wasPlaying = isPlaying;

            if (engineSound != null)
            {
                if (isPlaying)
                {

                    if (!engineSound.isPlaying) engineSound.Play();
                    else engineSound.UnPause();
                }
                else
                {
                    engineSound.Pause();
                }
            }

            if (skidSound != null)
            {
                if (isPlaying) skidSound.UnPause();
                else skidSound.Pause();
            }
        }

    }
    #endregion

    #region Movement

    private void Movement()
    {
        if (isGrounded)
        {
            EnforceSurfaceMaxSpeed();
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
        float acidSpeedMult = GetAcidSpeedMultiplier();

        float effectiveAcceleration = acceleration * activeSurface.accelerationMultiplier * acidSpeedMult;
        float effectiveDeceleration = deceleration * activeSurface.accelerationMultiplier;
        float effectiveMaxSpeed = maxSpeed * activeSurface.maxSpeedMultiplier * acidSpeedMult;

        if (moveInput > 0.1f)
        {
            if (forwardSpeed < -0.5f)
            {
                carRB.AddForceAtPosition(transform.forward * moveInput * (effectiveDeceleration * 3f), accelerationPoint.position, ForceMode.Acceleration);
            }
            else if (forwardSpeed < effectiveMaxSpeed)
            {
                float startAssist = (forwardSpeed < 1f) ? 1.5f : 1f;
                carRB.AddForceAtPosition(transform.forward * moveInput * effectiveAcceleration * startAssist, accelerationPoint.position, ForceMode.Acceleration);
            }
        }
        else if (moveInput < -0.1f)
        {
            if (preventReverse)
            {
                if (forwardSpeed > 0.5f)
                {
                    carRB.AddForceAtPosition(transform.forward * moveInput * (effectiveDeceleration * 1.5f), accelerationPoint.position, ForceMode.Acceleration);
                }
                else
                {
                    BrakeToStop();
                }
            }
            else
            {
                if (forwardSpeed > -effectiveMaxSpeed)
                {
                    carRB.AddForceAtPosition(transform.forward * moveInput * effectiveAcceleration, accelerationPoint.position, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            if (Mathf.Abs(forwardSpeed) < 1.0f)
            {
                carRB.linearVelocity = Vector3.Lerp(carRB.linearVelocity, new Vector3(0, carRB.linearVelocity.y, 0), Time.fixedDeltaTime * 10f);
            }
        }
    }

    private void LongitudinalDrag()
    {
        if (Mathf.Abs(moveInput) < 0.1f)
        {

            float dragForce = -currentCarLocalVelocity.z * (deceleration * 0.1f);
            carRB.AddForceAtPosition(transform.forward * dragForce, accelerationPoint.position, ForceMode.Acceleration);
        }
    }
    private void BrakeToStop()
    {
        float forwardSpeed = currentCarLocalVelocity.z;

        float stoppingForce = -forwardSpeed * (deceleration * 1.2f);
        carRB.AddForceAtPosition(transform.forward * stoppingForce, accelerationPoint.position, ForceMode.Acceleration);

        if (Mathf.Abs(forwardSpeed) < 0.2f)
        {
            Vector3 localVel = transform.InverseTransformDirection(carRB.linearVelocity);
            localVel.z = 0;
            carRB.linearVelocity = transform.TransformDirection(localVel);
        }
    }


    private void EnforceSurfaceMaxSpeed()
    {
        if (!activeSurface.killMomentum) return;

        float forwardSpeed = currentCarLocalVelocity.z;
        float effectiveMaxSpeed = maxSpeed * activeSurface.maxSpeedMultiplier * GetAcidSpeedMultiplier();

        if (forwardSpeed > effectiveMaxSpeed && forwardSpeed > 1f)
        {
            float overSpeedForce = (forwardSpeed - effectiveMaxSpeed) * (deceleration * 5f);
            carRB.AddForceAtPosition(-transform.forward * overSpeedForce, accelerationPoint.position, ForceMode.Acceleration);
        }
        else if (forwardSpeed < -effectiveMaxSpeed && forwardSpeed < -1f)
        {
            float overSpeedForce = (-forwardSpeed - effectiveMaxSpeed) * (deceleration * 5f);
            carRB.AddForceAtPosition(transform.forward * overSpeedForce, accelerationPoint.position, ForceMode.Acceleration);
        }
    }




    private void Steer()
    {
        float flatTireSteerBias = 0f;
        float currentSteerStrength = steerStrength;
        float leftSteerLoss = 0f;
        float rightSteerLoss = 0f;

        for (int i = 0; i < rayPoints.Length; i++)
        {
            if (isTireFlat[i])
            {
                Vector3 localWheelPos = transform.InverseTransformPoint(rayPoints[i].position);

                if (localWheelPos.x < 0) flatTireSteerBias -= steerPullPerFlatTire;
                else if (localWheelPos.x > 0) flatTireSteerBias += steerPullPerFlatTire;

                if (localWheelPos.z > 0)
                {
                    if (localWheelPos.x < 0) leftSteerLoss += steerLossPerFrontFlat;
                    else if (localWheelPos.x > 0) rightSteerLoss += steerLossPerFrontFlat;
                }
            }
        }

        if (steerInput < 0) currentSteerStrength *= Mathf.Clamp01(1f - leftSteerLoss);
        else if (steerInput > 0) currentSteerStrength *= Mathf.Clamp01(1f - rightSteerLoss);

        float speedRatioAbs = Mathf.Abs(carVelocityRatio);
        float direction = currentCarLocalVelocity.z >= -0.1f ? 1f : -1f;

        float finalSteerInput = Mathf.Clamp(steerInput + flatTireSteerBias, -1.5f, 1.5f);

        carRB.AddTorque(currentSteerStrength * finalSteerInput * steerCurve.Evaluate(speedRatioAbs) * direction * transform.up, ForceMode.Acceleration);
        if (Mathf.Abs(steerInput) < 0.1f)
        {

            if (Mathf.Abs(currentCarLocalVelocity.x) <= minSkidVelocity)
            {

                float currentAngularSpin = transform.InverseTransformDirection(carRB.angularVelocity).y;

                float counterTorque = -currentAngularSpin * 15f;
                carRB.AddTorque(transform.up * counterTorque, ForceMode.Acceleration);
            }
        }

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

        currentTireSpinAngle += tireRotationSpeed * carVelocityRatio * Time.deltaTime;

        float targetSteerAngle = steerInput * maxVisualSteerAngle;

        currentVisualSteerAngle = Mathf.Lerp(currentVisualSteerAngle, targetSteerAngle, Time.deltaTime * visualSteerSpeed);

        for (int i = 0; i < tires.Length; i++)
        {

            float applySteer = (i < 2) ? currentVisualSteerAngle : 0f;

            tires[i].transform.localRotation = Quaternion.Euler(currentTireSpinAngle, applySteer, 0f);
        }
    }

    private void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        tire.transform.position = targetPosition;
    }

    private void Vfx()
    {
        float sidewaysSpeed = Mathf.Abs(currentCarLocalVelocity.x);
        bool isSkidding = isGrounded && sidewaysSpeed > minSkidVelocity;

        ToggleSkidMarks(isSkidding);
        ToggleSkidSmokes(isSkidding);

        UpdateSkidSound(isSkidding, sidewaysSpeed);
    }

    private void ToggleSkidMarks(bool toggle)
    {
        foreach (TrailRenderer skid in skidMarks)
        {
            bool shouldEmit = toggle;

            if (toggle && IsWheelFlatNearEffect(skid.transform))
            {
                shouldEmit = false;
            }

            skid.emitting = shouldEmit;
        }
    }

    private void ToggleSkidSmokes(bool toggle)
    {
        foreach (ParticleSystem skid in skidSmokes)
        {
            bool shouldEmit = toggle;

            if (toggle && IsWheelFlatNearEffect(skid.transform))
            {
                shouldEmit = false;
            }

            var emission = skid.emission;
            emission.enabled = shouldEmit;

            if (shouldEmit && !skid.isPlaying)
            {
                skid.Play();
            }
        }
    }

    private bool IsWheelFlatNearEffect(Transform effectTransform)
    {
        int closestTireIndex = -1;
        float minDist = float.MaxValue;

        for (int i = 0; i < rayPoints.Length; i++)
        {
            float dist = Vector3.Distance(effectTransform.position, rayPoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closestTireIndex = i;
            }
        }

        if (closestTireIndex != -1)
        {
            return isTireFlat[closestTireIndex];
        }
        return false;
    }

    #endregion

    #region Audio
    private void Audio()
    {
        if (engineSound != null)
        {


            float currentRatio = Mathf.Abs(carVelocityRatio);

            float targetPitch = Mathf.Lerp(minPitch, maxPitch, currentRatio);

            engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * pitchSmoothSpeed);
        }
    }
    private void UpdateSkidSound(bool isSkidding, float sidewaysSpeed)
    {
        if (skidSound == null) return;

        if (isSkidding)
        {
            if (skidSound.mute) skidSound.mute = false;
            if (!skidSound.isPlaying) skidSound.Play();

            float maxSkidIntensitySpeed = minSkidVelocity + skidIntensityRange;
            float skidIntensity = Mathf.InverseLerp(minSkidVelocity, maxSkidIntensitySpeed, sidewaysSpeed);

            skidSound.volume = Mathf.Lerp(skidSound.volume, skidIntensity, Time.deltaTime * skidSmoothSpeed);

            float targetSkidPitch = Mathf.Lerp(minSkidPitch, maxSkidPitch, skidIntensity);
            skidSound.pitch = Mathf.Lerp(skidSound.pitch, targetSkidPitch, Time.deltaTime * skidSmoothSpeed);
        }
        else
        {

            skidSound.volume = Mathf.Lerp(skidSound.volume, 0f, Time.deltaTime * (skidSmoothSpeed * 1.5f));

            if (skidSound.volume < 0.05f && !skidSound.mute)
            {
                skidSound.mute = true;
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
    private float GetAcidSpeedMultiplier()
    {
        float multiplier = 1f;
        for (int i = 0; i < rayPoints.Length; i++)
        {
            if (isTireFlat[i])
            {

                Vector3 localWheelPos = transform.InverseTransformPoint(rayPoints[i].position);
                if (localWheelPos.z < 0)
                {
                    multiplier -= speedLossPerRearFlat;
                }
            }
        }
        return Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        float effectiveMaxSpeed = maxSpeed * activeSurface.maxSpeedMultiplier * GetAcidSpeedMultiplier();
        carVelocityRatio = currentCarLocalVelocity.z / effectiveMaxSpeed;
    }

    public void ResetCar()
    {
        carRB.linearVelocity = Vector3.zero;
        carRB.angularVelocity = Vector3.zero;

        isTireFlat = new bool[4];

        activeLingerTimer = 0f;
        activeSurface = SurfaceSettings.Default;

        for (int i = 0; i < tires.Length; i++)
        {
            if (tires[i] != null)
            {
                tires[i].transform.localScale = Vector3.one;
            }

            if (acidVapors.Length > i && acidVapors[i] != null)
            {
                acidVapors[i].Stop();
                acidVapors[i].Clear();
            }
        }
    }
    public float GetMaxSpeedOnAsphalt()
    {

        foreach (SurfaceSettings surface in surfaceSettings)
        {
            string sName = surface.name.ToLower();
            if (sName.Contains("asfalt") || sName.Contains("asphalt"))
            {
                return maxSpeed * surface.maxSpeedMultiplier;
            }
        }

        return maxSpeed;
    }

    #endregion

    #region Input Handeling
    private void GetInput()
    {
        float lastInput = moveInput;
        moveInput = Input.GetAxisRaw("Vertical");
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
            float maxDistance = restLength;
            float currentRadius = isTireFlat[i] ? wheelRadius * flatTireRadiusMultiplier : wheelRadius;

            RaycastHit[] hits = Physics.RaycastAll(rayPoints[i].position, -rayPoints[i].up, maxDistance + currentRadius, ~0, QueryTriggerInteraction.Collide);

            if (hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                int surfaceLayer = -1;
                bool foundDrivable = false;
                RaycastHit groundHit = new RaycastHit();

                foreach (RaycastHit hit in hits)
                {
                    int hitLayer = hit.collider.gameObject.layer;

                    if (surfaceLayer == -1 && (acidLayer.value & (1 << hitLayer)) == 0)
                    {
                        surfaceLayer = hitLayer;
                    }

                    if ((acidLayer.value & (1 << hitLayer)) != 0)
                    {
                        if (!isTireFlat[i])
                        {
                            isTireFlat[i] = true;
                            Debug.Log($"Wheel {i} is flat");

                            if (acidVapors.Length > i && acidVapors[i] != null)
                            {
                                acidVapors[i].Play();
                            }
                        }
                    }

                    if (!foundDrivable && (drivable.value & (1 << hitLayer)) != 0)
                    {
                        groundHit = hit;
                        foundDrivable = true;
                    }
                }

                if (foundDrivable)
                {
                    wheelIsGrounded[i] = 1;

                    if (mostGroundedIndex == -1 || foundDrivable)
                    {
                        dominantLayer = surfaceLayer;
                        mostGroundedIndex = i;
                    }

                    float currentSpringLength = groundHit.distance - currentRadius;
                    float springCompression = (restLength - currentSpringLength) / springTravel;

                    float springForce = springCompression * springStiffness;

                    if (springCompression > 0.7f)
                    {
                        float bumpFactor = Mathf.Pow(springCompression, 4f);
                        springForce += springStiffness * bumpFactor;
                    }

                    float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                    float damperForce = springVelocity * damperStiffness;

                    if (springVelocity < -1.5f && springCompression > 0.5f)
                    {
                        damperForce *= 2.5f;
                    }

                    float netForce = springForce - damperForce;

                    carRB.AddForceAtPosition(rayPoints[i].up * netForce, rayPoints[i].position);

                    SetTirePosition(tires[i], groundHit.point + rayPoints[i].up * currentRadius);

                    if (isTireFlat[i])
                    {
                        tires[i].transform.localScale = new Vector3(flatTireRadiusMultiplier, flatTireRadiusMultiplier, flatTireRadiusMultiplier);
                    }
                    else
                    {
                        tires[i].transform.localScale = Vector3.one;
                    }

                    Debug.DrawLine(rayPoints[i].position, groundHit.point, Color.red);
                }
                else
                {
                    wheelIsGrounded[i] = 0;
                    SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * maxDistance);
                    Debug.DrawLine(rayPoints[i].position, rayPoints[i].position - rayPoints[i].up * maxDistance, Color.green);
                }
            }
            else
            {
                wheelIsGrounded[i] = 0;
                SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * maxDistance);
                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position - rayPoints[i].up * maxDistance, Color.green);
            }
        }

        SurfaceSettings lastSettings = activeSurface;
        SurfaceSettings detectedSurface = dominantLayer >= 0 ? GetSurfaceForLayer(dominantLayer) : SurfaceSettings.Default;

        if (detectedSurface.lingerTime > 0f)
        {
            lingeringSurface = detectedSurface;
            activeLingerTimer = detectedSurface.lingerTime;
        }

        if (activeLingerTimer > 0f)
        {
            activeLingerTimer -= Time.fixedDeltaTime;
            activeSurface = lingeringSurface;
        }
        else
        {
            activeSurface = detectedSurface;
        }

        if (lastSettings.name != activeSurface.name)
        {
            Debug.Log($"Active Surface: {activeSurface.name}");
        }
    }
    #endregion
}
