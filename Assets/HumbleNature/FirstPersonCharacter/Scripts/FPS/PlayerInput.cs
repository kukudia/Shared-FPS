using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerInput : MonoBehaviour
{
    public FixedJoystick moveJoystick;
    public FixedButton jumpButton;
    public FixedButton runButton;
    public FixedTouchField touchField;


    void Update()
    {
        var fps = GetComponent<RigidbodyFirstPersonController>();

        fps.RunAxis = (moveJoystick.Direction + new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"))).normalized;
        fps.JumpAxis = jumpButton.Pressed || Input.GetButton("Jump");

  
        fps.movementSettings.inputRun = runButton.Pressed || Input.GetButton("Fire1");

        float h = Input.GetAxis("Mouse X");
        float v = Input.GetAxis("Mouse Y");

        if (!Cursor.visible)
            fps.mouseLook.lookAxis = touchField.TouchDist + new Vector2(h, v);
        else
            fps.mouseLook.lookAxis = Vector2.zero;

    }
}
