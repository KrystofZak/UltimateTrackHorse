using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraOrbitControl : MonoBehaviour
{
    private CinemachineInputProvider inputProvider;
    private CinemachineFreeLook freeLookCamera;

    void Start()
    {
        // Najde potøebné komponenty na kameøe
        inputProvider = GetComponent<CinemachineInputProvider>();
        freeLookCamera = GetComponent<CinemachineFreeLook>();
    }

    void LateUpdate() // Pro kamery je lepší používat LateUpdate
    {
        // 1. OMEZENÍ NA LEVÉ TLAÈÍTKO MYŠI
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            inputProvider.enabled = true; // Zapne otáèení
        }
        else
        {
            inputProvider.enabled = false; // Vypne otáèení
        }

        // 2. OMEZENÍ ÚHLU (MANTINELY -45 AŽ 45 STUPÒÙ)
        if (freeLookCamera != null)
        {
            // Natvrdo drží hodnotu osy X v našem rozmezí
            freeLookCamera.m_XAxis.Value = Mathf.Clamp(freeLookCamera.m_XAxis.Value, -45f, 45f);
        }
    }
}