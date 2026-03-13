using TMPro;
using UnityEngine;
public class Spedometer : MonoBehaviour
{
    public Rigidbody car;
    public TextMeshProUGUI speedText;

    void Update()
    {
        float speed = car.linearVelocity.magnitude * 3.6f;
        speedText.text = speed.ToString("0") + " km/h";
    }
}
