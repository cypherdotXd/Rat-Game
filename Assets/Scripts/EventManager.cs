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
    public static event Action<float> joystickInputEvent;
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
        if (inputActions.ratControls.jump.triggered) jumpEvent?.Invoke();
        if(TouchInputManager.delta_R.magnitude > 0) touchMoved?.Invoke(TouchInputManager.delta_R);
        joystickInputEvent?.Invoke(inputActions.ratControls.move.ReadValue<float>());

    }

    //int fps = (int)(Time.frameCount / Time.time);
    //fps_text.text = "fps: " + fps.ToString();

}