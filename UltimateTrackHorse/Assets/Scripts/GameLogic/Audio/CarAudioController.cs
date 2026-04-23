using UnityEngine;

namespace GameLogic.Audio
{
    [RequireComponent(typeof(Rigidbody))]
    public class CarAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private Rigidbody rb;

        [Header("Cues")]
        [SerializeField] private AudioCue engineLoopCue;
        [SerializeField] private AudioCue skidLoopCue;

        [Header("Engine Tuning")]
        [SerializeField] private float referenceTopSpeed = 18f;
        [SerializeField] private float idleVolume = 0.2f;
        [SerializeField] private float driveVolume = 0.9f;
        [SerializeField] private float idlePitch = 0.85f;
        [SerializeField] private float drivePitch = 1.6f;

        [Header("Skid Tuning")]
        [SerializeField] private float skidStartSidewaysSpeed = 1.5f;
        [SerializeField] private float skidMaxSidewaysSpeed = 6f;
        [SerializeField] private float skidMinPitch = 0.95f;
        [SerializeField] private float skidMaxPitch = 1.15f;

        private AudioSource engineSource;
        private AudioSource skidSource;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (audioManager == null) audioManager = FindAnyObjectByType<AudioManager>();
        }

        private void Start()
        {
            if (audioManager == null)
                return;

            if (engineLoopCue != null)
                engineSource = audioManager.CreateManagedLoop(engineLoopCue, transform, "EngineLoop");

            if (skidLoopCue != null)
                skidSource = audioManager.CreateManagedLoop(skidLoopCue, transform, "SkidLoop");
        }

        private void Update()
        {
            if (rb == null || audioManager == null)
                return;

            UpdateEngineAudio();
            UpdateSkidAudio();
        }

        private void OnDisable()
        {
            if (audioManager == null)
                return;

            if (engineSource != null)
                audioManager.StopManagedLoop(engineSource, 0.1f);

            if (skidSource != null)
                audioManager.StopManagedLoop(skidSource, 0.1f);
        }

        private void UpdateEngineAudio()
        {
            if (engineSource == null)
                return;

            float forwardSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward));
            float speed01 = Mathf.Clamp01(forwardSpeed / Mathf.Max(0.01f, referenceTopSpeed));

            float volume = Mathf.Lerp(idleVolume, driveVolume, speed01);
            float pitch = Mathf.Lerp(idlePitch, drivePitch, speed01);

            audioManager.SetManagedLoopState(engineSource, volume, pitch);
        }

        private void UpdateSkidAudio()
        {
            if (skidSource == null)
                return;

            float sidewaysSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.right));
            float skid01 = Mathf.InverseLerp(skidStartSidewaysSpeed, skidMaxSidewaysSpeed, sidewaysSpeed);

            float volume = skid01;
            float pitch = Mathf.Lerp(skidMinPitch, skidMaxPitch, skid01);

            audioManager.SetManagedLoopState(skidSource, volume, pitch);
        }
    }
}