using UnityEngine;

namespace GameLogic.Ghost
{
    public class GhostReplayController : MonoBehaviour
    {
        private GhostLapData lapData;
        private float replayTimer;
        private int currentFrameIndex;
        private bool isPlaying;

        public void Play(GhostLapData data)
        {
            if (data?.frames == null || data.frames.Count < 2)
            {
                isPlaying = false;
                gameObject.SetActive(false);
                return;
            }

            lapData = data;
            replayTimer = 0f;
            currentFrameIndex = 0;
            isPlaying = true;
            gameObject.SetActive(true);

            transform.SetPositionAndRotation(
                lapData.frames[0].position,
                lapData.frames[0].rotation
            );
        }

        public void StopReplay()
        {
            isPlaying = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isPlaying || lapData == null)
                return;

            replayTimer += Time.deltaTime;

            if (replayTimer > lapData.lapTime)
            {
                StopReplay();
                return;
            }

            var frames = lapData.frames;

            while (currentFrameIndex < frames.Count - 2 &&
                   frames[currentFrameIndex + 1].time < replayTimer)
            {
                currentFrameIndex++;
            }

            GhostFrame a = frames[currentFrameIndex];
            GhostFrame b = frames[currentFrameIndex + 1];

            float segmentDuration = b.time - a.time;
            float t = segmentDuration > 0.0001f
                ? (replayTimer - a.time) / segmentDuration
                : 0f;

            Vector3 position = Vector3.Lerp(a.position, b.position, t);
            Quaternion rotation = Quaternion.Slerp(a.rotation, b.rotation, t);

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}