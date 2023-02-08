using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TouchInputManager : MonoBehaviour
{
    [SerializeField] private RectTransform rect_R;
    [SerializeField] private RectTransform rect_L;

    static public bool touch_in_L;
    static public bool touch_in_R;

    static public Vector2 delta_L;
    static public Vector2 delta_R;

    [HideInInspector] public static bool drag_Hl;
    [HideInInspector] public static bool drag_Vl;

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
        // iterating over all the touches because there can be more than 1 touches on a smartphone screen
        foreach (Touch touch in Touch.activeTouches)
        {
            if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended || touch.isTap) {
                delta_L = Vector2.zero;
                delta_R = Vector2.zero;
                break;
            }
            // check whether the current touch is in the left side or the right side of the screen
            touch_in_L = RectTransformUtility.RectangleContainsScreenPoint(rect_L, touch.startScreenPosition);

            if (touch_in_L) // data of touch of left part of the screen
            {
                delta_L = touch.delta;
            }
            else // data of touch of right part of the screen
            {
                delta_R = touch.delta;
            }
        }
    }
}
