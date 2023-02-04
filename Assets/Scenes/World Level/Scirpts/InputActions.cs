//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.4.3
//     from Assets/Scenes/World Level/Characters/Rat/Scripts/InputActions.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @InputActions : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputActions"",
    ""maps"": [
        {
            ""name"": ""ratControls"",
            ""id"": ""6e319b59-c005-4a0f-bae7-2c766302ab7f"",
            ""actions"": [
                {
                    ""name"": ""jump"",
                    ""type"": ""Button"",
                    ""id"": ""38a2321e-412c-47a7-980c-c61c042f6e77"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""look"",
                    ""type"": ""Value"",
                    ""id"": ""9fad903a-b31b-4836-9dd4-db1e01f09ba4"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""move"",
                    ""type"": ""PassThrough"",
                    ""id"": ""37a836a8-7b36-4635-b29d-929d6c9f510c"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""cf4d716b-7806-47b2-a96a-5f03130f7f9d"",
                    ""path"": ""<Touchscreen>/Press"",
                    ""interactions"": ""MultiTap"",
                    ""processors"": """",
                    ""groups"": ""Mobile"",
                    ""action"": ""jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""87d02957-adf6-4213-96f0-722de8d7c4f3"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ee334835-3914-4449-8815-c6ec918211bc"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""WS"",
                    ""id"": ""717f71c7-0f5a-4d92-82aa-9aee8203243b"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""a9551fdf-0f49-44e7-9310-b7ad3a1a3690"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mobile;PC"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""df13fc2e-7609-4955-9302-52fc306726aa"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mobile;PC"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""shiftSprint"",
                    ""id"": ""7fdbbde5-2317-4dd1-b819-82e44936fb81"",
                    ""path"": ""1DAxis(minValue=-3,maxValue=3)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""063adbc9-bd2d-423f-bdbc-1197f5fa9456"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mobile;PC"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""girlPlayerControls"",
            ""id"": ""ddbc593d-ac98-414c-bd94-a43d16fe31a9"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""PassThrough"",
                    ""id"": ""6205b3dd-187f-4645-8f2f-bf9a83b59fa2"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""2db75888-730c-468d-94a3-9e362ad6a049"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c7394006-5e2a-4ceb-b627-ab34065d267d"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector(WASD)"",
                    ""id"": ""b8cf2511-542c-4382-91a9-4e190652c0c7"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""6bd7ebf3-6d4c-4ea5-828d-72cc74ae7dfb"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""d0200aa2-e588-4cba-94d5-64cc49aa95fb"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""3887f885-a254-46b2-b493-041430e9c427"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""33385da2-2796-47f3-aaa1-3c6ddde6f33c"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""a2c264f2-0d52-4a77-894e-57b010f52e6a"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Mobile"",
            ""bindingGroup"": ""Mobile"",
            ""devices"": [
                {
                    ""devicePath"": ""<Touchscreen>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""PC"",
            ""bindingGroup"": ""PC"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": true,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // ratControls
        m_ratControls = asset.FindActionMap("ratControls", throwIfNotFound: true);
        m_ratControls_jump = m_ratControls.FindAction("jump", throwIfNotFound: true);
        m_ratControls_look = m_ratControls.FindAction("look", throwIfNotFound: true);
        m_ratControls_move = m_ratControls.FindAction("move", throwIfNotFound: true);
        // girlPlayerControls
        m_girlPlayerControls = asset.FindActionMap("girlPlayerControls", throwIfNotFound: true);
        m_girlPlayerControls_Move = m_girlPlayerControls.FindAction("Move", throwIfNotFound: true);
        m_girlPlayerControls_Jump = m_girlPlayerControls.FindAction("Jump", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // ratControls
    private readonly InputActionMap m_ratControls;
    private IRatControlsActions m_RatControlsActionsCallbackInterface;
    private readonly InputAction m_ratControls_jump;
    private readonly InputAction m_ratControls_look;
    private readonly InputAction m_ratControls_move;
    public struct RatControlsActions
    {
        private @InputActions m_Wrapper;
        public RatControlsActions(@InputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @jump => m_Wrapper.m_ratControls_jump;
        public InputAction @look => m_Wrapper.m_ratControls_look;
        public InputAction @move => m_Wrapper.m_ratControls_move;
        public InputActionMap Get() { return m_Wrapper.m_ratControls; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(RatControlsActions set) { return set.Get(); }
        public void SetCallbacks(IRatControlsActions instance)
        {
            if (m_Wrapper.m_RatControlsActionsCallbackInterface != null)
            {
                @jump.started -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnJump;
                @jump.performed -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnJump;
                @jump.canceled -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnJump;
                @look.started -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnLook;
                @look.performed -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnLook;
                @look.canceled -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnLook;
                @move.started -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnMove;
                @move.performed -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnMove;
                @move.canceled -= m_Wrapper.m_RatControlsActionsCallbackInterface.OnMove;
            }
            m_Wrapper.m_RatControlsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @jump.started += instance.OnJump;
                @jump.performed += instance.OnJump;
                @jump.canceled += instance.OnJump;
                @look.started += instance.OnLook;
                @look.performed += instance.OnLook;
                @look.canceled += instance.OnLook;
                @move.started += instance.OnMove;
                @move.performed += instance.OnMove;
                @move.canceled += instance.OnMove;
            }
        }
    }
    public RatControlsActions @ratControls => new RatControlsActions(this);

    // girlPlayerControls
    private readonly InputActionMap m_girlPlayerControls;
    private IGirlPlayerControlsActions m_GirlPlayerControlsActionsCallbackInterface;
    private readonly InputAction m_girlPlayerControls_Move;
    private readonly InputAction m_girlPlayerControls_Jump;
    public struct GirlPlayerControlsActions
    {
        private @InputActions m_Wrapper;
        public GirlPlayerControlsActions(@InputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_girlPlayerControls_Move;
        public InputAction @Jump => m_Wrapper.m_girlPlayerControls_Jump;
        public InputActionMap Get() { return m_Wrapper.m_girlPlayerControls; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GirlPlayerControlsActions set) { return set.Get(); }
        public void SetCallbacks(IGirlPlayerControlsActions instance)
        {
            if (m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnMove;
                @Jump.started -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnJump;
                @Jump.performed -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnJump;
                @Jump.canceled -= m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface.OnJump;
            }
            m_Wrapper.m_GirlPlayerControlsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Jump.started += instance.OnJump;
                @Jump.performed += instance.OnJump;
                @Jump.canceled += instance.OnJump;
            }
        }
    }
    public GirlPlayerControlsActions @girlPlayerControls => new GirlPlayerControlsActions(this);
    private int m_MobileSchemeIndex = -1;
    public InputControlScheme MobileScheme
    {
        get
        {
            if (m_MobileSchemeIndex == -1) m_MobileSchemeIndex = asset.FindControlSchemeIndex("Mobile");
            return asset.controlSchemes[m_MobileSchemeIndex];
        }
    }
    private int m_PCSchemeIndex = -1;
    public InputControlScheme PCScheme
    {
        get
        {
            if (m_PCSchemeIndex == -1) m_PCSchemeIndex = asset.FindControlSchemeIndex("PC");
            return asset.controlSchemes[m_PCSchemeIndex];
        }
    }
    public interface IRatControlsActions
    {
        void OnJump(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnMove(InputAction.CallbackContext context);
    }
    public interface IGirlPlayerControlsActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
    }
}
