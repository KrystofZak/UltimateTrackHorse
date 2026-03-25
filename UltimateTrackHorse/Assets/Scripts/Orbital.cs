using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraOrbitControl : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float minAngle = -45f;
    public float maxAngle = 45f;
    public float mouseSensitivity = 0.5f;
    public float returnSpeed = 5f;

    [Header("Teleport Fix")]
    public Transform targetCar;
    public float teleportDistance = 10f;

    private CinemachineFreeLook freeLookCamera;
    private Vector3 lastCarPosition;

    void Start()
    {
        freeLookCamera = GetComponent<CinemachineFreeLook>();
        if (targetCar != null)
        {
            lastCarPosition = targetCar.position;
        }
    }

    void Update()
    {
        if (freeLookCamera == null) return;

        if (targetCar != null)
        {
            if (Vector3.Distance(targetCar.position, lastCarPosition) > teleportDistance)
            {
                freeLookCamera.m_XAxis.Value = 0f;
                freeLookCamera.PreviousStateIsValid = false;
            }
            lastCarPosition = targetCar.position;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            freeLookCamera.m_XAxis.Value += mouseX * mouseSensitivity;
        }
        else
        {
            freeLookCamera.m_XAxis.Value = Mathf.Lerp(freeLookCamera.m_XAxis.Value, 0f, Time.deltaTime * returnSpeed);
        }

        freeLookCamera.m_XAxis.Value = Mathf.Clamp(freeLookCamera.m_XAxis.Value, minAngle, maxAngle);
    }
}