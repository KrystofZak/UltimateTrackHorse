using UnityEngine;

namespace UI
{
    /// <summary>
    /// Smoothly rotates the camera around a specified target to create a cinematic menu flyover effect.
    /// </summary>
    public class MenuCameraFlyover : MonoBehaviour
    {
        [Tooltip("The central object the camera should orbit around.")]
        public Transform focalPoint;

        [Tooltip("How fast the camera rotates in degrees per second.")]
        public float rotationSpeed = 5f;

        [Tooltip("Should the camera always look perfectly at the target while moving?")]
        public bool lookAtTarget = true;

        private void Update()
        {
            if (focalPoint != null)
            {
                // Move the camera in a circle around the focal point continuously
                transform.RotateAround(focalPoint.position, Vector3.up, rotationSpeed * Time.deltaTime);

                // Ensure the camera keeps framing the focal point
                if (lookAtTarget)
                {
                    transform.LookAt(focalPoint);
                }
            }
        }
    }
}