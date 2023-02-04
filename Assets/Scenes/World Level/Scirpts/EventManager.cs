using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    //[SerializeField] private TMP_Text fps_text;
    private InputActions inputActions;

    #region Events
    public static event Action jumpEvent;
    public static event Action<Vector2> touchMoved;
    public static event Action<float> dragHlEvent;
    public static event Action<Vector2> joystickMoveEvent;
    #endregion

    public void Awake()
    {
        inputActions = new InputActions();
    }

    public void OnEnable()
    {
        inputActions.Enable();
    }

    public void OnDisable()
    {
        inputActions.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (inputActions.girlPlayerControls.Jump.triggered) jumpEvent?.Invoke();
        if (TouchInputManager.drag_Hl) dragHlEvent?.Invoke(TouchInputManager.touch_delta.x);
        if(TouchInputManager.touch_in_R) touchMoved?.Invoke(TouchInputManager.touch_delta);
        joystickMoveEvent?.Invoke(inputActions.girlPlayerControls.Move.ReadValue<Vector2>());

    }

    //int fps = (int)(Time.frameCount / Time.time);
    //fps_text.text = "fps: " + fps.ToString();

}