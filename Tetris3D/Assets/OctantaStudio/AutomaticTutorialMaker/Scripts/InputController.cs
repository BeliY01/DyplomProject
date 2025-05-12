using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AutomaticTutorialMaker
{
    public class InputController : MonoBehaviour
    {
        #region Variables
        [Grayed] public TutorialSceneReferences sceneReferences; // References to scene components
        [ReadOnly] public bool isJoystickConnected = false;
        public enum InputSystemType { Old, New }
        public InputSystemType inputSystemType;
        [Header("Input Settings")]
        public float minHoldDuration = 0.5f; // Minimum duration for hold gesture detection
        public float minDragDistance = 0.5f; // Minimum distance for drag gesture detection
        private static InputController instance; // Singleton instance
        public bool IsMobileDevice { get; private set; } // Device type flag
        private Vector2 lastScrollPosition; // Last recorded scroll position

        private HashSet<KeyCode> currentlyPressedKeys = new HashSet<KeyCode>(); // Currently pressed keys
        private List<KeyCode> lastPressedKeys = new List<KeyCode>(); // Previously pressed keys

        public float minSwipeDistance = 50f;    // Minimum distance for swipe detection
        public float maxSwipeTime = 0.5f;       // Maximum duration for swipe detection 
        public float swipeAngleTolerance = 30f; // Angle tolerance for swipe direction
        private float lastDPadHorizontal;
        private float lastDPadVertical;
        private float lastLT;
        private float lastRT;
        private float lastLeftStickX;
        private float lastLeftStickY;
        private float lastRightStickX;
        private float lastRightStickY;

        private float halfValue = 0.5f;

        private bool isPinching = false;
        private float pinchStartDistance = 0f;
        private float pinchCurrentDistance = 0f;
        private float pinchStartAngle = 0f;
        private float pinchCurrentAngle = 0f;
        private Vector2 pinchStartCenter = Vector2.zero;
        private Vector2 pinchCurrentCenter = Vector2.zero;
        private bool isMultitouchActive = false;

        [Header("Pinch Settings")]
        public float minPinchDistanceChange = 50f; 
        public float minPinchRotationAngle = 15f;  
        private InteractionTypeEnum? currentPinchType = null;

        [Header("Joystick Axes (Old System)")]
        [SerializeField] private string DpadHorizontalAxis = "Debug Horizontal";
        [SerializeField] private string DpadVerticalAxis = "Debug Vertical";
        [SerializeField] private string TriggerAxis = "Triggers";
        [SerializeField] private string LeftStickHorizontalAxis = "LeftStickHorizontal";
        [SerializeField] private string LeftStickVerticalAxis = "LeftStickVertical";
        [SerializeField] private string RightStickHorizontalAxis = "RightStickHorizontal";
        [SerializeField] private string RightStickVerticalAxis = "RightStickVertical";

        [System.Serializable]
        public struct ButtonMapping
        {
            public string buttonName;
            public int buttonIndex;
        }

        [Header("Joystick Mapping")]
        [Tooltip("Configure button mappings for different gamepad models.\n" +
         "Map button names to their specific index numbers for proper detection.")]
        public ButtonMapping[] buttonMappings;

        // Returns singleton instance of input controller
        public static InputController Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("InputController");
                    instance = go.AddComponent<InputController>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Initialization
        // Initializes input controller and determines device type
        public void Initialize()
        {
#if UNITY_EDITOR
            IsMobileDevice = UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld;
#else
    IsMobileDevice = Application.platform == RuntimePlatform.Android || 
                    Application.platform == RuntimePlatform.IPhonePlayer;
#endif

            if (inputSystemType == InputSystemType.New)
            {
#if ENABLE_INPUT_SYSTEM
       
#else
                Debug.LogError("[ATM] New input system is selected, but ENABLE_INPUT_SYSTEM is not defined! " +
                               "Ensure you have enabled the new input system in Player Settings.");
                inputSystemType = InputSystemType.Old;
#endif
            }

            Debug.Log($"[ATM] Input Controller initialized. Device type: {(IsMobileDevice ? "Mobile" : "Desktop")}.");

            if (IsJoystickConnected())
            {
                GamePadReset();
                isJoystickConnected = true;
                Debug.Log("[ATM] Gamepad initialized.");
            }
        }

        #endregion

        #region DefaultInputDetection
        // Detects and classifies swipe gestures based on input
        public InteractionTypeEnum? DetectSwipe(Vector2 startPos, Vector2 endPos, float duration)
        {
            if (duration > maxSwipeTime) return null;

            float distance = Vector2.Distance(startPos, endPos);
            if (distance < minSwipeDistance) return null;

            Vector2 direction = (endPos - startPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (angle < 0) angle += 360;

            if (IsAngleInRange(angle, 0, swipeAngleTolerance) ||
                IsAngleInRange(angle, 360 - swipeAngleTolerance, 360))
                return InteractionTypeEnum.SwipeRight;

            if (IsAngleInRange(angle, 180 - swipeAngleTolerance, 180 + swipeAngleTolerance))
                return InteractionTypeEnum.SwipeLeft;

            if (IsAngleInRange(angle, 90 - swipeAngleTolerance, 90 + swipeAngleTolerance))
                return InteractionTypeEnum.SwipeUp;

            if (IsAngleInRange(angle, 270 - swipeAngleTolerance, 270 + swipeAngleTolerance))
                return InteractionTypeEnum.SwipeDown;

            return null;
        }

        // Checks if angle is within specified range
        private bool IsAngleInRange(float angle, float start, float end)
        {
            return angle >= start && angle <= end;
        }

        // Checks if input has just started
        public bool GetInputDown()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.leftButton.wasPressedThisFrame ||
                       (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            }
#endif

            return false;
        }

        // Checks if input has just ended
        public bool GetInputUp()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Ended);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.leftButton.wasReleasedThisFrame ||
                       (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
            }
#endif
            return false;
        }

        // Checks for right mouse button down
        public bool GetRightClickDown()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonDown(1);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.rightButton.wasPressedThisFrame;
            }
