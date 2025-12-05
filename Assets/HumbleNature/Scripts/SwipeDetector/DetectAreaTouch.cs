using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class DetectAreaTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent OnHoldClick;
    public UnityEvent OnClickUp;

    private bool pointerDown;
    
    [HideInInspector]
    public bool pressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (pointerDown)
        {
            if (!pressed)
            {
                if (OnHoldClick != null)
                {
                    OnHoldClick.Invoke();
                    pressed = true;
                }

            }

        }
        else
        {
            if (pressed)
            {
                if (OnClickUp != null)
                {
                    OnClickUp.Invoke();
                    pressed = false;
                }
            }   
        }
        
    }
}
