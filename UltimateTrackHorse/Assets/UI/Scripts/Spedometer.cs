using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class Spedometer : MonoBehaviour
{
    public Rigidbody car;
    
    [Tooltip("The maximum speed (km/h) represented on the right side of the gauge.")]
    public float maxSpeedOnDial = 150f;
    
    // Legacy support
    public TextMeshProUGUI speedText;

    private Label uiToolkitSpeedLabel;
    private VisualElement uiToolkitNeedle;
    
    // Gives the needle a physical "weight" so it doesn't snap instantly, but visibly revs.
    private float currentDisplayedSpeed = 0f;

    private void Start()
    {
        // Try resolving new UI Toolkit document
        var document = FindObjectOfType<UIDocument>();
        if (document != null && document.rootVisualElement != null)
        {
            uiToolkitSpeedLabel = document.rootVisualElement.Q<Label>("SpeedLabel");
            uiToolkitNeedle = document.rootVisualElement.Q<VisualElement>("TachometerNeedle");
        }
    }

    void Update()
    {
        if (car == null) return;

        // Convert Unity physics velocity to km/h mathematically
        float targetSpeed = car.linearVelocity.magnitude * 3.6f;
        
        // Smoothly lerp towards the target speed so the dial acts like a heavy mechanical part
        currentDisplayedSpeed = Mathf.Lerp(currentDisplayedSpeed, targetSpeed, Time.deltaTime * 6f);
        
        // Snap the text to a whole number for clean readability
        string speedString = Mathf.FloorToInt(currentDisplayedSpeed).ToString("0") + " km/h";

        if (speedText != null)
        {
            speedText.text = speedString;
        }

        if (uiToolkitSpeedLabel != null)
        {
            uiToolkitSpeedLabel.text = speedString;
        }

        // Spin the physical UI needle!
        if (uiToolkitNeedle != null)
        {
            // Map the current speed to a rotational angle.
            // A half-circle dial runs exclusively from -90.0 degrees (pointing full left) to 90.0 degrees (pointing full right).
            float mappedAngle = Mathf.Lerp(-90f, 90f, currentDisplayedSpeed / maxSpeedOnDial);
            
            // Allow it to "over-rev" slightly so it looks cool if you drop down a huge hill, but clamp it before it spins into the ground.
            mappedAngle = Mathf.Clamp(mappedAngle, -95f, 95f); 

            // Apply rotation using advanced UI Toolkit Style properties
            uiToolkitNeedle.style.rotate = new StyleRotate(new Rotate(new Angle(mappedAngle, AngleUnit.Degree)));
        }
    }
}