#endif
            return false;
        }

        // Checks for right mouse button up
        public bool GetRightClickUp()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonUp(1);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.rightButton.wasReleasedThisFrame;
            }
#endif
            return false;
        }

        // Checks if input continues
        public bool GetInputL()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Moved);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.leftButton.isPressed ||
                       (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
            }
#endif
            return false;
        }

        // Checks if input continues
        public bool GetInputR()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButton(1) || (Input.touchCount > 1 && Input.GetTouch(1).phase == UnityEngine.TouchPhase.Moved);
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.rightButton.isPressed ||
                       (Touchscreen.current != null && Touchscreen.current.touches.Count > 1 && Touchscreen.current.touches[1].press.isPressed);
            }
#endif
            return false;
        }

        // Checks if cursor is within game view window
        private bool IsCursorInGameView()
        {
#if UNITY_EDITOR
            Vector2 mousePosition = GetInputPosition();
            return mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
                   mousePosition.y >= 0 && mousePosition.y <= Screen.height &&
                   UnityEditor.EditorWindow.mouseOverWindow != null &&
                   UnityEditor.EditorWindow.mouseOverWindow.GetType().ToString().Contains("GameView");
#else
        return true;
#endif
        }
        public float GetScrollDelta()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.mouseScrollDelta.y;
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                return Mouse.current.scroll.ReadValue().y;
            }
#endif
            return 0f;
        }

        // Checks for upward scroll input
        public bool IsScrollingUp()
        {
            return GetScrollDelta() > 0;
        }

        // Checks for downward scroll input
        public bool IsScrollingDown()
        {
            return GetScrollDelta() < 0;
        }

        // Gets current input position
        public Vector2 GetInputPosition()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                if (Input.touchCount > 0)
                    return Input.GetTouch(0).position;
                return Input.mousePosition;
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    return Touchscreen.current.primaryTouch.position.ReadValue();
                return Mouse.current.position.ReadValue();
            }
#endif
            return Vector2.zero;
        }

        // Returns clicked object and its type through raycast
        public GameObject GetClickedObject(out ObjectTypeEnum objectType)
        {
            Vector2 inputPosition = GetInputPosition();
            objectType = ObjectTypeEnum.None;

            bool isPointerOverUI = false;

            if (inputSystemType == InputSystemType.Old)
            {
                isPointerOverUI = EventSystem.current.IsPointerOverGameObject() ||
                                 (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId));
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                {
                    int pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                    isPointerOverUI |= EventSystem.current.IsPointerOverGameObject(pointerId);
                }
            }
#endif

            if (isPointerOverUI)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = inputPosition;
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0)
                {
                    objectType = ObjectTypeEnum.UI;
                    return results[0].gameObject;
                }
            }

            Ray ray = sceneReferences.mainCamera.ScreenPointToRay(inputPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                objectType = ObjectTypeEnum._3D;
                return hit.collider.gameObject;
            }

            Ray ray2d = Camera.main.ScreenPointToRay(inputPosition);
            RaycastHit2D hit2d = Physics2D.Raycast(ray2d.origin, ray2d.direction);
            if (hit2d)
            {
                objectType = ObjectTypeEnum._2D;
                return hit2d.collider.gameObject;
            }

            return null;
        }

        // Calculates duration of current input
        public float GetInputDuration(float startTime)
        {
            return Time.time - startTime;
        }

        // Checks if input duration qualifies as hold gesture
        public bool IsHoldGesture(float duration)
        {
            return duration > minHoldDuration;
        }

        // Checks if input qualifies as drag gesture
        public bool IsDragGesture(GameObject startObject, GameObject endObject, float duration)
        {
            return duration > minHoldDuration && startObject != null && endObject != null && startObject != endObject;
        }

        // Checks for keyboard input
        public bool IsKeyboardInput()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return KeyboardInputHelper.GetAnyKeyDown();
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return false;

                foreach (var key in keyboard.allKeys)
                {
                    if (key.wasPressedThisFrame)
                        return true;
                }
            }
#endif
            return false;
        }


        // Returns list of currently pressed keys
        public List<KeyCode> GetCurrentPressedKeys()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return KeyboardInputHelper.GetPressedKeys();
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                List<KeyCode> pressedKeys = new List<KeyCode>();
                var keyboard = Keyboard.current;

                if (keyboard == null)
                {
                    Debug.LogWarning("[ATM] Keyboard not found in the new Input System.");
                    return pressedKeys;
                }

                var allKeys = KeyboardInputHelper.GetAllAvailableKeys();

                foreach (KeyCode key in allKeys)
                {
                    try
                    {
                        Key systemKey = key.ToKey();

                        if (systemKey != Key.None && keyboard[systemKey].wasPressedThisFrame)
                        {
                            pressedKeys.Add(key);
                        }
                    }
                    catch (System.ArgumentException)
                    {
                        Debug.LogWarning($"[ATM] Key {key} is not supported in the new Input System.");
                        continue;
                    }
                }

                return pressedKeys;
            }
