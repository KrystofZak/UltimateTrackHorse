using UnityEngine;

namespace GameLogic
{
    public class Checkpoint : MonoBehaviour
    {
        public Transform respawnPoint; 
        [HideInInspector] public Quaternion correctRotation;
        private bool isActivated = false;
        
        // When reached finish line, reset checkpoint
        private void OnEnable()
        {
            FinishLine.OnPlayerFinished += ResetCheckpoint;
        }

        // When player leaves finish line, disable checkpoint reset
        private void OnDisable()
        {
            FinishLine.OnPlayerFinished -= ResetCheckpoint;
        }

        // Reset checkpoint to be ready for the next round
        public void ResetCheckpoint()
        {
            isActivated = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActivated && other.CompareTag("Player"))
            {
                isActivated = true;
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
                    gm.SetRespawnPoint(pos + new Vector3(0, 1f, 0), correctRotation);
                    gm.CheckpointCrossed();
                    Debug.Log("Checkpoint activated! New respawn point set.");
                }
            }
        }
    }
    
}
