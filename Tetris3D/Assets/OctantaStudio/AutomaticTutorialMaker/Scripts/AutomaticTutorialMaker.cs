using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AutomaticTutorialMaker
{
    #region StepStructure
    [Serializable]
    public class ClickData
    {
        [Header("Step state (read only)")]
        [SerializeField][ReadOnly] public StateEnum stepState; // Current state of the tutorial step (NotDoneYet, Displaying, Done)
        [ReadOnly] public string blockedByTimeDelay; // Whether the step is blocked by a time delay condition
        [ReadOnly] public bool blockedByTime; // Whether the step is blocked by a time condition
        [ReadOnly] public bool blockedByButton; // Whether the step is blocked by a button condition

        [Header("Condition for starting step")]
        [Tooltip("Defines how the step should be started. AutoAfterPreviousStep: Automatically starts after the previous step. ManuallyCall: Requires a manual script call to start.")]
        public StartTypeEnum startStep; // Type of step initialization

        [Tooltip("Determines how long the step should be delayed before it starts. Recommended for delayed start if you want to spawn objects first and then find them by tag or layer.")]
        public float startDelay;
        [Tooltip("If true, this step will execute in parallel with other steps in the same parallel group.")]
        public bool executeInParallel = false; // Whether the step executes in parallel

        [Tooltip("ID for the parallel execution group. Steps with the same ID will execute in parallel.")]
        public int parallelGroupId = -1; // ID for parallel execution group

        [Header("Condition for finishing step")]
        [Tooltip("Type of interaction required to complete the step (e.g., Click, Hold, Drag, etc.).")]
        public InteractionTypeEnum interaction; // Type of required interaction

        [Tooltip("Defines how the target object is identified (e.g., ByGameObject, ByTag, ByLayer, AnyTarget).")]
        public InteractionTargetEnum checkInteraction; // Type of interaction target check

        [HideInInspector] public float scrollAmount; // Amount of scroll movement required (hidden in inspector)
        [Tooltip("Minimum time required before the step can be completed.")]
        public float minTimeAmount; // Minimum time before step completion

        [HideInInspector] public bool fakeblockedByButton; // Internal state for button blocking (hidden in inspector)

        [Header("Target to interact data")]
        [Tooltip("List of target GameObjects for interaction. These are the objects the user needs to interact with.")]
        public List<UnityEngine.Object> GameObjects; // Target objects for interaction

        [Tooltip("List of tags for target objects. The step will check for objects with these tags.")]
        public List<string> tags = new List<string>(); // Target tags for interaction

        [Tooltip("List of layers for target objects. The step will check for objects on these layers.")]
        public List<int> layers = new List<int>(); // Target layers for interaction

        [Tooltip("List of object types for target objects (e.g., UI, 3D, 2D).")]
        public List<ObjectTypeEnum> objectTypes = new List<ObjectTypeEnum>(); // Types of target objects

        [Tooltip("List of key codes required for interaction (e.g., KeyCode.W, KeyCode.A).")]
        public List<KeyCode> keyCodes = new List<KeyCode>(); // Required key inputs

        [Header("UI Tips")]
        [Tooltip("Prefab for the pointer visual in UI space. Should have a UIPointerGraphAnimation component.")]
        public GameObject pointerPrefab; // Prefab for pointer visual

        [Tooltip("Text displayed with the pointer in UI space.")]
        [HideInInspector] public string pointerText; // Text displayed with pointer

        [Tooltip("Prefab for the graphic visual in UI space. Should have a UIGraphicAnimation component.")]
        public GameObject graphicPrefab; // Prefab for graphic visual

        [Tooltip("Text displayed with the graphic in UI space.")]
        [HideInInspector] public string graphicText; // Text displayed with graphic

        [Tooltip("Prefab for the hover visual in UI space. Should have a UIPointerGraphAnimation component.")]
        public GameObject hoverPrefab; // Prefab for hover visual

        [Header("World Tips")]
        [Tooltip("Prefab for the pointer visual in world space. Should have a WorldPointerAnimation component.")]
        public GameObject worldPointerPrefab; // Prefab for pointer visual in world space

        [Tooltip("Text displayed with the pointer in world space.")]
        [HideInInspector] public string worldPointerText; // Text displayed with pointer in world space

        [Tooltip("Prefab for the graphic visual in world space. Should have a WorldGraphicAnimation component.")]
        public GameObject worldGraphicPrefab; // Prefab for graphic visual in world space

        [Tooltip("Text displayed with the graphic in world space.")]
        [HideInInspector] public string worldGraphicText; // Text displayed with graphic in world space

        [Tooltip("Prefab for the hover visual in world space. Should have a WorldPointerAnimation component.")]
        public GameObject worldHoverPrefab; // Prefab for hover visual in world space

        [SerializeField] public InputStringsScriptableObject.InputElement localizationReference; // Reference to an element from the tipStrings list

        [Header("Additional step behavior")]
        [Tooltip("Event triggered when the step starts.")]
        public UnityEngine.Events.UnityEvent onStepStart; // Event triggered on step start

        [Tooltip("Event triggered when the step is completed.")]
        public UnityEngine.Events.UnityEvent onStepComplete; // Event triggered on step completion

    }

    // Defines how tutorial step should be started
    public enum StartTypeEnum
    {
        AutoAfterPreviousStep, // Auto start after previous step
        ManuallyCall // Manual script call required
    }

    // Defines type of interaction required to complete step
    public enum InteractionTypeEnum
    {
        Click,
        RightClick,
        DoubleClick,
        RightDoubleClick,
        Hold,
        RightHold,
        Drag,
        RightDrag,
        DragAndDrop,
        ScrollUp,
        ScrollDown,
        KeyCode,
        KeyCodeHold,
        KeyCodeCombo,
        KeyCodeDoublePress,
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown,
        JoystickButton,
        JoystickButtonHold,
        MiddleClick,
        MiddleHold,
        PinchIn,
        PinchOut,
        PinchRotate,
        ManuallyCall
    }

    // Defines how target object is identified
    public enum InteractionTargetEnum
    {
        ByGameObject,    // Target specific object
        ByTag,          // Target objects with tag
        ByLayer,        // Target objects in layer
        AnyTarget       // No specific target required
    }

    // Defines type of interactive object
    public enum ObjectTypeEnum
    {
        UI, // UI element type
        _3D, // 3D object type
        _2D, // 2D object type
        None // Undefined type
    }

    // Defines current state of tutorial step
    public enum StateEnum
    {
        NotDoneYet, // Step not started
        Displaying, // Step in progress
        Done // Step completed     
    }

    #endregion

    public class AutomaticTutorialMaker : MonoBehaviour
    {
        #region Variables
        [Grayed] public TutorialSceneReferences sceneReferences; // References to scene components
        private float recordingStartTime; // Time when recording was started
        [SerializeField] private bool isTracking; // Whether tutorial maker is currently recording
        private string clicksFilePath; // Path to save click sequence data
        public string directoryPath = "ClickSequences"; // Directory for storing sequences

        public List<ClickData> stepSequence = new List<ClickData>(); // List of tutorial steps

        private static List<ClickData> copiedClickSequence = null; // Temporary copy of sequence
        private static bool shouldPasteAfterExit = false; // Flag to paste sequence after play mode
        private static List<ClickData> tempSequence = null; // Temporary sequence storage

        private float clickStartTime; // Time when click/touch started
        private GameObject clickedObject; // Currently clicked/touched object
        private Vector3 clickStartPosition; // Initial position of click/touch
        private bool isHolding; // Whether click/touch is being held
        private ObjectTypeEnum objectType; // Type of clicked/touched object

        private Vector3 clickedObjectStartPosition;
        private Vector3 clickedObjectEndPosition;

        public bool IsTracking => isTracking; // Public accessor for tracking state

        private float lastScrollTime = 0f; // Time of last scroll event
        private InteractionTypeEnum lastScrollType = InteractionTypeEnum.Click; // Type of last scroll
        private float accumulatedScrollDelta = 0f; // Accumulated scroll movement
        private bool isScrolling = false; // Whether scroll is in progress
        private float lastClickTime = 0f; // Time of last click event
        public float doubleClickTimeWindow = 0.3f; // Time window for double click detection

        private Dictionary<KeyCode, float> keyPressStartTimes = new Dictionary<KeyCode, float>(); // Dictionary to track when each key began being pressed       
        private Dictionary<KeyCode, float> keyPressDurations = new Dictionary<KeyCode, float>(); // Dictionary to store how long each key was held down        
        private Dictionary<int, float> joystickButtonPressStartTimes = new Dictionary<int, float>(); // Dictionary to track when each joystick button began being pressed        
        private Dictionary<int, float> joystickButtonPressDurations = new Dictionary<int, float>(); // Dictionary to store how long each joystick button was held down        
        private bool isRecordingCombo = false; // Flag indicating whether a key combination is currently being recorded        
        private List<KeyCode> currentComboKeys = new List<KeyCode>(); // List of keys in the current key combination being recorded        
        private ClickData currentComboStep = null; // Reference to the tutorial step representing the current key combination        
        private bool waitForAllKeysRelease = false; // Flag to prevent new combinations from starting until all keys are released        
        private Dictionary<KeyCode, float> lastKeyPressTimes = new Dictionary<KeyCode, float>(); // Dictionary to track the time of the last press of each key (for double press detection)

        private bool isRecordingPinch = false;
        private bool isPinchSessionActive = false;

        private bool refreshed = false;

        #endregion

        #region Initialization
        // Initializes tutorial maker when component is enabled
        private void OnEnable()
        {
            LoadClickData();
        }

        // Initializes directory structure for click data storage
        public void Initialize()
        {
            string clicksDirectory = Path.Combine(Application.persistentDataPath, directoryPath);
            Directory.CreateDirectory(clicksDirectory);
            clicksFilePath = Path.Combine(clicksDirectory, "click_sequence.json");
        }

        private void SetReference()
        {
            if (!sceneReferences)
            {
                sceneReferences = transform.parent.GetComponent<TutorialSceneReferences>();
            }
        }

        #endregion

        #region Update
        // Processes input and records tutorial steps each frame
        private void Update()
        {
            if (!sceneReferences || !sceneReferences.inputController) return;
            if (sceneReferences.inputTextSettings != null)
            {
            
            if (!sceneReferences.inputTextSettings.refreshed || !refreshed)
            {
                AutomaticTextRefresher();
                refreshed = true;
                sceneReferences.inputTextSettings.refreshed = true;
            }
        }

            if (!isTracking) return;

            // Joystick 
            if (sceneReferences.inputController.isJoystickConnected)
            {
                var buttonsDown = sceneReferences.inputController.GetJoystickButtonsDown();
                foreach (int buttonIndex in buttonsDown)
                {
                    // Exeptions for triggers and d-pad
                    if (sceneReferences.inputController.IsStickButton(buttonIndex)) //sceneReferences.inputController.IsDpadButton(buttonIndex) || sceneReferences.inputController.IsTriggerButton(buttonIndex)
                    {
                        RecordJoystickInput(buttonIndex, false);
                    }
                    else
                    {
                        joystickButtonPressStartTimes[buttonIndex] = Time.time;
                    }
                }

                var buttonsUp = sceneReferences.inputController.GetJoystickButtonsUp();
                foreach (int buttonIndex in buttonsUp)
                {
                    if (joystickButtonPressStartTimes.TryGetValue(buttonIndex, out float startTime))
                    {
                        float duration = Time.time - startTime;
                        joystickButtonPressDurations[buttonIndex] = duration;
                        joystickButtonPressStartTimes.Remove(buttonIndex);
                        Debug.Log($"[ATM] Joystick button {buttonIndex} held for {duration} seconds");
                        if (duration > sceneReferences.inputController.minHoldDuration)
                        {
                            RecordJoystickInput(buttonIndex, true);
                        }
                        else
                        {
                            RecordJoystickInput(buttonIndex, false);
                        }
                    }
                }
            }

            // PC
            sceneReferences.inputController.UpdateKeyboardState();

            if (!sceneReferences.inputController.IsMobileDevice)
            {
                var pressedKeys = sceneReferences.inputController.GetCurrentPressedKeys();
                foreach (var key in pressedKeys)
                {
                    if (!keyPressStartTimes.ContainsKey(key))
                    {
                        keyPressStartTimes[key] = Time.time;
                    }
                }

                var releasedKeys = sceneReferences.inputController.GetReleasedKeys();

                HandleKeyCodeCombo(pressedKeys, releasedKeys);

                if (!isRecordingCombo && !waitForAllKeysRelease)
                {
                    foreach (var key in releasedKeys)
                    {
                        if (keyPressStartTimes.ContainsKey(key))
                        {
                            keyPressDurations[key] = Time.time - keyPressStartTimes[key];
                            Debug.Log($"[ATM] Key {key} held for {keyPressDurations[key]} seconds");

                            if (keyPressDurations[key] > sceneReferences.inputController.minHoldDuration)
                            {
                                RecordKeyboardInput(releasedKeys, true);
                                Debug.Log("[ATM] Keyboard hold input detected: " + releasedKeys);
                            }
                            else
                            {
                                RecordKeyboardInput(releasedKeys, false);
                                Debug.Log("[ATM] Keyboard regular input detected: " + releasedKeys);
                            }
                        }
                    }
                }
                foreach (var key in releasedKeys)
                {
                    if (keyPressStartTimes.ContainsKey(key))
                    {
                        keyPressStartTimes.Remove(key);
                    }
                }
            }

            // Mobile
            sceneReferences.inputController.UpdatePinchGesture();
            InteractionTypeEnum? pinchType = sceneReferences.inputController.GetCurrentPinchType();
            if (pinchType.HasValue && !isRecordingPinch)
            {
                RecordPinchGesture(pinchType.Value);
                isRecordingPinch = true;
                isPinchSessionActive = true;
            }
            else if (!sceneReferences.inputController.IsPinching() && isRecordingPinch)
            {
                isRecordingPinch = false;
                sceneReferences.inputController.ResetPinchGesture();
                SaveClickData();
            }

            float currentScrollDelta = sceneReferences.inputController.GetScrollDelta();
            bool isScrollingNow = Mathf.Abs(currentScrollDelta) > 0;

#if UNITY_EDITOR
            bool isMouseOverGameWindow = UnityEditor.EditorWindow.mouseOverWindow != null &&
                                       UnityEditor.EditorWindow.mouseOverWindow.GetType().ToString().Contains("GameView");
            if (!isMouseOverGameWindow)
            {
                if (isScrolling)
                {
                    accumulatedScrollDelta = 0f;
                    isScrolling = false;
                }
                isScrollingNow = false;
            }
#endif
            if (isScrollingNow)
            {
                float currentTime = Time.time;
                InteractionTypeEnum currentScrollType = currentScrollDelta > 0 ?
                    InteractionTypeEnum.ScrollUp : InteractionTypeEnum.ScrollDown;

                if (!isScrolling || currentScrollType != lastScrollType)
                {
                    isScrolling = true;
                    lastScrollType = currentScrollType;
                    accumulatedScrollDelta = currentScrollDelta;

                    RecordScrollInteraction(currentScrollType, Mathf.Abs(accumulatedScrollDelta));
                }
                else
                {
                    accumulatedScrollDelta += currentScrollDelta;
                    if (stepSequence.Count > 0)
                    {
                        var lastStep = stepSequence[stepSequence.Count - 1];
                        if (lastStep.interaction == currentScrollType)
                        {
                            lastStep.scrollAmount = Mathf.Abs(accumulatedScrollDelta);
                        }
                    }
                }

                lastScrollTime = currentTime;
            }
            else if (isScrolling && Time.time - lastScrollTime >= 0.5f)
            {
                RecordScrollInteraction(lastScrollType, Mathf.Abs(accumulatedScrollDelta));
                accumulatedScrollDelta = 0f;
                isScrolling = false;
                SaveClickData();
            }

            bool isInputDown = sceneReferences.inputController.GetInputDown() ||
                    sceneReferences.inputController.GetRightClickDown() ||
                    sceneReferences.inputController.GetMiddleClickDown();
            bool isInputUp = sceneReferences.inputController.GetInputUp() ||
                            sceneReferences.inputController.GetRightClickUp() ||
                            sceneReferences.inputController.GetMiddleClickUp();

            if (isInputDown || isInputUp)
            {
                // Don't record clicks if the cursor is outside the game window
#if UNITY_EDITOR
                bool mouseOverGameWindow = UnityEditor.EditorWindow.mouseOverWindow != null &&
                                             UnityEditor.EditorWindow.mouseOverWindow.GetType().ToString().Contains("GameView");
                if (!mouseOverGameWindow)
                {
                    Debug.LogWarning("[ATM] Keep the cursor within the Game window to record actions.");
                    return;
                }

                if (!IsCursorOverGameWindow())
                {
                    Debug.LogWarning("[ATM] Keep the cursor within the Game window to record actions.");
                    return;
                }
#endif

                if (isScrolling && Mathf.Abs(accumulatedScrollDelta) > 0)
                {
                    RecordScrollInteraction(lastScrollType, Mathf.Abs(accumulatedScrollDelta));
                    accumulatedScrollDelta = 0f;
                    isScrolling = false;
                }

                if (isInputDown)
                {
                    if (!isPinchSessionActive)
                    {
                        HandleInputDown();
                    }
                }
                else if (isInputUp)
                {
                    if (isPinchSessionActive)
                    {
                        if (!sceneReferences.inputController.IsMultitouchActive() &&
                            !sceneReferences.inputController.GetInputDown() &&
                            !sceneReferences.inputController.GetInputUp())
                        {
                            isPinchSessionActive = false;
                        }
                    }
                    else
                    {
                        HandleInputUp();
                    }
                }
            }
        }
              
        #endregion

        #region RecognizingInput

        // Non-game clicks
        private bool IsCursorOverGameWindow()
        {
#if ENABLE_INPUT_SYSTEM
    Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif

            return mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
                   mousePosition.y >= 0 && mousePosition.y <= Screen.height;
        }

        // Records pinch interaction from Unity Remote
        private void RecordPinchGesture(InteractionTypeEnum pinchType)
        {
            if (stepSequence.Count > 0)
            {
                var lastStep = stepSequence[stepSequence.Count - 1];
                if (lastStep.interaction == pinchType)
                {
                    Debug.Log($"[ATM] Skipping duplicate {pinchType} gesture recording");
                    return;
                }
            }

            int lastStepsToCheck = Math.Min(5, stepSequence.Count);
            for (int i = stepSequence.Count - 1; i >= stepSequence.Count - lastStepsToCheck; i--)
            {
                if (i < 0) break;

                var step = stepSequence[i];
                if (step.interaction == InteractionTypeEnum.PinchIn ||
                    step.interaction == InteractionTypeEnum.PinchOut ||
                    step.interaction == InteractionTypeEnum.PinchRotate)
                {
                    if (isRecordingPinch)
                    {
                        Debug.Log($"[ATM] Skipping new {pinchType} gesture during active pinch sequence");
                        return;
                    }
                }
            }

            ClickData click = new ClickData
            {
                interaction = pinchType,
                checkInteraction = InteractionTargetEnum.AnyTarget,
                GameObjects = new List<UnityEngine.Object>(),
                tags = new List<string>(),
                layers = new List<int>(),
                objectTypes = new List<ObjectTypeEnum>()
            };

            if (pinchType == InteractionTypeEnum.PinchIn ||
                pinchType == InteractionTypeEnum.PinchOut)
            {
                click.scrollAmount = sceneReferences.inputController.GetPinchScale();
            }
            else if (pinchType == InteractionTypeEnum.PinchRotate)
            {
                click.scrollAmount = sceneReferences.inputController.GetPinchRotation();
            }

            SetupVisualAndText(click);
            stepSequence.Add(click);
            Debug.Log($"[ATM] Recorded {pinchType} gesture with value {click.scrollAmount}");
        }

        // Records scroll interaction as tutorial step
        private void RecordScrollInteraction(InteractionTypeEnum scrollType, float scrollAmount)
        {
            if (stepSequence.Count > 0)
            {
                var lastStep = stepSequence[stepSequence.Count - 1];
                if (lastStep.interaction == scrollType)
                {
                    lastStep.scrollAmount += scrollAmount;
                    SaveClickData();
                    Debug.Log($"[ATM] Updated {scrollType} scroll amount to {lastStep.scrollAmount}");
                    return;
                }
            }

            ClickData click = new ClickData
            {
                interaction = scrollType,
                checkInteraction = InteractionTargetEnum.AnyTarget,
                GameObjects = new List<UnityEngine.Object>(),
                tags = new List<string>(),
                layers = new List<int>(),
                objectTypes = new List<ObjectTypeEnum>(),
                scrollAmount = scrollAmount
            };

            SetupVisualAndText(click);
            stepSequence.Add(click);
            SaveClickData();

            Debug.Log($"[ATM] Recorded new {scrollType} with amount {scrollAmount}");
        }

        // Processes input start event
        private void HandleInputDown()
        {
            if (sceneReferences.inputController.IsMultitouchActive())
            {
                return;
            }

            clickStartTime = Time.time;
            clickStartPosition = sceneReferences.inputController.GetInputPosition();
            ObjectTypeEnum detectedType;
            clickedObject = sceneReferences.inputController.GetClickedObject(out detectedType);
            objectType = detectedType;
            isHolding = clickedObject != null;
            if (clickedObject)
            {
                clickedObjectStartPosition = clickedObject.transform.position;
            }
        }

        // Processes input end event
        private void HandleInputUp()
        {
            if (sceneReferences.inputController.IsMultitouchActive())
            {
                return;
            }

            float inputDuration = sceneReferences.inputController.GetInputDuration(clickStartTime);
            Vector2 endPosition = sceneReferences.inputController.GetInputPosition();

            var swipeType = sceneReferences.inputController.DetectSwipe(
                clickStartPosition,
                endPosition,
                inputDuration
            );

            if (swipeType.HasValue)
            {
                RecordClick(
                    null,
                    objectType,
                    clickedObject?.transform.position,
                    swipeType.Value,
                    new List<UnityEngine.Object> { }
                );

                Debug.Log($"[ATM] Recorded swipe: {swipeType.Value}");
            }
            else
            {
                GameObject releaseObject = null;
                ObjectTypeEnum releaseObjectType;

                if (clickedObject != null)
                {
                    releaseObject = sceneReferences.inputController.GetClickedObject(out releaseObjectType);
                    if (releaseObject == clickedObject)
                    {
                        clickedObjectEndPosition = clickedObject.transform.position;
                    }
                }

                bool isSameObject = clickedObject != null && releaseObject != null && clickedObject == releaseObject;
                bool draggedTargetObject = IsObjectDragged(clickedObjectStartPosition, clickedObjectEndPosition);

                InteractionTypeEnum interactionType;
                if (sceneReferences.inputController.IsHoldGesture(inputDuration))
                {
                    if (sceneReferences.inputController.GetRightClickUp())
                    {
                        if (draggedTargetObject && isSameObject)
                        {
                            interactionType = InteractionTypeEnum.RightDrag;
                        }
                        else
                        {
                            interactionType = InteractionTypeEnum.RightHold;
                        }
                    }
                    else if (sceneReferences.inputController.GetMiddleClickUp())
                    {
                        interactionType = InteractionTypeEnum.MiddleHold;

                    }
                    else
                    {
                        if (draggedTargetObject && isSameObject)
                        {
                            interactionType = InteractionTypeEnum.Drag;
                        }
                        else
                        {
                            interactionType = InteractionTypeEnum.Hold;
                        }
                    }
                }
                else if (sceneReferences.inputController.GetRightClickUp())
                {
                    interactionType = InteractionTypeEnum.RightClick;
                }
                else if (sceneReferences.inputController.GetMiddleClickUp())
                {
                    interactionType = InteractionTypeEnum.MiddleClick;
                }
                else
                {
                    interactionType = InteractionTypeEnum.Click;
                }

                List<UnityEngine.Object> interactionObjects;
                if (interactionType == InteractionTypeEnum.DragAndDrop && clickedObject != null && releaseObject != null)
                {
                    interactionObjects = new List<UnityEngine.Object> { clickedObject, releaseObject };
                }
                else if (clickedObject != null)
                {
                    interactionObjects = new List<UnityEngine.Object> { clickedObject };
                }
                else
                {
                    interactionObjects = new List<UnityEngine.Object>();
                }

                RecordClick(
                    clickedObject,
                    objectType,
                    clickedObject?.transform.position,
                    interactionType,
                    interactionObjects
                );

                clickedObject = null;
                isHolding = false;
            }
        }

        private bool IsObjectDragged(Vector3 startPosition, Vector3 endPosition)
        {
            float distance = Vector3.Distance(startPosition, endPosition);
            return distance > sceneReferences.inputController.minDragDistance;
        }

        // Processes consecutive clicks for double click detection
        private void ProcessDoubleClicks()
        {
            if (stepSequence.Count < 2) return;

            for (int i = stepSequence.Count - 1; i > 0; i--)
            {
                var currentStep = stepSequence[i];
                var previousStep = stepSequence[i - 1];

                if ((currentStep.interaction == InteractionTypeEnum.Click && previousStep.interaction == InteractionTypeEnum.Click) ||
                    (currentStep.interaction == InteractionTypeEnum.MiddleClick && previousStep.interaction == InteractionTypeEnum.MiddleClick))
                {
                    bool sameTargets = IsSameTargets(currentStep, previousStep);

                    if (sameTargets)
                    {
                        if (currentStep.interaction == InteractionTypeEnum.Click)
                        {
                            previousStep.interaction = InteractionTypeEnum.DoubleClick;
                        }
                        SetupVisualAndText(previousStep);
                        stepSequence.RemoveAt(i);

                        Debug.Log($"[ATM] Converted two consecutive clicks to double click: {previousStep.interaction}");
                    }
                }
            }

            SaveClickData();
        }

        private void RecordJoystickInput(int buttonIndex, bool isHeld)
        {

            if (stepSequence.Count > 0)
            {
                var lastStep = stepSequence[stepSequence.Count - 1];
                if (lastStep.interaction == InteractionTypeEnum.JoystickButton || lastStep.interaction == InteractionTypeEnum.JoystickButtonHold)
                {
                    var leftStickButtons = new[] {
                sceneReferences.inputController.buttonMappings[6].buttonIndex,
                sceneReferences.inputController.buttonMappings[7].buttonIndex,
                sceneReferences.inputController.buttonMappings[8].buttonIndex,
                sceneReferences.inputController.buttonMappings[9].buttonIndex
            };

                    var rightStickButtons = new[] {
                sceneReferences.inputController.buttonMappings[10].buttonIndex,
                sceneReferences.inputController.buttonMappings[11].buttonIndex,
                sceneReferences.inputController.buttonMappings[12].buttonIndex,
                sceneReferences.inputController.buttonMappings[13].buttonIndex
            };

                    if (leftStickButtons.Contains(buttonIndex) &&
                        lastStep.keyCodes.Any(k => leftStickButtons.Contains((int)k - (int)KeyCode.JoystickButton0)))
                    {
                        return;
                    }

                    if (rightStickButtons.Contains(buttonIndex) &&
                        lastStep.keyCodes.Any(k => rightStickButtons.Contains((int)k - (int)KeyCode.JoystickButton0)))
                    {
                        return;
                    }
                    //if (sceneReferences.inputController.IsDpadButton(buttonIndex))
                    //{
                    //    var lastButtonIndex = (int)lastStep.keyCodes[0] - (int)KeyCode.JoystickButton0;

                    //    if (sceneReferences.inputController.IsDpadButton(lastButtonIndex) && sceneReferences.inputController.IsSameDpadDirection(buttonIndex, lastButtonIndex))
                    //    {
                    //        return;
                    //    }
                    //}
                }

                //if (buttonIndex == sceneReferences.inputController.buttonMappings[4].buttonIndex || buttonIndex == sceneReferences.inputController.buttonMappings[5].buttonIndex)
                //{
                //    var lastButtonIndex = (int)lastStep.keyCodes[0] - (int)KeyCode.JoystickButton0;
                //    if (lastButtonIndex == sceneReferences.inputController.buttonMappings[4].buttonIndex || lastButtonIndex == sceneReferences.inputController.buttonMappings[5].buttonIndex)
                //    {
                //        return;
                //    }
                //}
            }

            ClickData click = new ClickData
            {
                interaction = isHeld ? InteractionTypeEnum.JoystickButtonHold : InteractionTypeEnum.JoystickButton,
                checkInteraction = InteractionTargetEnum.AnyTarget,
                GameObjects = new List<UnityEngine.Object>(),
                tags = new List<string>(),
                layers = new List<int>(),
                objectTypes = new List<ObjectTypeEnum>(),
                keyCodes = new List<KeyCode> { (KeyCode)((int)KeyCode.JoystickButton0 + buttonIndex) }
            };

            SetupVisualAndText(click);
            stepSequence.Add(click);
            SaveClickData();
            Debug.Log($"[ATM] Recorded Joystick Button: {buttonIndex}, Held: {isHeld}");
        }

        #endregion

        #region RecognizingTargets

        // Compares targets between two steps
        private bool IsSameTargets(ClickData step1, ClickData step2)
        {
            if (step1.checkInteraction == InteractionTargetEnum.AnyTarget &&
                step2.checkInteraction == InteractionTargetEnum.AnyTarget)
            {
                return true;
            }

            if (step1.checkInteraction != step2.checkInteraction ||
                step1.GameObjects.Count != step2.GameObjects.Count)
            {
                return false;
            }

            if (step1.checkInteraction == InteractionTargetEnum.ByGameObject)
            {
                for (int i = 0; i < step1.GameObjects.Count; i++)
                {
                    if (step1.GameObjects[i] != step2.GameObjects[i])
                        return false;
                }
                return true;
            }

            if (step1.checkInteraction == InteractionTargetEnum.ByTag)
            {
                return step1.tags.SequenceEqual(step2.tags);
            }

            if (step1.checkInteraction == InteractionTargetEnum.ByLayer)
            {
                return step1.layers.SequenceEqual(step2.layers);
            }

            return false;
        }

        // Determine object type from components
        public ObjectTypeEnum GetObjectType(GameObject obj)
        {
            if (obj.TryGetComponent<RectTransform>(out _))
                return ObjectTypeEnum.UI;

            if (obj.TryGetComponent<SpriteRenderer>(out _))
                return ObjectTypeEnum._2D;

            if (obj.TryGetComponent<MeshFilter>(out _) ||
                obj.TryGetComponent<MeshRenderer>(out _) ||
                obj.TryGetComponent<SkinnedMeshRenderer>(out _))
                return ObjectTypeEnum._3D;

            Debug.LogWarning($"[ATM] Object {obj.name} doesn't match any known type (UI/2D/3D)");
            return ObjectTypeEnum.None;
        }

        // Attempts to get 3D object under cursor
        private bool TryGet3DObject(out GameObject obj)
        {
            Ray ray = Camera.main.ScreenPointToRay(sceneReferences.inputController.GetInputPosition());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                obj = hit.collider.gameObject;
                return true;
            }
            obj = null;
            return false;
        }

        // Attempts to get 2D object under cursor
        private bool TryGet2DObject(out GameObject obj)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(sceneReferences.inputController.GetInputPosition()), Vector2.zero);
            if (hit.collider != null)
            {
                obj = hit.collider.gameObject;
                return true;
            }
            obj = null;
            return false;
        }
        #endregion

        #region KeyboardPatterns
        // Checks if key is part of WASD control scheme
        private bool IsWASDKey(KeyCode key)
        {
            return key == KeyCode.W || key == KeyCode.A || key == KeyCode.S || key == KeyCode.D;
        }

        // Checks if a WASD step already exists in sequence 
        private bool HasWASDStepInSequence()
        {
            return stepSequence.Any(step =>
                step.interaction == InteractionTypeEnum.KeyCode &&
                step.keyCodes.Count == 4 &&
                step.keyCodes.All(IsWASDKey));
        }

        // Searches for WASD key pattern in sequence
        private (bool hasPattern, int firstIndex) FindWASDPattern()
        {
            HashSet<KeyCode> uniqueWASDKeys = new HashSet<KeyCode>();
            int firstWASDIndex = -1;

            for (int i = 0; i < stepSequence.Count; i++)
            {
                var step = stepSequence[i];
                if (step.interaction == InteractionTypeEnum.KeyCode)
                {
                    foreach (var key in step.keyCodes)
                    {
                        if (IsWASDKey(key))
                        {
                            uniqueWASDKeys.Add(key);
                            if (firstWASDIndex == -1)
                            {
                                firstWASDIndex = i;
                            }
                        }
                    }
                }
            }

            return (uniqueWASDKeys.Count >= 3, firstWASDIndex);
        }

        // Checks if text input step exists in sequence
        private bool HasTextInputStep()
        {
            return stepSequence.Any(step =>
                step.interaction == InteractionTypeEnum.KeyCode &&
                step.keyCodes.Count == 0);
        }

        // Checks if WASD step is last in sequence
        private bool HasWASDAsLastStep()
        {
            if (stepSequence.Count == 0) return false;
            var lastStep = stepSequence[stepSequence.Count - 1];
            return lastStep.interaction == InteractionTypeEnum.KeyCode &&
                   lastStep.keyCodes.Count == 4 &&
                   lastStep.keyCodes.Contains(KeyCode.W) &&
                   lastStep.keyCodes.Contains(KeyCode.A) &&
                   lastStep.keyCodes.Contains(KeyCode.S) &&
                   lastStep.keyCodes.Contains(KeyCode.D);
        }

        // Records keyboard input and creates appropriate steps
        private void RecordKeyboardInput(List<KeyCode> keys, bool isHeld)
        {
            if (isRecordingCombo || waitForAllKeysRelease)
            {
                Debug.Log("[ATM] Currently recording combo or waiting for keys release, ignoring single key input");
                return;
            }

            if (HasTextInputStep())
            {
                Debug.Log("[ATM] Text input step already exists, ignoring keyboard input");
                return;
            }

            bool containsWASD = keys.Any(IsWASDKey);

            if (HasWASDAsLastStep())
            {
                if (containsWASD)
                {
                    Debug.Log("[ATM] WASD is last step, ignoring WASD keys");
                    return;
                }
            }

            if (HasWASDStepInSequence() && !HasWASDAsLastStep())
            {
                ClickData click = new ClickData
                {
                    interaction = InteractionTypeEnum.KeyCode,
                    checkInteraction = InteractionTargetEnum.AnyTarget,
                    GameObjects = new List<UnityEngine.Object>(),
                    tags = new List<string>(),
                    layers = new List<int>(),
                    objectTypes = new List<ObjectTypeEnum>(),
                    keyCodes = new List<KeyCode>(keys)
                };
                SetupVisualAndText(click);
                stepSequence.Add(click);

                if (HasConsecutiveKeyPressPattern())
                {
                    ConsolidateTextInputSteps();
                }
                SaveClickData();
                return;
            }

            if (containsWASD && !HasWASDStepInSequence())
            {
                var (hasPattern, firstIndex) = FindWASDPattern();
                if (hasPattern)
                {
                    ClickData wasdStep = new ClickData
                    {
                        interaction = InteractionTypeEnum.KeyCode,
                        checkInteraction = InteractionTargetEnum.AnyTarget,
                        GameObjects = new List<UnityEngine.Object>(),
                        tags = new List<string>(),
                        layers = new List<int>(),
                        objectTypes = new List<ObjectTypeEnum>(),
                        keyCodes = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D }
                    };
                    SetupVisualAndText(wasdStep);

                    for (int i = stepSequence.Count - 1; i >= 0; i--)
                    {
                        if (stepSequence[i].interaction == InteractionTypeEnum.KeyCode &&
                            stepSequence[i].keyCodes.Any(IsWASDKey))
                        {
                            stepSequence.RemoveAt(i);
                        }
                    }

                    stepSequence.Insert(firstIndex, wasdStep);
                    SaveClickData();
                    return;
                }
            }

            bool isDoublePress = false;
            KeyCode doublePressedKey = KeyCode.None;

            if (!isHeld && keys.Count == 1)
            {
                KeyCode currentKey = keys[0];
                float currentTime = Time.time;

                if (lastKeyPressTimes.TryGetValue(currentKey, out float lastPressTime))
                {
                    if (currentTime - lastPressTime <= doubleClickTimeWindow)
                    {
                        isDoublePress = true;
                        doublePressedKey = currentKey;

                        lastKeyPressTimes.Remove(currentKey);

                        if (stepSequence.Count > 0)
                        {
                            var lastStep = stepSequence[stepSequence.Count - 1];
                            if (lastStep.interaction == InteractionTypeEnum.KeyCode &&
                                lastStep.keyCodes.Count == 1 &&
                                lastStep.keyCodes[0] == currentKey)
                            {
                                stepSequence.RemoveAt(stepSequence.Count - 1);
                                Debug.Log($"[ATM] Removed previous single press of {currentKey} to create double press");
                            }
                        }
                    }
                    else
                    {
                        lastKeyPressTimes[currentKey] = currentTime;
                    }
                }
                else
                {
                    lastKeyPressTimes[currentKey] = currentTime;
                }
            }

            if (isDoublePress)
            {
                ClickData doublePressClick = new ClickData
                {
                    interaction = InteractionTypeEnum.KeyCodeDoublePress,
                    checkInteraction = InteractionTargetEnum.AnyTarget,
                    GameObjects = new List<UnityEngine.Object>(),
                    tags = new List<string>(),
                    layers = new List<int>(),
                    objectTypes = new List<ObjectTypeEnum>(),
                    keyCodes = new List<KeyCode> { doublePressedKey }
                };
                SetupVisualAndText(doublePressClick);
                stepSequence.Add(doublePressClick);
                Debug.Log($"[ATM] Recorded double press of key: {doublePressedKey}");
            }
            else if (!isHeld)
            {
                ClickData regularClick = new ClickData
                {
                    interaction = InteractionTypeEnum.KeyCode,
                    checkInteraction = InteractionTargetEnum.AnyTarget,
                    GameObjects = new List<UnityEngine.Object>(),
                    tags = new List<string>(),
                    layers = new List<int>(),
                    objectTypes = new List<ObjectTypeEnum>(),
                    keyCodes = new List<KeyCode>(keys)
                };
                SetupVisualAndText(regularClick);
                stepSequence.Add(regularClick);

                if (HasConsecutiveKeyPressPattern())
                {
                    ConsolidateTextInputSteps();
                }
            }
            else
            {
                ClickData holdClick = new ClickData
                {
                    interaction = InteractionTypeEnum.KeyCodeHold,
                    checkInteraction = InteractionTargetEnum.AnyTarget,
                    GameObjects = new List<UnityEngine.Object>(),
                    tags = new List<string>(),
                    layers = new List<int>(),
                    objectTypes = new List<ObjectTypeEnum>(),
                    keyCodes = new List<KeyCode>(keys)
                };
                SetupVisualAndText(holdClick);
                stepSequence.Add(holdClick);
            }

            SaveClickData();
        }

        // Checks for consecutive key press pattern in sequence
        private bool HasConsecutiveKeyPressPattern()
        {
            if (stepSequence.Count < 5) return false;

            int consecutiveKeyPresses = 0;
            for (int i = stepSequence.Count - 1; i >= 0; i--)
            {
                if (stepSequence[i].interaction != InteractionTypeEnum.KeyCode)
                {
                    break;
                }
                consecutiveKeyPresses++;
            }

            return consecutiveKeyPresses >= 5;
        }

        // Consolidates multiple key press steps into a single text input step
        private void ConsolidateTextInputSteps()
        {
            int firstKeyPressIndex = stepSequence.Count - 1;
            bool foundWASD = false;

            for (int i = stepSequence.Count - 1; i >= 0; i--)
            {
                var step = stepSequence[i];
                if (step.interaction != InteractionTypeEnum.KeyCode)
                {
                    break;
                }

                if (step.keyCodes.Count == 4 &&
                    step.keyCodes.Contains(KeyCode.W) &&
                    step.keyCodes.Contains(KeyCode.A) &&
                    step.keyCodes.Contains(KeyCode.S) &&
                    step.keyCodes.Contains(KeyCode.D))
                {
                    foundWASD = true;
                    continue;
                }

                firstKeyPressIndex = i;
            }

            if (foundWASD && firstKeyPressIndex > 0 &&
                IsWASDStep(stepSequence[firstKeyPressIndex - 1]))
            {
                firstKeyPressIndex = Math.Min(firstKeyPressIndex, stepSequence.Count - 1);
            }

            ClickData textInputStep = new ClickData
            {
                interaction = InteractionTypeEnum.KeyCode,
                checkInteraction = InteractionTargetEnum.AnyTarget,
                GameObjects = new List<UnityEngine.Object>(),
                tags = new List<string>(),
                layers = new List<int>(),
                objectTypes = new List<ObjectTypeEnum>(),
                keyCodes = new List<KeyCode>()
            };
            SetupVisualAndText(textInputStep);

            stepSequence.RemoveRange(firstKeyPressIndex, stepSequence.Count - firstKeyPressIndex);
            stepSequence.Insert(firstKeyPressIndex, textInputStep);
            SaveClickData();
            Debug.Log("[ATM] Consolidated text input steps");
        }

        // Checks if step represents WASD controls
        private bool IsWASDStep(ClickData step)
        {
            return step.interaction == InteractionTypeEnum.KeyCode &&
                   step.keyCodes.Count == 4 &&
                   step.keyCodes.Contains(KeyCode.W) &&
                   step.keyCodes.Contains(KeyCode.A) &&
                   step.keyCodes.Contains(KeyCode.S) &&
                   step.keyCodes.Contains(KeyCode.D);
        }

        // Records keycodes pressed at the same time
        private void StartNewCombination(List<KeyCode> initialKeys)
        {
            bool foundExistingCombo = false;

            if (stepSequence.Count > 0)
            {
                var lastStep = stepSequence[stepSequence.Count - 1];
                if (lastStep.interaction == InteractionTypeEnum.KeyCodeCombo)
                {
                    bool containsAllKeys = true;
                    foreach (var key in lastStep.keyCodes)
                    {
                        if (!initialKeys.Contains(key))
                        {
                            containsAllKeys = false;
                            break;
                        }
                    }

                    if (containsAllKeys)
                    {
                        foundExistingCombo = true;
                        currentComboStep = lastStep;

                        foreach (var key in initialKeys)
                        {
                            if (!currentComboStep.keyCodes.Contains(key))
                            {
                                currentComboStep.keyCodes.Add(key);
                                currentComboKeys.Add(key);
                            }
                            else if (!currentComboKeys.Contains(key))
                            {
                                currentComboKeys.Add(key);
                            }
                        }

                        SetupVisualAndText(currentComboStep);
                        SaveClickData();

                        Debug.Log($"[ATM] Updated existing combo step, current keys: {string.Join(", ", currentComboKeys)}");
                    }
                }
            }

            if (!foundExistingCombo)
            {
                isRecordingCombo = true;
                currentComboKeys = new List<KeyCode>(initialKeys);

                currentComboStep = new ClickData
                {
                    interaction = InteractionTypeEnum.KeyCodeCombo,
                    checkInteraction = InteractionTargetEnum.AnyTarget,
                    GameObjects = new List<UnityEngine.Object>(),
                    tags = new List<string>(),
                    layers = new List<int>(),
                    objectTypes = new List<ObjectTypeEnum>(),
                    keyCodes = new List<KeyCode>(currentComboKeys)
                };

                SetupVisualAndText(currentComboStep);
                stepSequence.Add(currentComboStep);
                SaveClickData();

                Debug.Log($"[ATM] Created new combo step with keys: {string.Join(", ", currentComboKeys)}");
            }

            isRecordingCombo = true;
        }

        // Adds keycodes to currently held combo
        private void AddKeyToCurrentCombo(KeyCode key)
        {
            if (!currentComboKeys.Contains(key))
            {
                currentComboKeys.Add(key);

                currentComboStep.keyCodes.Add(key);

                SetupVisualAndText(currentComboStep);

                SaveClickData();

                Debug.Log($"[ATM] Added key {key} to current combo, current keys: {string.Join(", ", currentComboKeys)}");
            }
        }

        // Updates currently held combo
        private void HandleKeyCodeCombo(List<KeyCode> pressedKeys, List<KeyCode> releasedKeys)
        {
            if (waitForAllKeysRelease)
            {
                if (keyPressStartTimes.Count == 0)
                {
                    waitForAllKeysRelease = false;
                    currentComboKeys.Clear();
                    isRecordingCombo = false;
                    currentComboStep = null;
                    Debug.Log("[ATM] All keys released, can start recording new combos");
                }
                return;
            }

            if (isRecordingCombo && currentComboStep != null)
            {
                foreach (var key in pressedKeys)
                {
                    if (!currentComboKeys.Contains(key))
                    {
                        AddKeyToCurrentCombo(key);
                        if (!keyPressStartTimes.ContainsKey(key))
                        {
                            keyPressStartTimes[key] = Time.time;
                        }
                    }
                }

                bool anyComboKeyReleased = false;
                foreach (var key in releasedKeys)
                {
                    if (currentComboKeys.Contains(key))
                    {
                        anyComboKeyReleased = true;
                        Debug.Log($"[ATM] Combo key {key} released, finalizing combo");
                        break;
                    }
                }

                if (anyComboKeyReleased)
                {
                    FinalizeCombinationStep();
                    waitForAllKeysRelease = true;
                    return;
                }

            }
            else
            {
                int heldKeysCount = 0;
                List<KeyCode> heldKeys = new List<KeyCode>();

                foreach (var keyEntry in keyPressStartTimes)
                {
                    if (!releasedKeys.Contains(keyEntry.Key))
                    {
                        heldKeysCount++;
                        heldKeys.Add(keyEntry.Key);
                    }
                }

                foreach (var key in pressedKeys)
                {
                    if (!keyPressStartTimes.ContainsKey(key))
                    {
                        keyPressStartTimes[key] = Time.time;
                        heldKeysCount++;
                        if (!heldKeys.Contains(key))
                        {
                            heldKeys.Add(key);
                        }
                    }
                }

                if (heldKeysCount >= 2 && !isRecordingCombo)
                {
                    StartNewCombination(heldKeys);
                }
            }
        }

        // Resets combo
        private void FinalizeCombinationStep()
        {
            Debug.Log($"[ATM] Finalized combo step with keys: {string.Join(", ", currentComboKeys)}");
            isRecordingCombo = false;
            currentComboKeys.Clear();
            currentComboStep = null;
        }


        #endregion

        #region Recording
        // Record interaction with object
        private void RecordClick(GameObject obj, ObjectTypeEnum type, Vector3? worldPosition, InteractionTypeEnum interactionType, List<UnityEngine.Object> GameObjects)
        {
            bool hasTarget = obj != null && GameObjects != null && GameObjects.Count > 0;
            bool isRightClick = sceneReferences.inputController.GetRightClickUp();

            if ((interactionType == InteractionTypeEnum.Click || interactionType == InteractionTypeEnum.RightClick) && stepSequence.Count > 0)
            {
                var lastStep = stepSequence[stepSequence.Count - 1];
                float timeSinceLastClick = Time.time - lastClickTime;

                if ((lastStep.interaction == InteractionTypeEnum.Click && !isRightClick && interactionType == InteractionTypeEnum.Click) ||
                    (lastStep.interaction == InteractionTypeEnum.RightClick && isRightClick && interactionType == InteractionTypeEnum.RightClick))
                {
                    if (timeSinceLastClick <= doubleClickTimeWindow &&
                        IsSameTargets(lastStep, new ClickData
                        {
                            checkInteraction = hasTarget ? InteractionTargetEnum.ByGameObject : InteractionTargetEnum.AnyTarget,
                            GameObjects = GameObjects
                        }))
                    {
                        stepSequence.RemoveAt(stepSequence.Count - 1);

                        interactionType = isRightClick ? InteractionTypeEnum.RightDoubleClick : InteractionTypeEnum.DoubleClick;
                        Debug.Log($"[ATM] Converted to {(isRightClick ? "right " : "")}double click in runtime");
                    }
                }
            }

            lastClickTime = Time.time;

            ClickData click = new ClickData
            {
                interaction = interactionType,
                GameObjects = GameObjects ?? new List<UnityEngine.Object>(),
                checkInteraction = hasTarget ? InteractionTargetEnum.ByGameObject : InteractionTargetEnum.AnyTarget,
                tags = new List<string>(),
                layers = new List<int>(),
                objectTypes = new List<ObjectTypeEnum>()
            };

            if (hasTarget)
            {
                foreach (UnityEngine.Object target in GameObjects)
                {
                    if (target is GameObject gameObj)
                    {
                        click.tags.Add(gameObj.tag);
                        click.layers.Add(gameObj.layer);
                        click.objectTypes.Add(GetObjectType(gameObj));
                    }
                }
            }

            SetupVisualAndText(click);

            stepSequence.Add(click);
            SaveClickData();

            Debug.Log($"[ATM] Recorded {interactionType}, HasTarget: {hasTarget}, Objects: {GameObjects.Count}");
        }

        // Start recording sequence
        public void StartTracking()
        {
            ClearClickSequence();
            if (sceneReferences && sceneReferences.inputController)
            {
                sceneReferences.inputController.ResetPinchGesture();
            }
            isPinchSessionActive = false;
            isTracking = true;
            recordingStartTime = Time.time;
        }

        // Stop recording sequence
        public void StopTracking()
        {
            if (!isTracking) return;
            isTracking = false;
            ProcessDoubleClicks();

            SaveClickData();

#if UNITY_EDITOR
            tempSequence = new List<ClickData>(stepSequence);
            shouldPasteAfterExit = true;

            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = false;
            };