#endif

            return new List<KeyCode>();
        }

        // Returns list of released keys
        public List<KeyCode> GetReleasedKeys()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return KeyboardInputHelper.GetReleasedKeys();
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                List<KeyCode> releasedKeys = new List<KeyCode>();
                var keyboard = Keyboard.current;

                if (keyboard == null)
                {
                    Debug.LogWarning("[ATM] Keyboard not found in the new Input System.");
                    return releasedKeys;
                }

                var allKeys = KeyboardInputHelper.GetAllAvailableKeys();

                foreach (KeyCode key in allKeys)
                {
                    try
                    {
                        Key systemKey = key.ToKey();

                        if (systemKey != Key.None && keyboard[systemKey].wasReleasedThisFrame)
                        {
                            releasedKeys.Add(key);
                        }
                    }
                    catch (System.ArgumentException)
                    {
                        Debug.LogWarning($"[ATM] Key {key} is not supported in the new Input System.");
                        continue;
                    }
                }

                return releasedKeys;
            }
#endif

            return new List<KeyCode>();
        }


        // Updates current keyboard state
        public void UpdateKeyboardState()
        {
            currentlyPressedKeys.Clear();

            if (inputSystemType == InputSystemType.Old)
            {
                foreach (KeyCode key in KeyboardInputHelper.GetAllAvailableKeys())
                {
                    if (Input.GetKey(key))
                    {
                        currentlyPressedKeys.Add(key);
                    }
                }
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    foreach (KeyCode key in KeyboardInputHelper.GetAllAvailableKeys())
                    {
                        try
                        {
                            Key systemKey = key.ToKey();
                            if (systemKey != Key.None && keyboard[systemKey].isPressed)
                            {
                                currentlyPressedKeys.Add(key);
                            }
                        }
                        catch (ArgumentException)
                        {
                            Debug.LogWarning($"[ATM] Key {key} is not supported in the new Input System.");
                            continue;
                        }
                    }
                }
            }
#endif
        }


        #endregion

        #region Pinch Gesture Detection

        public bool IsMultitouchActive()
        {
            int touchCount = 0;

#if ENABLE_INPUT_SYSTEM
            if (inputSystemType == InputSystemType.New)
            {
                var touchscreen = Touchscreen.current;
                if (touchscreen != null)
                {
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var touch = touchscreen.touches[i];
                        {
                            if (touch.press.isPressed)
                            {
                                touchCount++;
                            }
                        }
                    }
                }
            }
#else
if (inputSystemType == InputSystemType.Old)
            {
                touchCount = Input.touchCount;
            }
#endif

            isMultitouchActive = touchCount >= 2;
            return isMultitouchActive;
        }

        public bool IsPinching()
        {
            return isPinching;
        }

        // Gets the current pinch gesture type (PinchIn, PinchOut, PinchRotate)
        public InteractionTypeEnum? GetCurrentPinchType()
        {
            return currentPinchType;
        }

        // Resets the current pinch gesture
        public void ResetPinchGesture()
        {
            isPinching = false;
            currentPinchType = null;
        }

        // Updates the pinch gesture state - called in Update
        public void UpdatePinchGesture()
        {        

            if (!IsMultitouchActive())
            {
                if (isPinching)
                {
                    isPinching = false;
                }
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (inputSystemType == InputSystemType.New)
            {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.touches.Count >= 2)
        {
            var touch0 = touchscreen.touches[0];
            var touch1 = touchscreen.touches[1];
            
            bool touch0Active = touch0.press.isPressed;
            bool touch1Active = touch1.press.isPressed;
            
            if (touch0Active && touch1Active)
            {
                pinchCurrentCenter = (touch0.position.ReadValue() + touch1.position.ReadValue()) * halfValue;
                pinchCurrentDistance = Vector2.Distance(touch0.position.ReadValue(), touch1.position.ReadValue());
                Vector2 direction = touch1.position.ReadValue() - touch0.position.ReadValue();
                pinchCurrentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if ((touch0.press.wasPressedThisFrame || touch1.press.wasPressedThisFrame) && !isPinching)
                {
                    isPinching = true;
                    pinchStartDistance = pinchCurrentDistance;
                    pinchStartCenter = pinchCurrentCenter;
                    pinchStartAngle = pinchCurrentAngle;
                    currentPinchType = null; 
                }
                else if (isPinching && currentPinchType == null)
                {
                    DeterminePinchType();
                }
            }
            else if (isPinching && (!touch0Active || !touch1Active))
            {
                if (currentPinchType == null)
                {
                    DeterminePinchType();
                }
                isPinching = false;
            }
        }
        else if (isPinching)
        {
            isPinching = false;
        }
    }
#else
            if (inputSystemType == InputSystemType.Old)
            {
                if (Input.touchCount == 2)
                {
                    Touch touch0 = Input.GetTouch(0);
                    Touch touch1 = Input.GetTouch(1);

                    pinchCurrentCenter = (touch0.position + touch1.position) * halfValue;
                    pinchCurrentDistance = Vector2.Distance(touch0.position, touch1.position);
                    Vector2 direction = touch1.position - touch0.position;
                    pinchCurrentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    if ((touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began) && !isPinching)
                    {
                        isPinching = true;
                        pinchStartDistance = pinchCurrentDistance;
                        pinchStartCenter = pinchCurrentCenter;
                        pinchStartAngle = pinchCurrentAngle;
                        currentPinchType = null; 
                    }
                    else if (isPinching && currentPinchType == null && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
                    {
                        DeterminePinchType();
                    }
                    else if (isPinching && (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended ||
                             touch0.phase == TouchPhase.Canceled || touch1.phase == TouchPhase.Canceled))
                    {
                        if (currentPinchType == null)
                        {
                            DeterminePinchType();
                        }
                        isPinching = false;
                    }
                }
                else if (isPinching)
                {
                    isPinching = false;
                }
            }
#endif
        }

        // Determines the type of pinch gesture based on distance and angle changes
        private void DeterminePinchType()
        {
            float distanceDelta = pinchCurrentDistance - pinchStartDistance;
            float angleDelta = Mathf.DeltaAngle(pinchStartAngle, pinchCurrentAngle);

            if (Mathf.Abs(angleDelta) > minPinchRotationAngle)
            {
                currentPinchType = InteractionTypeEnum.PinchRotate;
            }
            else if (distanceDelta < -minPinchDistanceChange)
            {
                currentPinchType = InteractionTypeEnum.PinchIn;
            }
            else if (distanceDelta > minPinchDistanceChange)
            {
                currentPinchType = InteractionTypeEnum.PinchOut;
            }
        }

        // Gets the pinch scale (0 if pinch is not active)
        public float GetPinchScale()
        {
            if (!isPinching) return 0f;
            return pinchCurrentDistance / pinchStartDistance;
        }

        // Gets the pinch rotation angle (0 if pinch is not active)
        public float GetPinchRotation()
        {
            if (!isPinching) return 0f;
            return Mathf.DeltaAngle(pinchStartAngle, pinchCurrentAngle);
        }
        #endregion

        #region Joystick Input

        // Initialization
        public bool IsJoystickConnected()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                string[] joystickNames = Input.GetJoystickNames();
                return joystickNames.Length > 0 && !string.IsNullOrEmpty(joystickNames[0]);
            }
