using UnityEngine;
using System;

namespace GameLogic
{
    /// <summary>
    /// Class that represents the finish line in the game.
    /// </summary>
    public class FinishLine : MonoBehaviour
    {
        // Game event that is triggered when the player reaches the finish line
        public static event Action OnPlayerFinished;

        // Triggered when the player enters the collider of the finish line
        void OnTriggerEnter(Collider other)
        {
            // Check if the collider belongs to the player
            if (other.CompareTag("Player"))
            {
                OnPlayerFinished?.Invoke();
            }
        }
    }
}
