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

    // Update is called once per frame
    void Update()
    {
        // Získání vstupu (-1 až 1 podle kláves A/D)
        horizontalInput = Input.GetAxis("Horizontal");

        // Vypoèítáme cílový úhel (napø. -1 * 45 = -45 stupòù)
        float targetAngle = horizontalInput * maxSteerAngle;

        // Plynulý pøesun aktuálního úhlu smìrem k cílovému úhlu
        rotationAngle = Mathf.MoveTowards(rotationAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // Aplikování rotace na objekt. 
        // POZNÁMKA: Pokud se ti kolo toèí ve špatné ose, uprav Vector3.
        // (0, rotationAngle, 0) otáèí kolo do stran jako u auta (osa Y).
        // (0, 0, -rotationAngle) by otáèelo kolem jako volantem (osa Z).
        transform.localEulerAngles = new Vector3(0, rotationAngle, 0);

    }
}