#if ENABLE_INPUT_SYSTEM
    else
    {
        return Gamepad.current != null;
    }
#endif
            return false;
        }

        public void GamePadReset()
        {
#if ENABLE_INPUT_SYSTEM
        if (inputSystemType == InputSystemType.New)
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            lastDPadHorizontal = gamepad.dpad.x.ReadValue();
            lastDPadVertical = gamepad.dpad.y.ReadValue();
            lastLeftStickX = gamepad.leftStick.x.ReadValue();
            lastLeftStickY = gamepad.leftStick.y.ReadValue();
            lastRightStickX = gamepad.rightStick.x.ReadValue();
            lastRightStickY = gamepad.rightStick.y.ReadValue();
            lastLT = gamepad.leftTrigger.ReadValue();
            lastRT = gamepad.rightTrigger.ReadValue();
        }
    }
#else
            if (inputSystemType == InputSystemType.Old)
            {
                if (DpadHorizontalAxis != String.Empty && DpadVerticalAxis != String.Empty)
                {
                    float dpadH = Input.GetAxisRaw(DpadHorizontalAxis);
                    float dpadV = Input.GetAxisRaw(DpadVerticalAxis);
                    lastDPadHorizontal = dpadH;
                    lastDPadVertical = dpadV;
                }
                if (LeftStickHorizontalAxis != String.Empty && RightStickHorizontalAxis != String.Empty)
                {
                    float leftStickX = Input.GetAxisRaw(LeftStickHorizontalAxis);
                    float leftStickY = Input.GetAxisRaw(LeftStickVerticalAxis);
                    float rightStickX = Input.GetAxisRaw(RightStickHorizontalAxis);
                    float rightStickY = Input.GetAxisRaw(RightStickVerticalAxis);
                    lastLeftStickX = leftStickX;
                    lastLeftStickY = leftStickY;
                    lastRightStickX = rightStickX;
                    lastRightStickY = rightStickY;
                }
                if (TriggerAxis != String.Empty)
                {
                    float triggers = Input.GetAxisRaw(TriggerAxis);
                    lastRT = triggers;
                    lastLT = triggers;
                }
            }
