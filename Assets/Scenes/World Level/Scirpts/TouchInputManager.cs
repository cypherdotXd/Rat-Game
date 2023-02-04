using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TouchInputManager : MonoBehaviour
{
    public static event Action swipeUpInL;
    public static event Action swipeDownInL;
    public static event Action swipeRightInL;
    public static event Action swipeLeftInL;
    public static event Action swipeUpInR;
    public static event Action swipeDownInR;
    public static event Action swipeRightInR;
    public static event Action swipeLeftInR;

    [SerializeField] private RectTransform rect_R;
    [SerializeField] private RectTransform rect_L;

    static public bool touch_in_L;
    static public bool touch_in_R;
    [HideInInspector] public static bool drag_Hl;
    [HideInInspector] public static bool drag_Vl;

    [HideInInspector] public static Vector2 touchDisplacement;
    [HideInInspector] public static Vector2 touch_delta;
    [HideInInspector] public static Vector2 touch_position_last_frame = Vector2.zero;

    protected void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    protected void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    public void Update()
    {
        // iterating over all the touches because there can be more than 1 touches on a mobile
        foreach (Touch touch in Touch.activeTouches)
        {
            // check whether the current touch is in the left side or the right side of the screen
            touch_in_L = RectTransformUtility.RectangleContainsScreenPoint(rect_L, touch.startScreenPosition);
            touch_in_R = RectTransformUtility.RectangleContainsScreenPoint(rect_R, touch.startScreenPosition);

            touch_in_L = false;
            if (touch_in_L)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    case TouchPhase.Moved:
                        touch_in_L = RectTransformUtility.RectangleContainsScreenPoint(rect_L, touch.startScreenPosition);
                        touch_in_R = RectTransformUtility.RectangleContainsScreenPoint(rect_R, touch.startScreenPosition);
                        // length of swipe in vertical direction
                        touchDisplacement.x = touch.startScreenPosition.x - touch.screenPosition.x;
                        touchDisplacement.y = touch.startScreenPosition.y - touch.screenPosition.y;

                        //drag in horizontal direction
                        drag_Hl = true;

                        //touch_delta = current_touch.screenPosition - touch_position_last_frame;
                        touch_delta = touch.delta;

                        //swipe up in left area
                        bool isSwipeUpInL = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) + 50 < Mathf.Abs(touchDisplacement.y));
                        if (isSwipeUpInL) swipeUpInL?.Invoke();

                        //swipe down in left area
                        bool isSwipeDownInL = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) + 50 < Mathf.Abs(touchDisplacement.y));
                        if (isSwipeDownInL) swipeDownInL?.Invoke();

                        // swipe right in left area
                        bool isSwipeRightInL = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) > Mathf.Abs(touchDisplacement.y) + 50);
                        if (isSwipeRightInL) swipeRightInL?.Invoke();

                        // swipe left in left area
                        bool isSwipeLeftInL = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) > Mathf.Abs(touchDisplacement.y) + 50);
                        if (isSwipeLeftInL) swipeLeftInL?.Invoke();
                        break;

                    case TouchPhase.Ended:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    case TouchPhase.Canceled:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    default:
                        break;
                }
            }
            if (touch_in_R)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    case TouchPhase.Moved:
                        touch_in_L = RectTransformUtility.RectangleContainsScreenPoint(rect_L, touch.startScreenPosition);
                        touch_in_R = RectTransformUtility.RectangleContainsScreenPoint(rect_R, touch.startScreenPosition);
                        // length of swipe in vertical direction
                        touchDisplacement.x = touch.startScreenPosition.x - touch.screenPosition.x;
                        touchDisplacement.y = touch.startScreenPosition.y - touch.screenPosition.y;

                        //drag in horizontal direction
                        drag_Hl = true;

                        //touch_delta = current_touch.screenPosition - touch_position_last_frame;
                        touch_delta = touch.delta;

                        //swipe up in right area
                        bool isSwipeUpInR = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) + 50 < Mathf.Abs(touchDisplacement.y));
                        if (isSwipeUpInR) swipeUpInR?.Invoke();

                        //swipe down in right area
                        bool isSwipeDownInR = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) + 50 < Mathf.Abs(touchDisplacement.y));
                        if (isSwipeDownInR) swipeDownInR?.Invoke();

                        // swipe right in right area
                        bool isSwipeRightInR = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) > Mathf.Abs(touchDisplacement.y) + 50);
                        if (isSwipeRightInR) swipeRightInR?.Invoke();

                        // swipe left in right area
                        bool isSwipeLeftInR = touchDisplacement.magnitude > 60 && (Mathf.Abs(touchDisplacement.x) > Mathf.Abs(touchDisplacement.y) + 50);
                        if (isSwipeLeftInR) swipeLeftInR?.Invoke();
                        break;

                    case TouchPhase.Ended:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    case TouchPhase.Canceled:
                        touch_delta = Vector2.zero;
                        drag_Hl = false;
                        break;

                    default:
                        break;
                }
            }
            
        }
    }
}
