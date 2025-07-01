using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{

    public VariableJoystick variableJoystick;
    public float SpinSpeed;
    public bool ReverseRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!ReverseRotation)
        {
            this.transform.Rotate(0, 0, variableJoystick.Horizontal * SpinSpeed);
        }
        else
        {
            this.transform.Rotate(0, 0, -variableJoystick.Horizontal * SpinSpeed);
        }
    }
}