#endif
        }

        public List<int> GetJoystickButtonsDown()
        {
            List<int> buttons = new List<int>();

            if (inputSystemType == InputSystemType.Old)
            {
                // Digital button detection

                if (Input.GetKeyDown(KeyCode.JoystickButton0)) buttons.Add(buttonMappings[14].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton1)) buttons.Add(buttonMappings[15].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton2)) buttons.Add(buttonMappings[16].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton3)) buttons.Add(buttonMappings[17].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton4)) buttons.Add(buttonMappings[18].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton5)) buttons.Add(buttonMappings[19].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton6)) buttons.Add(buttonMappings[20].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton7)) buttons.Add(buttonMappings[21].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton8)) buttons.Add(buttonMappings[22].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton9)) buttons.Add(buttonMappings[23].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton10)) buttons.Add(buttonMappings[24].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton11)) buttons.Add(buttonMappings[25].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton12)) buttons.Add(buttonMappings[26].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton13)) buttons.Add(buttonMappings[27].buttonIndex);
                if (Input.GetKeyDown(KeyCode.JoystickButton14)) buttons.Add(14);
                if (Input.GetKeyDown(KeyCode.JoystickButton15)) buttons.Add(15);
                if (Input.GetKeyDown(KeyCode.JoystickButton16)) buttons.Add(16);
                if (Input.GetKeyDown(KeyCode.JoystickButton17)) buttons.Add(17);
                if (Input.GetKeyDown(KeyCode.JoystickButton18)) buttons.Add(18);
                if (Input.GetKeyDown(KeyCode.JoystickButton19)) buttons.Add(19);


                // Analog axis handling for triggers/sticks

                float switching = 1;

                if (DpadHorizontalAxis != String.Empty && DpadVerticalAxis != String.Empty)
                {
                    float dpadH = Input.GetAxisRaw(DpadHorizontalAxis);
                    float dpadV = Input.GetAxisRaw(DpadVerticalAxis);

                    if (lastDPadHorizontal == 0 && dpadH != 0)
                    {
                        if (dpadH > 0)
                        {
                            buttons.Add(buttonMappings[0].buttonIndex);
                        }
                        else if (dpadH < 0)
                        {
                            buttons.Add(buttonMappings[1].buttonIndex);
                        }
                        lastDPadHorizontal = dpadH;
                    }
                    if (lastDPadVertical == 0 && dpadV != 0)
                    {
                        if (dpadV > 0)
                        {
                            buttons.Add(buttonMappings[2].buttonIndex);
                        }
                        else if (dpadV < 0)
                        {
                            buttons.Add(buttonMappings[3].buttonIndex);
                        }
                        lastDPadVertical = dpadV;
                    }

                }

                if (TriggerAxis != String.Empty)
                {
                    float triggers = Input.GetAxisRaw(TriggerAxis);
                    if (lastRT == 0 && triggers != 0)
                    {
                        if (triggers > 0)
                        {
                            buttons.Add(buttonMappings[4].buttonIndex);
                        }
                        else if (triggers < 0)
                        {
                            buttons.Add(buttonMappings[5].buttonIndex);
                        }
                        lastRT = triggers;
                        lastLT = triggers;
                    }
                }

                if (LeftStickHorizontalAxis != String.Empty && RightStickHorizontalAxis != String.Empty)
                {
                    float leftStickX = Input.GetAxisRaw(LeftStickHorizontalAxis);
                    float leftStickY = Input.GetAxisRaw(LeftStickVerticalAxis);
                    float rightStickX = Input.GetAxisRaw(RightStickHorizontalAxis);
                    float rightStickY = Input.GetAxisRaw(RightStickVerticalAxis);
                    if (Mathf.Abs(leftStickX - lastLeftStickX) != switching && Mathf.Abs(leftStickY - lastLeftStickY) != switching)
                    {
                        if (leftStickX > halfValue && lastLeftStickX <= halfValue) buttons.Add(buttonMappings[6].buttonIndex); 
                        if (leftStickX < -halfValue && lastLeftStickX >= -halfValue) buttons.Add(buttonMappings[7].buttonIndex);
                        if (leftStickY > halfValue && lastLeftStickY <= halfValue) buttons.Add(buttonMappings[8].buttonIndex); 
                        if (leftStickY < -halfValue && lastLeftStickY >= -halfValue) buttons.Add(buttonMappings[9].buttonIndex);
                    }
                    if (Mathf.Abs(rightStickX - lastRightStickX) != switching && Mathf.Abs(rightStickY - lastRightStickY) != switching)
                    {
                        if (rightStickX > halfValue && lastRightStickX <= halfValue) buttons.Add(buttonMappings[10].buttonIndex); 
                        if (rightStickX < -halfValue && lastRightStickX >= -halfValue) buttons.Add(buttonMappings[11].buttonIndex); 
                        if (rightStickY > halfValue && lastRightStickY <= halfValue) buttons.Add(buttonMappings[12].buttonIndex);
                        if (rightStickY < -halfValue && lastRightStickY >= -halfValue) buttons.Add(buttonMappings[13].buttonIndex); 
                    }
                    lastLeftStickX = leftStickX;
                    lastLeftStickY = leftStickY;
                    lastRightStickX = rightStickX;
                    lastRightStickY = rightStickY;

                }
            }
