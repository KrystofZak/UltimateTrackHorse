using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class Spedometer : MonoBehaviour
{
    public Rigidbody car;
    
    // Legacy support
    public TextMeshProUGUI speedText;

    private Label uiToolkitSpeedLabel;

    private void Start()
    {
        // Try resolving new UI Toolkit document
        var document = FindObjectOfType<UIDocument>();
        if (document != null && document.rootVisualElement != null)
        {
            uiToolkitSpeedLabel = document.rootVisualElement.Q<Label>("SpeedLabel");
        }
    }

    void Update()
    {
        if (car == null) return;

        float speed = car.linearVelocity.magnitude * 3.6f;
        string speedString = speed.ToString("0") + " km/h";

        if (speedText != null)
        {
            speedText.text = speedString;
        }

        if (uiToolkitSpeedLabel != null)
        {
            uiToolkitSpeedLabel.text = speedString;
        }
    }
}
