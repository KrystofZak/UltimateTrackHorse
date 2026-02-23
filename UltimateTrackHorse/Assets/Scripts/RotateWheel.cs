using UnityEngine;

public class RotateWheel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float rotationSpeed = 100f; // Rotation speed in degrees per second
    private float horizontalInput;
    private float rotationAngle;
    private float maxSteerAngle = 45f;
    void Start()
    {
        
    }

    void Update()
    {
      
        horizontalInput = Input.GetAxis("Horizontal");
        float targetAngle = horizontalInput * maxSteerAngle;
        rotationAngle = Mathf.MoveTowards(rotationAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.localEulerAngles = new Vector3(0, rotationAngle, 0);

    }
}
