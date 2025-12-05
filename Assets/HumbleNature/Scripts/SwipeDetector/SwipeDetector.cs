using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SwipeDetector : MonoBehaviour
{
    [Tooltip("Detect swipe with DetectAreaTouch.cs")]
    public bool useAreaDetector = false;

    public bool touchOnStart;

    [SerializeField]
    private float swipeThreshold = 0.2f;

    [SerializeField]
    private float mouseThreshold = 50f;

    private Vector2 fingerUp, fingerDown;

    public UnityEvent OnRight;
    public UnityEvent OnLeft;
    public UnityEvent OnUp;
    public UnityEvent OnDown;
    public UnityEvent OnTouch;
    public UnityEvent OnEndSwipe;

    public void StartDetection()
    {

#if UNITY_EDITOR

        if (touchOnStart)
        {
            OnTouch.Invoke();
        }


        fingerUp = Input.mousePosition;
        fingerDown = Input.mousePosition;
#endif
    }

    public void EndDetection()
    {

#if UNITY_EDITOR
        fingerDown = Input.mousePosition;
        DetectSwipeDirection(mouseThreshold);
#endif

    }

    void Update()
    {

        foreach (Touch touch in Input.touches)
        {

            if (touch.phase == TouchPhase.Began)
            {
                fingerUp = touch.position;
                fingerDown = touch.position;

                if (touchOnStart)
                {
                    OnTouch.Invoke();
                }
            }

            if (touch.phase == TouchPhase.Moved)
            {
                fingerDown = touch.position;

            }

            if (touch.phase == TouchPhase.Ended)
            {
                fingerDown = touch.position;

                if (!useAreaDetector)
                    DetectSwipeDirection(swipeThreshold);

            }
        }

        if (!useAreaDetector)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartDetection();
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDetection();
            }
        }


    }

    private void DetectSwipeDirection(float threshold)
    {
        var magnitudeAndDirection = fingerDown.x - fingerUp.x;
        bool isTouch = false;

        if (magnitudeAndDirection > (threshold * 2))
        {
            SwipeRight();
        }
        else if (magnitudeAndDirection < -(threshold * 2))
        {
            SwipeLeft();
        }
        else
        {
            isTouch = true;
        }

        magnitudeAndDirection = fingerDown.y - fingerUp.y;

        if (magnitudeAndDirection > (threshold))
        {
            isTouch = false;
            SwipeUp();
        }
        else if (magnitudeAndDirection < -(threshold))
        {
            isTouch = false;
            SwipeDown();
        }

        fingerUp = fingerDown;

        if (isTouch)
        {
            if (!touchOnStart)
            {
                if (Mathf.Abs(magnitudeAndDirection) < threshold)
                {
                    OnTouch.Invoke();
                }
            }

        }

        OnEndSwipe.Invoke();
    }


    public void SwipeLeft()
    {
        OnLeft.Invoke();
    }

    public void SwipeRight()
    {
        OnRight.Invoke();
    }

    public void SwipeUp()
    {
        OnUp.Invoke();
    }

    public void SwipeDown()
    {
        OnDown.Invoke();

    }

    public void Touch()
    {
        OnTouch.Invoke();

    }

}