#endif
        }
#if UNITY_EDITOR
        // Handle play mode state change
        public static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && shouldPasteAfterExit && tempSequence != null)
            {
                shouldPasteAfterExit = false;

                var instance = UnityEngine.Object.FindObjectOfType<AutomaticTutorialMaker>();
                if (instance != null)
                {
                    instance.stepSequence = new List<ClickData>(tempSequence);
                    EditorUtility.SetDirty(instance);
                    AssetDatabase.SaveAssets();
                }

                tempSequence = null;
            }
        }
#endif
        #endregion

        #region GeneratingDefaultStep
        // Sets up visual elements and text for tutorial step
        private void SetupVisualAndText(ClickData click)
        {
            //click.pointerText = GetInstructionText(click);
            // click.graphicText = click.pointerText;

            string clickpointerText = GetInstructionText(click);
            string clickgraphicText = clickpointerText;
            if (click.checkInteraction == InteractionTargetEnum.AnyTarget)
            {
                click.pointerPrefab = null;
                click.hoverPrefab = null;
                if (click.interaction == InteractionTypeEnum.SwipeDown || click.interaction == InteractionTypeEnum.SwipeRight || click.interaction == InteractionTypeEnum.SwipeUp || click.interaction == InteractionTypeEnum.SwipeLeft)
                {
                    click.graphicPrefab = sceneReferences.defaultUiGraphicSwipeCircle;
                }
                else if (click.interaction == InteractionTypeEnum.JoystickButton || click.interaction == InteractionTypeEnum.JoystickButtonHold)
                {
                    click.graphicPrefab = sceneReferences.defaultJoystickUiGraphic;
                }
                //else if(click.interaction == InteractionTypeEnum.Hold)
                //{
                //    click.graphicPrefab = sceneReferences.defaultUiGraphicHoldCircle;
                //}
                //else if (click.interaction == InteractionTypeEnum.Click)
                //{
                //    click.graphicPrefab = sceneReferences.defaultUiGraphicTouchCircle;
                //}
                else
                {
                    click.graphicPrefab = sceneReferences.defaultUiGraphicText;
                }
            }
            else
            {
                switch (click.interaction)
                {
                    case InteractionTypeEnum.Click:
                    case InteractionTypeEnum.DragAndDrop:
                        click.pointerPrefab = sceneReferences.defaultUiPointer;
                        click.graphicPrefab = sceneReferences.defaultUiGraphicText;
                        click.hoverPrefab = sceneReferences.defaultUiHover;
                        clickpointerText = "";
                        break;
                    case InteractionTypeEnum.ScrollDown:
                    case InteractionTypeEnum.ScrollUp:
                    case InteractionTypeEnum.KeyCode:
                    case InteractionTypeEnum.KeyCodeHold:
                        click.graphicPrefab = sceneReferences.defaultUiGraphicText;
                        break;
                    case InteractionTypeEnum.SwipeDown:
                    case InteractionTypeEnum.SwipeRight:
                    case InteractionTypeEnum.SwipeUp:
                    case InteractionTypeEnum.SwipeLeft:
                        click.graphicPrefab = sceneReferences.defaultUiGraphic;
                        break;
                    case InteractionTypeEnum.RightClick:
                    case InteractionTypeEnum.DoubleClick:
                    case InteractionTypeEnum.RightDoubleClick:
                    case InteractionTypeEnum.Hold:
                    case InteractionTypeEnum.RightHold:
                    case InteractionTypeEnum.Drag:
                    case InteractionTypeEnum.RightDrag:
                    default:
                        click.pointerPrefab = sceneReferences.defaultUiPointerMouse;
                        click.hoverPrefab = sceneReferences.defaultUiHover;
                        break;
                }

            }
            if (!click.graphicPrefab)
            {
                clickgraphicText = "";
            }
            if (!click.pointerPrefab)
            {
                clickpointerText = "";
            }

            // Input Refresh
            if (sceneReferences.inputTextSettings)
            {
                InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;
                int currentStepIndex = stepSequence.Count;
                string currentScene = SceneManager.GetActiveScene().name;
                string currentATM = sceneReferences.currentDeviceATM;
                bool existingStrings = false;
                InputStringsScriptableObject.InputElement newElement = null;
                for (int i = 0; i < localizator.tipStrings.Count; i++)
                {
                    if (localizator.tipStrings[i].stepIndex == currentStepIndex && localizator.tipStrings[i].currentScene == currentScene && localizator.tipStrings[i].currentATM == currentATM)
                    {
                        newElement = localizator.tipStrings[i];
                        existingStrings = true;
                        break;
                    }
                }

                if (!existingStrings)
                {
                    newElement = new InputStringsScriptableObject.InputElement();
                    localizator.tipStrings.Add(newElement);
                    click.localizationReference = newElement;
                }

                newElement.stepIndex = currentStepIndex;
                newElement.currentScene = currentScene;
                newElement.currentATM = currentATM;
                newElement.PointerText = clickpointerText;
                newElement.GraphicText = clickgraphicText;

                click.localizationReference = newElement;
                AutomaticTextRefresher();
            }
            else
            {
                Debug.LogError("[ATM] Set InputTextSettings in Tutorial Scene References.");
            }
        }

        // Generates instruction text based on interaction type and targets
        private string GetInstructionText(ClickData click)
        {
            bool hasSpecificTarget = click.checkInteraction != InteractionTargetEnum.AnyTarget;
            bool hasSingleTarget = hasSpecificTarget && click.GameObjects?.Count == 1;
            bool draggedTargetObject = clickedObjectStartPosition == clickedObjectEndPosition;

            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;

            string targetName = localizator.GetStringText(14); // "this object";
            if (hasSpecificTarget && click.GameObjects != null && click.GameObjects.Count > 0)
            {
                targetName = GetObjectName(click.GameObjects[0]);
            }

            switch (click.interaction)
            {
                case InteractionTypeEnum.Click:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(1) + " " + targetName //$"Click on the {targetName}"
                        : localizator.GetStringText(2); //"Click anywhere";

                case InteractionTypeEnum.RightClick:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(3) + " " + targetName//$"Right-click on the {targetName}"
                        : localizator.GetStringText(4); //"Right-click anywhere";

                case InteractionTypeEnum.DoubleClick:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(5) + " " + targetName//$"Double-click on the {targetName}"
                        : localizator.GetStringText(6); //"Double-click anywhere";

                case InteractionTypeEnum.RightDoubleClick:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(7) + " " + targetName//$"Right double-click on the {targetName}"
                        : localizator.GetStringText(8); //"Right double-click anywhere";

                case InteractionTypeEnum.Drag:
                    return hasSpecificTarget && hasSingleTarget ? localizator.GetStringText(9) + " " + targetName : localizator.GetStringText(10);// "Drag the " + targetName : "Hold click anywhere";

                case InteractionTypeEnum.Hold:
                    return hasSpecificTarget && hasSingleTarget ? localizator.GetStringText(11) + " " + targetName : localizator.GetStringText(10);// "Hold click on the " + targetName : "Hold click anywhere";

                case InteractionTypeEnum.RightHold:
                    return hasSpecificTarget && hasSingleTarget ? localizator.GetStringText(12) + " " + targetName : localizator.GetStringText(13);// "Hold right-click on the " + targetName : "Hold right-click anywhere";

                case InteractionTypeEnum.RightDrag:
                    return hasSpecificTarget && hasSingleTarget ? localizator.GetStringText(9) + " " + targetName : localizator.GetStringText(13);// "Drag the " + targetName + " with right-click" : "Hold right-click anywhere";

                case InteractionTypeEnum.DragAndDrop:
                    string dropTargetName = localizator.GetStringText(15); // "target";
                    if (click.GameObjects != null && click.GameObjects.Count > 1)
                    {
                        dropTargetName = GetObjectName(click.GameObjects[click.GameObjects.Count - 1]);
                    }
                    return localizator.GetStringText(9) + " " + targetName + " " + localizator.GetStringText(16) + " " + "{dropTargetName}";//$"Drag the {targetName} and drop it on the {dropTargetName}";

                case InteractionTypeEnum.ScrollUp:
                    return localizator.GetStringText(17) + " " + localizator.GetStringText(18); //"Scroll mouse wheel up";

                case InteractionTypeEnum.ScrollDown:
                    return localizator.GetStringText(17) + " " + localizator.GetStringText(19); //"Scroll mouse wheel down";
                case InteractionTypeEnum.KeyCodeHold:

                    return click.keyCodes.Count == 1
                        ? localizator.GetStringText(20) + " " + click.keyCodes[0] //$"Hold {click.keyCodes[0]}"
                        : localizator.GetStringText(20) + " " + $" {string.Join(" + ", click.keyCodes)}"; //$"Hold {string.Join(" + ", click.keyCodes)}";

                case InteractionTypeEnum.KeyCode:
                    if (click.keyCodes.Count == 0)
                    {
                        return localizator.GetStringText(21); // "Enter text";
                    }
                    if (click.keyCodes.Count == 4 &&
                        click.keyCodes.Contains(KeyCode.W) &&
                        click.keyCodes.Contains(KeyCode.A) &&
                        click.keyCodes.Contains(KeyCode.S) &&
                        click.keyCodes.Contains(KeyCode.D))
                    {
                        return localizator.GetStringText(22); // "Use WASD keys to move";
                    }
                    return click.keyCodes.Count == 1
                        ? localizator.GetStringText(23) + " " + click.keyCodes[0] //$"Press {click.keyCodes[0]}"
                        : localizator.GetStringText(23) + " " + $" {string.Join(" + ", click.keyCodes)}"; //$"Press {string.Join(" + ", click.keyCodes)}";

                case InteractionTypeEnum.SwipeLeft:
                    return localizator.GetStringText(24) + " " + localizator.GetStringText(25);// "Swipe left";

                case InteractionTypeEnum.SwipeRight:
                    return localizator.GetStringText(24) + " " + localizator.GetStringText(26);//"Swipe right";

                case InteractionTypeEnum.SwipeUp:
                    return localizator.GetStringText(24) + " " + localizator.GetStringText(18);//"Swipe up";

                case InteractionTypeEnum.SwipeDown:
                    return localizator.GetStringText(24) + " " + localizator.GetStringText(19);//"Swipe down";

                case InteractionTypeEnum.JoystickButton:
                    if (click.keyCodes.Count > 0)
                    {
                        KeyCode keyCode = click.keyCodes[0];
                        int buttonIndex = (int)keyCode - (int)KeyCode.JoystickButton0;
                        return $"{GetJoystickButtonDescription(buttonIndex)}";
                    }
                    return localizator.GetStringText(27);// "Use joystick";
                case InteractionTypeEnum.JoystickButtonHold:
                    if (click.keyCodes.Count > 0)
                    {
                        KeyCode keyCode = click.keyCodes[0];
                        int buttonIndex = (int)keyCode - (int)KeyCode.JoystickButton0;
                        return localizator.GetStringText(20) + " " + $"{GetJoystickButtonDescription(buttonIndex)}";//$"Hold {GetJoystickButtonDescription(buttonIndex)}";
                    }
                    return localizator.GetStringText(20) + " " + localizator.GetStringText(28);//"Hold joystick button";
                case InteractionTypeEnum.MiddleClick:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(29) + " " + targetName//$"Middle-click on the {targetName}"
                        : localizator.GetStringText(30);//"Middle-click anywhere";

                case InteractionTypeEnum.MiddleHold:
                    return hasSpecificTarget && hasSingleTarget
                        ? localizator.GetStringText(31) + " " + targetName// $"Hold middle-click on the {targetName}"
                        : localizator.GetStringText(32);//"Hold middle-click anywhere";
                case InteractionTypeEnum.KeyCodeCombo:
                    return localizator.GetStringText(23) + $" {string.Join(" + ", click.keyCodes)}" + " " + localizator.GetStringText(33);//$"Press {string.Join(" + ", click.keyCodes)} together";
                case InteractionTypeEnum.KeyCodeDoublePress:
                    return click.keyCodes.Count == 1
                        ? localizator.GetStringText(34) + " " + click.keyCodes[0] //$"Double press {click.keyCodes[0]}"
                        : localizator.GetStringText(34) + $" {string.Join(" + ", click.keyCodes)}"; //$"Double press {string.Join(" + ", click.keyCodes)}";
                case InteractionTypeEnum.PinchIn:
                    return localizator.GetStringText(35);// "Pinch in (zoom out)";

                case InteractionTypeEnum.PinchOut:
                    return localizator.GetStringText(36); // "Pinch out (zoom in)";

                case InteractionTypeEnum.PinchRotate:
                    return localizator.GetStringText(37); // "Rotate with pinch gesture";
                default:
                    return localizator.GetStringText(38); // "Complete this action";
            }
        }

        // Joystick mapping namings
        private string GetJoystickButtonDescription(int buttonIndex)
        {
            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;

            if (buttonIndex == sceneReferences.inputController.buttonMappings[14].buttonIndex) return localizator.GetStringText(39); //"A (Cross)";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[15].buttonIndex) return localizator.GetStringText(40); //"B (Circle)";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[16].buttonIndex) return localizator.GetStringText(41); //"X (Square)";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[17].buttonIndex) return localizator.GetStringText(42); //"Y (Triangle)";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[18].buttonIndex) return localizator.GetStringText(43); //"LB";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[19].buttonIndex) return localizator.GetStringText(44); //"RB";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[20].buttonIndex) return localizator.GetStringText(45); //"Back/Select";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[21].buttonIndex) return localizator.GetStringText(46); //"Start";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[22].buttonIndex) return localizator.GetStringText(47); //"Left Stick";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[23].buttonIndex) return localizator.GetStringText(48); //"Right Stick";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[27].buttonIndex || buttonIndex == sceneReferences.inputController.buttonMappings[0].buttonIndex) return localizator.GetStringText(49); //"D-Pad Right";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[26].buttonIndex || buttonIndex == sceneReferences.inputController.buttonMappings[1].buttonIndex) return localizator.GetStringText(50); //"D-Pad Left";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[24].buttonIndex || buttonIndex == sceneReferences.inputController.buttonMappings[2].buttonIndex) return localizator.GetStringText(51); //"D-Pad Up";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[25].buttonIndex || buttonIndex == sceneReferences.inputController.buttonMappings[3].buttonIndex) return localizator.GetStringText(52); //"D-Pad Down";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[4].buttonIndex) return localizator.GetStringText(53); //"RT";
            else if (buttonIndex == sceneReferences.inputController.buttonMappings[5].buttonIndex) return localizator.GetStringText(54); //"LT";
            else if (
    buttonIndex == sceneReferences.inputController.buttonMappings[6].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[7].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[8].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[9].buttonIndex
) return localizator.GetStringText(55); //"Use Left Stick";

            else if (
    buttonIndex == sceneReferences.inputController.buttonMappings[10].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[11].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[12].buttonIndex ||
    buttonIndex == sceneReferences.inputController.buttonMappings[13].buttonIndex
) return localizator.GetStringText(56); //"Use Right Stick";

            else return localizator.GetStringText(57) + " " + buttonIndex; //$"Button {buttonIndex}";

        }

        // Gets display name for tutorial object
        private string GetObjectName(UnityEngine.Object obj)
        {
            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;
            if (obj == null) return localizator.GetStringText(14);//"this object";

            string name = obj.name;
            int dotIndex = name.IndexOf('.');
            return dotIndex > 0 ? name.Substring(0, dotIndex) : name;
        }

        #endregion

        #region SequenceActions
        // Save sequence data
        private void SaveClickData()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // Load sequence data
        private void LoadClickData()
        {
            if (stepSequence == null)
            {
                stepSequence = new List<ClickData>();
            }
        }

        // Copy sequence to buffer
        public void CopyClickSequence()
        {
            if (stepSequence != null && stepSequence.Count > 0)
            {
                copiedClickSequence = new List<ClickData>(stepSequence);
                Debug.Log("[ATM] Click sequence copied.");
            }
            else
            {
                Debug.LogWarning("[ATM] No sequence to copy.");
            }
        }

        // Paste sequence from buffer
        public void PasteClickSequence()
        {
            if (copiedClickSequence != null)
            {
                stepSequence = new List<ClickData>(copiedClickSequence);
                Debug.Log("[ATM] Click sequence pasted.");
                SaveClickData();

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }
            else
            {
                Debug.LogWarning("[ATM] No click sequence to paste.");
            }
        }

        // Clear current sequence
        public void ClearClickSequence()
        {
            isPinchSessionActive = false;
            keyPressStartTimes.Clear();
            keyPressDurations.Clear();
            lastKeyPressTimes.Clear();
            stepSequence = new List<ClickData>();
            if (Application.isPlaying && sceneReferences?.visualManager != null)
            {
                sceneReferences.visualManager.DisableAllVisuals();
            }
        }

        // Check if sequence exists
        public bool HasClickSequence()
        {
            return stepSequence != null && stepSequence.Count > 0;
        }

        public void RefreshLocalizationReferences()
        {
            SetReference();
            if (!sceneReferences || !sceneReferences.inputTextSettings)
            {
                return;
            }            

            bool refresh = false;
            for(int i = 0; i < stepSequence.Count; i++)
            {
                ClickData step = stepSequence[i];
                if ((!string.IsNullOrEmpty(step.pointerText)))
                {
                    refresh = true;
                    break;
                }
                else if ((!string.IsNullOrEmpty(step.worldPointerText)))
                {
                    refresh = true;
                    break;
                }
                else if ((!string.IsNullOrEmpty(step.graphicText)))
                {
                    refresh = true;
                    break;
                }
                else if ((!string.IsNullOrEmpty(step.worldGraphicText)))
                {
                    refresh = true;
                    break;
                }
            }

            if (refresh)
            {
                AutomaticTextFiller();
            }

            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;
            string currentScene = SceneManager.GetActiveScene().name;
            string currentATM = sceneReferences.currentDeviceATM;
            bool anyUpdated = false;

            for (int i = 0; i < stepSequence.Count; i++)
            {
                ClickData step = stepSequence[i];

                if (step.localizationReference != null)
                {
                    for (int j = 0; j < localizator.tipStrings.Count; j++)
                    {
                        if (localizator.tipStrings[j].stepIndex == i &&
                            localizator.tipStrings[j].currentScene == currentScene && localizator.tipStrings[j].currentATM == currentATM)
                        {
                            if (step.localizationReference != localizator.tipStrings[j])
                            {
                                anyUpdated = true;
                            }
                            step.localizationReference = localizator.tipStrings[j];
                        }
                    }
                }
            }

            if (anyUpdated)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(sceneReferences.inputTextSettings);
#endif
            }
        }
        #endregion

        #region Localization
        // Updates text tips if anything changed
        private void AutomaticTextRefresher()
        {
            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;
            string currentScene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < stepSequence.Count; i++)
            {
                ClickData step = stepSequence[i];
                for (int j = 0; j < localizator.tipStrings.Count; j++)
                {
                    if (localizator.tipStrings[j].stepIndex == i && localizator.tipStrings[j].currentScene == currentScene)
                    {
                        step.localizationReference = localizator.tipStrings[j];
                        sceneReferences.visualManager.UpdateTranslation(step);
                        break;
                    }
                }
            }
        }

        // Auto trasfers old text system to new
        private void AutomaticTextFiller()
        {
            InputStringsScriptableObject localizator = sceneReferences.inputTextSettings;
            string currentScene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < stepSequence.Count; i++)
            {
                ClickData step = stepSequence[i];
                bool existingStrings = false;
                InputStringsScriptableObject.InputElement currentReference = null;
                for (int j = 0; j < localizator.tipStrings.Count; j++)
                {
                    if (localizator.tipStrings[j].stepIndex == i && localizator.tipStrings[j].currentScene == currentScene)
                    {
                        currentReference = localizator.tipStrings[j];
                        existingStrings = true;
                        break;
                    }
                }

                if (!existingStrings)
                {
                    InputStringsScriptableObject.InputElement newElement = new InputStringsScriptableObject.InputElement();
                    newElement.stepIndex = i;
                    newElement.currentScene = currentScene;

                    localizator.tipStrings.Add(newElement);
                    step.localizationReference = newElement;
                    currentReference = newElement;
                }

                if ((!string.IsNullOrEmpty(step.pointerText)))
                {
                    currentReference.PointerText = step.pointerText;
                }
                if ((!string.IsNullOrEmpty(step.worldPointerText)))
                {
                    currentReference.WorldPointerText = step.worldPointerText;
                }
                if ((!string.IsNullOrEmpty(step.graphicText)))
                {
                    currentReference.PointerText = step.graphicText;
                }
                if ((!string.IsNullOrEmpty(step.worldGraphicText)))
                {
                    currentReference.WorldPointerText = step.worldGraphicText;
                }
                step.pointerText = "";
                step.worldPointerText = "";
                step.graphicText = "";
                step.worldGraphicText = "";
            }
            RefreshLocalizationReferences();
        }
        #endregion
    }

    #region Editor
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AutomaticTutorialMaker))]
    public class AutomaticTutorialMakerEditor : UnityEditor.Editor
    {
        private bool isSequenceCopied = false; // Copy state
        private bool shouldPasteAfterExit = false; // Delayed paste flag
        private bool languageFoldout = true; // Language

        // Draws custom inspector GUI
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var clickTracker = (AutomaticTutorialMaker)target;
            if (clickTracker.sceneReferences != null && clickTracker.sceneReferences.inputTextSettings != null)
            {
                var inputSettings = clickTracker.sceneReferences.inputTextSettings;

                if (languageFoldout)
                {
                    EditorGUI.indentLevel++;
                    InputStringsScriptableObject.Language currentLanguage = inputSettings._currentLanguage;
                    InputStringsScriptableObject.Language newLanguage =
                        (InputStringsScriptableObject.Language)EditorGUILayout.EnumPopup("Current Language", currentLanguage);

                    if (newLanguage != currentLanguage)
                    {
                        if (newLanguage != InputStringsScriptableObject.Language.FoundByString)
                        {
                            inputSettings.ChangeLanguage(newLanguage);
                            EditorUtility.SetDirty(inputSettings);

                            clickTracker.RefreshLocalizationReferences();
                        }
                        else
                        {
                            Debug.LogWarning("[ATM] Cannot directly select FoundByString language. Call ChangeLanguage(string languageName) instead.");
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
           
            EditorGUILayout.Space();

            GUI.enabled = Application.isPlaying;
            GUI.backgroundColor = clickTracker.IsTracking ? Color.green : (Application.isPlaying ? new Color(0.85f, 0.85f, 0.85f) : Color.gray);
            var buttonHeight = GUILayout.Height(40);

            if (GUILayout.Button(clickTracker.IsTracking ? "Stop Recording" : "Start Recording", buttonHeight))
            {
                if (clickTracker.IsTracking)
                {
                    clickTracker.StopTracking();
                    clickTracker.CopyClickSequence();

                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.isPlaying = false;
                    };
                }
                else
                {
                    clickTracker.StartTracking();
                }
            }
            GUILayout.Space(10);

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            var clickSequenceProp = serializedObject.FindProperty("stepSequence");
            EditorGUILayout.PropertyField(clickSequenceProp, true);

            if (GUI.changed)
            {
                SyncTextsWithLocalization(clickTracker);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = clickTracker.HasClickSequence();
            GUI.backgroundColor = isSequenceCopied ? Color.gray : Color.gray;
            if (GUILayout.Button(isSequenceCopied ? "Copied" : "Copy Sequence"))
            {
                clickTracker.CopyClickSequence();
                isSequenceCopied = true;
            }

            GUI.enabled = true;
            GUI.backgroundColor = isSequenceCopied ? new Color(0.85f, 0.85f, 0.85f) : Color.gray;
            if (GUILayout.Button("Paste Sequence"))
            {
                clickTracker.PasteClickSequence();
                isSequenceCopied = false;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUI.backgroundColor = Color.gray;
            if (GUILayout.Button("Clear Sequence"))
            {
                clickTracker.ClearClickSequence();
                isSequenceCopied = false;
            }
            GUI.backgroundColor = Color.white;

            // Draw the sceneReferences field at the bottom of the inspector
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneReferences"));

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(target);
            }
        }

        private void SyncTextsWithLocalization(AutomaticTutorialMaker clickTracker)
        {
            if (clickTracker.sceneReferences == null || clickTracker.sceneReferences.inputTextSettings == null)
                return;

            var localizator = clickTracker.sceneReferences.inputTextSettings;
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string currentATM = clickTracker.sceneReferences.currentDeviceATM;
            for (int i = 0; i < clickTracker.stepSequence.Count; i++)
            {
                var step = clickTracker.stepSequence[i];

                InputStringsScriptableObject.InputElement element = null;
                for (int j = 0; j < localizator.tipStrings.Count; j++)
                {
                    if (localizator.tipStrings[j].stepIndex == i && localizator.tipStrings[j].currentScene == currentScene && localizator.tipStrings[j].currentATM == currentATM)
                    {
                        element = localizator.tipStrings[j];
                        break;
                    }
                }

                if (element == null)
                {
                    element = new InputStringsScriptableObject.InputElement();
                    element.stepIndex = i;
                    element.currentScene = currentScene;
                    element.currentATM = currentATM;
                    localizator.tipStrings.Add(element);
                }

                step.localizationReference = element;

                if (!string.IsNullOrEmpty(step.pointerText))
                {
                    element.PointerText = step.pointerText;
                    step.pointerText = "";
                }

                if (!string.IsNullOrEmpty(step.worldPointerText))
                {
                    element.WorldPointerText = step.worldPointerText;
                    step.worldPointerText = "";
                }

                if (!string.IsNullOrEmpty(step.graphicText))
                {
                    element.GraphicText = step.graphicText;
                    step.graphicText = "";
                }

                if (!string.IsNullOrEmpty(step.worldGraphicText))
                {
                    element.WorldGraphicText = step.worldGraphicText;
                    step.worldGraphicText = "";
                }
            }

            EditorUtility.SetDirty(localizator);
            EditorUtility.SetDirty(clickTracker);
        }

        // Subscribe to play mode state changes
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= AutomaticTutorialMaker.HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += AutomaticTutorialMaker.HandlePlayModeStateChanged;

            var clickTracker = (AutomaticTutorialMaker)target;
            clickTracker.RefreshLocalizationReferences();
        }

        // Unsubscribe from play mode state changes
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= AutomaticTutorialMaker.HandlePlayModeStateChanged;
        }

        // Handle play mode state changes
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && shouldPasteAfterExit)
            {
                shouldPasteAfterExit = false;
                var clickTracker = (AutomaticTutorialMaker)target;
                EditorApplication.delayCall += () =>
                {
                    clickTracker.PasteClickSequence();
                };
            }
        }
    }
#endif
    #endregion
}