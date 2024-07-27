using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TouchInputManager : MonoBehaviour
{
    [SerializeField] private RectTransform _rectR;
    [SerializeField] private RectTransform _rectL;

    static private bool _isTouchInL;
    static private bool _isTouchInR;

    static public Vector2 DeltaL { get; private set; }
    static public Vector2 DeltaR { get; private set; }

    static public Vector2 DistanceL;
    static public Vector2 DistanceR;

    static public Vector2 StartPositionL { get; private set; }
    static public Vector2 StartPositionR { get; private set; }

    public static PlayerControls.RatControlsActions InputMain;

    private void Awake()
    {
        InputMain = new PlayerControls().ratControls;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        InputMain.Enable();
    }

    private void OnDisable()
    {

        InputMain.Disable();
        EnhancedTouchSupport.Disable();
    }

    public void Update()
    {
        // iterating over all the touches
        foreach (Touch touch in Touch.activeTouches)
        {

            if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended || touch.isTap)
            {
                DeltaL = Vector2.zero;
                DeltaR = Vector2.zero;
                break;
            }

            // is current touch left or right
            _isTouchInL = RectTransformUtility.RectangleContainsScreenPoint(_rectL, touch.startScreenPosition);

            // differ b/w left finger and right finger touches
            if (_isTouchInL)
            {
                DeltaL = touch.delta;
                DistanceL = touch.startScreenPosition - touch.startScreenPosition;
            }
            else
            {
                DeltaR = touch.delta;
                DistanceR = touch.startScreenPosition - touch.startScreenPosition;
            }
        }
    }
}