#if ENABLE_INPUT_SYSTEM
            else
            {
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    // Face buttons

                    if (gamepad.buttonSouth.wasPressedThisFrame) buttons.Add(buttonMappings[14].buttonIndex); // A
                    if (gamepad.buttonEast.wasPressedThisFrame) buttons.Add(buttonMappings[15].buttonIndex); // B
                    if (gamepad.buttonWest.wasPressedThisFrame) buttons.Add(buttonMappings[16].buttonIndex); // X
                    if (gamepad.buttonNorth.wasPressedThisFrame) buttons.Add(buttonMappings[17].buttonIndex); // Y
                    if (gamepad.leftShoulder.wasPressedThisFrame) buttons.Add(buttonMappings[18].buttonIndex); // LB
                    if (gamepad.rightShoulder.wasPressedThisFrame) buttons.Add(buttonMappings[19].buttonIndex); // RB
                    if (gamepad.selectButton.wasPressedThisFrame) buttons.Add(buttonMappings[20].buttonIndex); // Back
                    if (gamepad.startButton.wasPressedThisFrame) buttons.Add(buttonMappings[21].buttonIndex); // Start
                    if (gamepad.leftStickButton.wasPressedThisFrame) buttons.Add(buttonMappings[22].buttonIndex); // Left Stick
                    if (gamepad.rightStickButton.wasPressedThisFrame) buttons.Add(buttonMappings[23].buttonIndex); // Right Stick
                    if (gamepad.dpad.up.wasPressedThisFrame) buttons.Add(buttonMappings[24].buttonIndex); // D-Pad Up
                    if (gamepad.dpad.down.wasPressedThisFrame) buttons.Add(buttonMappings[25].buttonIndex); // D-Pad Down
                    if (gamepad.dpad.left.wasPressedThisFrame) buttons.Add(buttonMappings[26].buttonIndex); // D-Pad Left
                    if (gamepad.dpad.right.wasPressedThisFrame) buttons.Add(buttonMappings[27].buttonIndex); // D-Pad Right

                    // D-pad axis handling

                    float dpadH = gamepad.dpad.x.ReadValue();
                    float dpadV = gamepad.dpad.y.ReadValue();
                    float leftStickX = gamepad.leftStick.x.ReadValue();
                    float leftStickY = gamepad.leftStick.y.ReadValue();
                    float rightStickX = gamepad.rightStick.x.ReadValue();
                    float rightStickY = gamepad.rightStick.y.ReadValue();
                    float lt = gamepad.leftTrigger.ReadValue();
                    float rt = gamepad.rightTrigger.ReadValue();

                    //if (lastDPadHorizontal == 0 && dpadH != 0)
                    //{
                    //    if (dpadH > 0)
                    //    {
                    //        buttons.Add(buttonMappings[0].buttonIndex);
                    //    }
                    //    else if (dpadH < 0)
                    //    {
                    //        buttons.Add(buttonMappings[1].buttonIndex);
                    //    }
                    //    lastDPadHorizontal = dpadH;
                    //}
                    //if (lastDPadVertical == 0 && dpadV != 0)
                    //{
                    //    if (dpadV > 0)
                    //    {
                    //        buttons.Add(buttonMappings[2].buttonIndex);
                    //    }
                    //    else if (dpadV < 0)
                    //    {
                    //        buttons.Add(buttonMappings[3].buttonIndex);
                    //    }
                    //    lastDPadVertical = dpadV;
                    //}

                    if (lastLT == 0 && lt != 0)
                    {
                        buttons.Add(buttonMappings[5].buttonIndex);
                        lastLT = lt;
                    }
                    if (lastRT == 0 && rt != 0)
                    {
                        buttons.Add(buttonMappings[4].buttonIndex);
                        lastRT = rt;
                    }                   

            if (leftStickX > halfValue && lastLeftStickX <= halfValue) buttons.Add(buttonMappings[6].buttonIndex); // Left Stick Right
            if (leftStickX < -halfValue && lastLeftStickX >= -halfValue) buttons.Add(buttonMappings[7].buttonIndex); // Left Stick Left
            if (leftStickY > halfValue && lastLeftStickY <= halfValue) buttons.Add(buttonMappings[8].buttonIndex); // Left Stick Up
            if (leftStickY < -halfValue && lastLeftStickY >= -halfValue) buttons.Add(buttonMappings[9].buttonIndex); // Left Stick Down

            if (rightStickX > halfValue && lastRightStickX <= halfValue) buttons.Add(buttonMappings[10].buttonIndex); // Right Stick Right
            if (rightStickX < -halfValue && lastRightStickX >= -halfValue) buttons.Add(buttonMappings[11].buttonIndex); // Right Stick Left
            if (rightStickY > halfValue && lastRightStickY <= halfValue) buttons.Add(buttonMappings[12].buttonIndex); // Right Stick Up
            if (rightStickY < -halfValue && lastRightStickY >= -halfValue) buttons.Add(buttonMappings[13].buttonIndex); // Right Stick Down

            lastLeftStickX = leftStickX;
            lastLeftStickY = leftStickY;
            lastRightStickX = rightStickX;
            lastRightStickY = rightStickY;
        }
    }
#endif
            return buttons;
        }

        public List<int> GetJoystickButtonsUp()
        {
            List<int> buttons = new List<int>();

            if (inputSystemType == InputSystemType.Old)
            {
                // Digital button detection

                if (Input.GetKeyUp(KeyCode.JoystickButton0)) buttons.Add(buttonMappings[14].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton1)) buttons.Add(buttonMappings[15].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton2)) buttons.Add(buttonMappings[16].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton3)) buttons.Add(buttonMappings[17].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton4)) buttons.Add(buttonMappings[18].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton5)) buttons.Add(buttonMappings[19].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton6)) buttons.Add(buttonMappings[20].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton7)) buttons.Add(buttonMappings[21].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton8)) buttons.Add(buttonMappings[22].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton9)) buttons.Add(buttonMappings[23].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton10)) buttons.Add(buttonMappings[24].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton11)) buttons.Add(buttonMappings[25].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton12)) buttons.Add(buttonMappings[26].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton13)) buttons.Add(buttonMappings[27].buttonIndex);
                if (Input.GetKeyUp(KeyCode.JoystickButton14)) buttons.Add(14);
                if (Input.GetKeyUp(KeyCode.JoystickButton15)) buttons.Add(15);
                if (Input.GetKeyUp(KeyCode.JoystickButton16)) buttons.Add(16);
                if (Input.GetKeyUp(KeyCode.JoystickButton17)) buttons.Add(17);
                if (Input.GetKeyUp(KeyCode.JoystickButton18)) buttons.Add(18);
                if (Input.GetKeyUp(KeyCode.JoystickButton19)) buttons.Add(19);


                // Analog axis handling for triggers/sticks

                float switching = 1;

                if (DpadHorizontalAxis != String.Empty && DpadVerticalAxis != String.Empty)
                {
                    float dpadH = Input.GetAxisRaw(DpadHorizontalAxis);
                    float dpadV = Input.GetAxisRaw(DpadVerticalAxis);
                    if (lastDPadHorizontal != 0 && dpadH == 0)
                    {
                        if (lastDPadHorizontal > 0)
                        {
                            buttons.Add(buttonMappings[0].buttonIndex);
                        }
                        else if (lastDPadHorizontal < 0)
                        {
                            buttons.Add(buttonMappings[1].buttonIndex);
                        }
                        lastDPadHorizontal = dpadH;
                    }
                    if (lastDPadVertical != 0 && dpadV == 0)
                    {
                        if (lastDPadVertical > 0)
                        {
                            buttons.Add(buttonMappings[2].buttonIndex);
                        }
                        else if (lastDPadVertical < 0)
                        {
                            buttons.Add(buttonMappings[3].buttonIndex);
                        }
                        lastDPadVertical = dpadV;
                    }
                }

                if (TriggerAxis != String.Empty)
                {
                    float triggers = Input.GetAxisRaw(TriggerAxis);
                    if (lastRT != 0 && triggers == 0)
                    {
                        if (lastRT == 1)
                        {
                            buttons.Add(buttonMappings[4].buttonIndex);
                        }
                        else if (lastLT == -1)
                        {
                            buttons.Add(buttonMappings[5].buttonIndex);
                        }
                        lastRT = triggers;
                        lastLT = triggers;
                    }
                }

                if (LeftStickHorizontalAxis != String.Empty && RightStickHorizontalAxis != String.Empty)
                {
                    float leftStickX = Input.GetAxisRaw(LeftStickHorizontalAxis);
                    float leftStickY = Input.GetAxisRaw(LeftStickVerticalAxis);
                    float rightStickX = Input.GetAxisRaw(RightStickHorizontalAxis);
                    float rightStickY = Input.GetAxisRaw(RightStickVerticalAxis);
                    if (Mathf.Abs(leftStickX - lastLeftStickX) != switching && Mathf.Abs(leftStickY - lastLeftStickY) != switching)
                    {
                        if (leftStickX > halfValue && lastLeftStickX <= halfValue) buttons.Add(buttonMappings[6].buttonIndex);
                        if (leftStickX < -halfValue && lastLeftStickX >= -halfValue) buttons.Add(buttonMappings[7].buttonIndex);
                        if (leftStickY > halfValue && lastLeftStickY <= halfValue) buttons.Add(buttonMappings[8].buttonIndex);
                        if (leftStickY < -halfValue && lastLeftStickY >= -halfValue) buttons.Add(buttonMappings[9].buttonIndex);
                    }
                    if (Mathf.Abs(rightStickX - lastRightStickX) != switching && Mathf.Abs(rightStickY - lastRightStickY) != switching)
                    {
                        if (rightStickX > halfValue && lastRightStickX <= halfValue) buttons.Add(buttonMappings[10].buttonIndex);
                        if (rightStickX < -halfValue && lastRightStickX >= -halfValue) buttons.Add(buttonMappings[11].buttonIndex);
                        if (rightStickY > halfValue && lastRightStickY <= halfValue) buttons.Add(buttonMappings[12].buttonIndex);
                        if (rightStickY < -halfValue && lastRightStickY >= -halfValue) buttons.Add(buttonMappings[13].buttonIndex);
                    }
                    lastLeftStickX = leftStickX;
                    lastLeftStickY = leftStickY;
                    lastRightStickX = rightStickX;
                    lastRightStickY = rightStickY;

                }
            }
