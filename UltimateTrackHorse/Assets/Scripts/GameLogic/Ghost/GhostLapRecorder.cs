using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Ghost
{
    public class GhostLapRecorder : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float sampleInterval = 0.05f;

        private readonly List<GhostFrame> frames = new();
        private float lapTimer;
        private float sampleTimer;
        private bool isRecording;

        public bool IsRecording => isRecording;

        public void BeginRecording()
        {
            frames.Clear();
            lapTimer = 0f;
            sampleTimer = 0f;
            isRecording = true;

            RecordFrame(); // capture start immediately
        }

        public GhostLapData EndRecording(string mapId)
        {
            if (!isRecording)
                return null;

            RecordFrame(); // final frame
            isRecording = false;

            return new GhostLapData(mapId, lapTimer, new List<GhostFrame>(frames));
        }

        private void Update()
        {
            if (!isRecording || !playerTransform)
                return;

            float dt = Time.deltaTime;
            lapTimer += dt;
            sampleTimer += dt;

            if (sampleTimer >= sampleInterval)
            {
                sampleTimer = 0f;
                RecordFrame();
            }
        }

        public void SetPlayerTransform(Transform target)
        {
            playerTransform = target;
        }

        private void RecordFrame()
        {
            frames.Add(new GhostFrame(
                lapTimer,
                playerTransform.position,
                playerTransform.rotation
            ));
        }
    }
}