#if ENABLE_INPUT_SYSTEM
    else
    {
          var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // Face buttons

            if (gamepad.buttonSouth.wasReleasedThisFrame) buttons.Add(buttonMappings[14].buttonIndex); // A
            if (gamepad.buttonEast.wasReleasedThisFrame) buttons.Add(buttonMappings[15].buttonIndex); // B
            if (gamepad.buttonWest.wasReleasedThisFrame) buttons.Add(buttonMappings[16].buttonIndex); // X
            if (gamepad.buttonNorth.wasReleasedThisFrame) buttons.Add(buttonMappings[17].buttonIndex); // Y
            if (gamepad.leftShoulder.wasReleasedThisFrame) buttons.Add(buttonMappings[18].buttonIndex); // LB
            if (gamepad.rightShoulder.wasReleasedThisFrame) buttons.Add(buttonMappings[19].buttonIndex); // RB
            if (gamepad.selectButton.wasReleasedThisFrame) buttons.Add(buttonMappings[20].buttonIndex); // Back
            if (gamepad.startButton.wasReleasedThisFrame) buttons.Add(buttonMappings[21].buttonIndex); // Start
            if (gamepad.leftStickButton.wasReleasedThisFrame) buttons.Add(buttonMappings[22].buttonIndex); // Left Stick
            if (gamepad.rightStickButton.wasReleasedThisFrame) buttons.Add(buttonMappings[23].buttonIndex); // Right Stick
            if (gamepad.dpad.up.wasReleasedThisFrame) buttons.Add(buttonMappings[24].buttonIndex); // D-Pad Up
            if (gamepad.dpad.down.wasReleasedThisFrame) buttons.Add(buttonMappings[25].buttonIndex); // D-Pad Down
            if (gamepad.dpad.left.wasReleasedThisFrame) buttons.Add(buttonMappings[26].buttonIndex); // D-Pad Left
            if (gamepad.dpad.right.wasReleasedThisFrame) buttons.Add(buttonMappings[27].buttonIndex); // D-Pad Right
            
            // D-pad axis handling

            float dpadH = gamepad.dpad.x.ReadValue();
            float dpadV = gamepad.dpad.y.ReadValue();
            float leftStickX = gamepad.leftStick.x.ReadValue();
            float leftStickY = gamepad.leftStick.y.ReadValue();
            float rightStickX = gamepad.rightStick.x.ReadValue();
            float rightStickY = gamepad.rightStick.y.ReadValue();
            float lt = gamepad.leftTrigger.ReadValue();
            float rt = gamepad.rightTrigger.ReadValue();

                    if (lastDPadHorizontal != 0 && dpadH == 0)
                    {
                        if (lastDPadHorizontal > 0)
                        {
                            buttons.Add(buttonMappings[0].buttonIndex);
                        }
                        else if (lastDPadHorizontal < 0)
                        {
                            buttons.Add(buttonMappings[1].buttonIndex);
                        }
                        lastDPadHorizontal = dpadH;
                    }
                    if (lastDPadVertical != 0 && dpadV == 0)
                    {
                        if (lastDPadVertical > 0)
                        {
                            buttons.Add(buttonMappings[2].buttonIndex);
                        }
                        else if (lastDPadVertical < 0)
                        {
                            buttons.Add(buttonMappings[3].buttonIndex);
                        }
                        lastDPadVertical = dpadV;
                    }

                    if (lastLT != 0 && lt == 0)
                    {
                        buttons.Add(buttonMappings[5].buttonIndex);
                        lastLT = lt;
                    }
                    if (lastRT != 0 && rt == 0)
                    {
                        buttons.Add(buttonMappings[4].buttonIndex);
                        lastRT = rt;
                    }

                    if (leftStickX > halfValue && lastLeftStickX <= halfValue) buttons.Add(buttonMappings[6].buttonIndex); // Left Stick Right
            if (leftStickX < -halfValue && lastLeftStickX >= -halfValue) buttons.Add(buttonMappings[7].buttonIndex); // Left Stick Left
            if (leftStickY > halfValue && lastLeftStickY <= halfValue) buttons.Add(buttonMappings[8].buttonIndex); // Left Stick Up
            if (leftStickY < -halfValue && lastLeftStickY >= -halfValue) buttons.Add(buttonMappings[9].buttonIndex); // Left Stick Down

            if (rightStickX > halfValue && lastRightStickX <= halfValue) buttons.Add(buttonMappings[10].buttonIndex); // Right Stick Right
            if (rightStickX < -halfValue && lastRightStickX >= -halfValue) buttons.Add(buttonMappings[11].buttonIndex); // Right Stick Left
            if (rightStickY > halfValue && lastRightStickY <= halfValue) buttons.Add(buttonMappings[12].buttonIndex); // Right Stick Up
            if (rightStickY < -halfValue && lastRightStickY >= -halfValue) buttons.Add(buttonMappings[13].buttonIndex); // Right Stick Down

            lastLeftStickX = leftStickX;
            lastLeftStickY = leftStickY;
            lastRightStickX = rightStickX;
            lastRightStickY = rightStickY;
        }
    }
#endif
            return buttons;
        }

        public bool IsDpadButton(int buttonIndex)
        {
            return buttonIndex == buttonMappings[24].buttonIndex || buttonIndex == buttonMappings[25].buttonIndex || buttonIndex == buttonMappings[26].buttonIndex || buttonIndex == buttonMappings[27].buttonIndex ||
                   buttonIndex == buttonMappings[0].buttonIndex || buttonIndex == buttonMappings[1].buttonIndex || buttonIndex == buttonMappings[2].buttonIndex || buttonIndex == buttonMappings[3].buttonIndex;
        }

        public bool IsTriggerButton(int buttonIndex)
        {
            return buttonIndex == buttonMappings[4].buttonIndex || // RT
                   buttonIndex == buttonMappings[5].buttonIndex;  // LT
        }
        public bool IsStickButton(int buttonIndex)
        {
            return buttonIndex == buttonMappings[6].buttonIndex || // Left Stick Right
                   buttonIndex == buttonMappings[7].buttonIndex || // Left Stick Left
                   buttonIndex == buttonMappings[8].buttonIndex || // Left Stick Up
                   buttonIndex == buttonMappings[9].buttonIndex || // Left Stick Down
                   buttonIndex == buttonMappings[10].buttonIndex || // Right Stick Right
                   buttonIndex == buttonMappings[11].buttonIndex || // Right Stick Left
                   buttonIndex == buttonMappings[12].buttonIndex || // Right Stick Up
                   buttonIndex == buttonMappings[13].buttonIndex;  // Right Stick Down
        }

        public bool IsSameDpadDirection(int buttonIndex1, int buttonIndex2)
        {
            var dpadMappings = new Dictionary<int, int>
    {
        { buttonMappings[24].buttonIndex, buttonMappings[2].buttonIndex },
        { buttonMappings[2].buttonIndex, buttonMappings[24].buttonIndex },
        { buttonMappings[25].buttonIndex, buttonMappings[3].buttonIndex },
        { buttonMappings[3].buttonIndex, buttonMappings[25].buttonIndex },
        { buttonMappings[26].buttonIndex, buttonMappings[1].buttonIndex },
        { buttonMappings[1].buttonIndex, buttonMappings[26].buttonIndex },
        { buttonMappings[27].buttonIndex, buttonMappings[0].buttonIndex },
        { buttonMappings[0].buttonIndex, buttonMappings[27].buttonIndex }
    };

            return dpadMappings.TryGetValue(buttonIndex1, out var mappedButton) && mappedButton == buttonIndex2;
        }

        #endregion

        #region Other Input Types
        // Checks for middle mouse button down
        public bool GetMiddleClickDown()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonDown(2);
            }
#if ENABLE_INPUT_SYSTEM
    else
    {
        return Mouse.current.middleButton.wasPressedThisFrame;
    }
#endif
            return false;
        }

        // Checks for middle mouse button up
        public bool GetMiddleClickUp()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButtonUp(2);
            }
#if ENABLE_INPUT_SYSTEM
    else
    {
        return Mouse.current.middleButton.wasReleasedThisFrame;
    }
#endif
            return false;
        }

        // Checks if middle mouse button continues to be pressed
        public bool GetMiddleClick()
        {
            if (inputSystemType == InputSystemType.Old)
            {
                return Input.GetMouseButton(2);
            }
#if ENABLE_INPUT_SYSTEM
    else
    {
        return Mouse.current.middleButton.isPressed;
    }
#endif
            return false;
        }
        #endregion
    }
}