using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using static AutomaticTutorialMaker.TutorialSceneReferences;

namespace AutomaticTutorialMaker
{
    public class StepSequencePlayer : MonoBehaviour
    {
        #region Variables
       [Grayed] public TutorialSceneReferences sceneReferences; // Main container for tutorial components
        private string progressFilePath; // Path for saving tutorial progress
        [SerializeField][ReadOnly] public int currentStepIndex = -1; // Index of current tutorial step
        [ReadOnly][SerializeField] private bool waitingForManualStart = false; // Flag for manual step initialization

        private GameObject startClickObject; // Object where interaction started
        private GameObject endClickObject; // Object where interaction ended
        private float clickStartTime; // Time when interaction started
        private bool isHolding; // Flag for hold interaction state
        private Vector3 clickStartPosition; // Starting position of interaction
        private HashSet<int> activeStepIndices = new HashSet<int>(); // Currently active step indices
        private float lastClickTime = 0f; // Time of last click
        private List<int> cachedActiveIndices = new List<int>(); // Cached list of active indices

        private Vector3 clickedObjectStartPosition;
        private Vector3 clickedObjectEndPosition;

        private bool isDoubleClickPossible = false; // Flag for possible double click
        private GameObject lastClickedObject = null; // Last clicked object for double click
        private bool isRightDoubleClickPossible = false; // Flag for possible right double click
        private GameObject lastRightClickedObject = null; // Last right-clicked object
        private float lastRightClickTime = 0f; // Time of last right click
        private Dictionary<int, HashSet<KeyCode>> pressedKeysProgress = new Dictionary<int, HashSet<KeyCode>>(); // Progress of pressed keys

        private float lastMiddleClickTime = 0f; // Time of last middle click
        private Dictionary<int, float> joystickButtonPressTimes = new Dictionary<int, float>(); 
        private HashSet<int> heldJoystickButtons = new HashSet<int>();
        private Dictionary<int, float> joystickButtonHoldDurations = new Dictionary<int, float>();

        private Dictionary<KeyCode, float> keyPressStartTimes = new Dictionary<KeyCode, float>();
        private Dictionary<KeyCode, float> keyPressDurations = new Dictionary<KeyCode, float>();
        private List<KeyCode> heldKeys = new List<KeyCode>();
        private HashSet<KeyCode> currentlyHeldKeys = new HashSet<KeyCode>();

        private bool hasKeyboardInteractionSteps = false;

        private Dictionary<int, float> pendingSteps = new Dictionary<int, float>(); // Delayed steps

        private Dictionary<KeyCode, float> lastKeyPressTimes = new Dictionary<KeyCode, float>();
        private float keyDoublePressTimeWindow = 0.3f;
        private Dictionary<KeyCode, bool> possibleDoublePressKeys = new Dictionary<KeyCode, bool>();

        private bool isPinchDetected = false;
        private InteractionTypeEnum currentPinchType = InteractionTypeEnum.ManuallyCall;
        private float lastPinchTime = 0f;
        #endregion

        #region Initialization
        public void Initialize()
        {
            pressedKeysProgress = new Dictionary<int, HashSet<KeyCode>>();
            string baseDirectory = Path.Combine(Application.persistentDataPath, "TutorialProgress");
            Directory.CreateDirectory(baseDirectory);
            progressFilePath = GetSceneSpecificPath();
            LoadProgress();
            if (CanStartTutorial())
            {
                FindFirstUncompletedStep();
            }
        }
        
        // Initializes a tutorial step
        private void InitializeStep(int stepIndex)
        {
            var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
            step.stepState = StateEnum.Displaying;
            activeStepIndices.Add(stepIndex);

            if (step.onStepStart != null)
            {
                step.onStepStart.Invoke();
            }
            sceneReferences.visualManager.UpdateVisuals(step);
            SaveProgress();
            LogStepInfo(step);
        }

        // Sets up and begins execution of a tutorial step
        private void InitializeStep(int stepIndex, bool isAutoStart)
        {
            if (currentStepIndex >= 0)
            {
                var previousStep = sceneReferences.tutorialMaker.stepSequence[currentStepIndex];
                sceneReferences.visualManager.DisableVisualsForStep(previousStep);
            }

            currentStepIndex = stepIndex;
            var currentStep = sceneReferences.tutorialMaker.stepSequence[currentStepIndex];
            currentStep.stepState = StateEnum.Displaying;

            if (currentStep.onStepStart != null)
            {
                currentStep.onStepStart.Invoke();
            }
            sceneReferences.visualManager.UpdateVisuals(currentStep);

            if (!isAutoStart)
            {
                waitingForManualStart = false;
            }

            SaveProgress();
            LogStepInfo(currentStep);
        }

        // Checks if tutorial can be started
        private bool CanStartTutorial()
        {
            return sceneReferences.tutorialMaker != null &&
                   sceneReferences.tutorialMaker.stepSequence != null &&
                   sceneReferences.tutorialMaker.stepSequence.Count > 0 &&
                   (sceneReferences.tutorialMaker.stepSequence.Exists(step => step.stepState == StateEnum.NotDoneYet) || sceneReferences.tutorialMaker.stepSequence.Exists(step => step.stepState == StateEnum.Displaying));
        }

        // Locates and initiates first incomplete tutorial step
        private void FindFirstUncompletedStep()
        {
            int nextIndex = FindNextUncompletedStepIndex();
            if (nextIndex != -1)
            {
                var nextStep = sceneReferences.tutorialMaker.stepSequence[nextIndex];
                if (nextStep.startStep == StartTypeEnum.ManuallyCall)
                {
                    waitingForManualStart = true;
                    Debug.Log($"[ATM] Step {nextIndex} requires manual start. Waiting for StartSpecificStep call.");
                    return;
                }

                StartStep(nextIndex);
            }
        }
        private bool ValidateStepIndex(int stepIndex)
        {
            if (sceneReferences.tutorialMaker == null || sceneReferences.tutorialMaker.stepSequence == null)
            {
                Debug.LogWarning("[ATM] Tutorial maker or step sequence is null");
                return false;
            }

            if (stepIndex < 0 || stepIndex >= sceneReferences.tutorialMaker.stepSequence.Count)
            {
                Debug.LogWarning($"[ATM] Step index {stepIndex} is out of range");
                return false;
            }

            return true;
        }

        // Finds index of next uncompleted step
        private int FindNextUncompletedStepIndex()
        {
            for (int i = 0; i < sceneReferences.tutorialMaker.stepSequence.Count; i++)
            {
                if (sceneReferences.tutorialMaker.stepSequence[i].stepState != StateEnum.Done)
                {
                    return i;
                }
            }
            return -1;
        }

        // Delayed Steps Processing
        private void ProcessDelayedSteps()
        {
            List<int> completedSteps = new List<int>();

            foreach (var kvp in pendingSteps.ToList())
            {
                int stepIndex = kvp.Key;
                float remainingTime = kvp.Value - Time.deltaTime;
                sceneReferences.tutorialMaker.stepSequence[stepIndex].blockedByTimeDelay = remainingTime.ToString("F2");
                if (remainingTime <= 0)
                {
                    completedSteps.Add(stepIndex);
                    StartStepAfterDelay(stepIndex);
                }
                else
                {
                    pendingSteps[stepIndex] = remainingTime; 
                }
            }

            foreach (int stepIndex in completedSteps)
            {
                pendingSteps.Remove(stepIndex);
            }
        }

        #endregion

        #region CheckingInteractions
        // Handles main tutorial update loop
        private void Update()
        {
            if (!sceneReferences || !sceneReferences.inputController) return;
            if (sceneReferences.tutorialMaker == null || sceneReferences.tutorialMaker.stepSequence.Count == 0)
            {
                this.enabled = false;
                Debug.Log("[ATM] Tutorial has not yet been recorded...");
                return;
            }

            if (!CanStartTutorial() || waitingForManualStart) return;

            if (!sceneReferences.inputController.IsMobileDevice)
            {
                CheckScrollInteractions();
                if (hasKeyboardInteractionSteps)
                {
                    CheckKeyboardInteractions();
                }

                if (sceneReferences.inputController.isJoystickConnected)
                {
                    CheckJoystickInteractions();
                }
            }

            CheckPinchInteractions();

            ProcessDelayedSteps();

            bool isInputDown = sceneReferences.inputController.GetInputDown() ||
                     sceneReferences.inputController.GetRightClickDown() ||
                     sceneReferences.inputController.GetMiddleClickDown();
            bool isInputUp = sceneReferences.inputController.GetInputUp() ||
                             sceneReferences.inputController.GetRightClickUp() ||
                             sceneReferences.inputController.GetMiddleClickUp();

            if (isHolding)
            {
                float currentDuration = sceneReferences.inputController.GetInputDuration(clickStartTime);

                cachedActiveIndices.Clear();
                cachedActiveIndices.AddRange(activeStepIndices);

                foreach (int stepIndex in cachedActiveIndices)
                {
                    var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                    if (step.stepState == StateEnum.Displaying && step.interaction != InteractionTypeEnum.ManuallyCall && !step.blockedByButton && !step.blockedByTime)
                    {
                        float distance = 0;
                        if (startClickObject != null && (step.interaction == InteractionTypeEnum.Drag || step.interaction == InteractionTypeEnum.RightDrag))
                        {
                            clickedObjectEndPosition = startClickObject.transform.position;
                            distance = Vector3.Distance(clickedObjectStartPosition, clickedObjectEndPosition);
                        }
                        if ((step.interaction == InteractionTypeEnum.Hold ||
                             step.interaction == InteractionTypeEnum.RightHold ||
                             step.interaction == InteractionTypeEnum.MiddleHold) &&
                            currentDuration > sceneReferences.inputController.minHoldDuration)
                        {
                            bool isRightHold = step.interaction == InteractionTypeEnum.RightHold;
                            bool isMiddleHold = step.interaction == InteractionTypeEnum.MiddleHold;
                            bool correctButton;

                            if (isRightHold)
                            {
                                correctButton = sceneReferences.inputController.GetInputR() && !sceneReferences.inputController.GetInputL();
                            }
                            else if (isMiddleHold)
                            {
                                correctButton = sceneReferences.inputController.GetMiddleClick();
                            }
                            else
                            {
                                correctButton = sceneReferences.inputController.GetInputL() && !sceneReferences.inputController.GetInputR();
                            }

                            if (correctButton && (CheckInteractionTarget(step)))
                            {
                                Debug.Log($"[ATM] Step {stepIndex} - Completing hold step during hold. IsRightHold: {isRightHold}, IsMiddleHold: {isMiddleHold}");
                                CompleteStep(stepIndex);
                                startClickObject = null;
                                isHolding = false;
                                return;
                            }
                        }
                        else if((step.interaction == InteractionTypeEnum.Drag || step.interaction == InteractionTypeEnum.RightDrag) && distance > sceneReferences.inputController.minDragDistance)
                        {
                            bool isRightHold = step.interaction == InteractionTypeEnum.RightDrag;
                            bool correctButton;

                            if (isRightHold)
                            {
                                correctButton = sceneReferences.inputController.GetInputR() && !sceneReferences.inputController.GetInputL();
                            }
                            else
                            {
                                correctButton = sceneReferences.inputController.GetInputL() && !sceneReferences.inputController.GetInputR();
                            }

                            if (correctButton && (CheckInteractionTarget(step)))
                            {
                                bool draggedTargetObject = clickedObjectStartPosition == clickedObjectEndPosition;
                                if (!draggedTargetObject)
                                {
                                    Debug.Log($"[ATM] Step {stepIndex} - Completing drag step. IsRightDrag: {isRightHold}");
                                    CompleteStep(stepIndex);
                                    startClickObject = null;
                                    isHolding = false;
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (isInputDown)
            {
                HandleStepInputDown();
            }

            else if (isInputUp)
            {
                float inputDuration = sceneReferences.inputController.GetInputDuration(clickStartTime);
                ObjectTypeEnum endObjectType;
                endClickObject = sceneReferences.inputController.GetClickedObject(out endObjectType);

                cachedActiveIndices.Clear();
                cachedActiveIndices.AddRange(activeStepIndices);

                foreach (int stepIndex in cachedActiveIndices)
                {
                    var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                    if (step.stepState == StateEnum.Displaying && step.interaction != InteractionTypeEnum.ManuallyCall && !step.blockedByButton && !step.blockedByTime)
                    {
                        if (step.interaction != InteractionTypeEnum.Hold &&
                            step.interaction != InteractionTypeEnum.RightHold)
                        {
                            bool interactionPassed = CheckInteractionType(
                                step.interaction,
                                inputDuration,
                                startClickObject,
                                endClickObject
                            );

                            Debug.Log($"[ATM] Step {stepIndex} - InteractionPassed: {interactionPassed}");

                            if (interactionPassed && CheckInteractionTarget(step))
                            {
                                Debug.Log($"[ATM] Step {stepIndex} - Completing step...");
                                CompleteStep(stepIndex);
                                if (step.interaction == InteractionTypeEnum.Click)
                                {
                                    lastClickTime = Time.time;
                                }
                            }
                        }
                    }
                    if (step.blockedByButton && !step.fakeblockedByButton)
                    {
                        step.blockedByButton = false;
                    }

                }

                startClickObject = null;
                endClickObject = null;
                isHolding = false;

                SaveProgress();
            }

        }


        // Validates pinch interactions for tutorial steps
        private void CheckPinchInteractions()
        {          
            sceneReferences.inputController.UpdatePinchGesture();

            InteractionTypeEnum? pinchType = sceneReferences.inputController.GetCurrentPinchType();
            bool isPinchActive = sceneReferences.inputController.IsPinching();

            if (isPinchActive && pinchType.HasValue && !isPinchDetected)
            {
                isPinchDetected = true;
                currentPinchType = pinchType.Value;
                lastPinchTime = Time.time;

                CheckPinchSteps(currentPinchType);
            }
            else if (!isPinchActive && isPinchDetected)
            {
                    isPinchDetected = false;
                    currentPinchType = InteractionTypeEnum.ManuallyCall;                
            }
        }

        private void CheckPinchSteps(InteractionTypeEnum pinchType)
        {
            cachedActiveIndices.Clear();
            cachedActiveIndices.AddRange(activeStepIndices);

            foreach (int stepIndex in cachedActiveIndices)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState != StateEnum.Displaying ||
                    step.blockedByButton ||
                    step.blockedByTime) continue;

                if (step.interaction == pinchType)
                {
                    Debug.Log($"[ATM] Step {stepIndex} - {pinchType} gesture detected and matches expected interaction");
                    CompleteStep(stepIndex);
                }
            }
        }

        // Validates joystick interactions for tutorial steps
        private void CheckJoystickInteractions()
        {
            if (!sceneReferences.inputController.isJoystickConnected) return;

            var pressedButtons = sceneReferences.inputController.GetJoystickButtonsDown();
            var releasedButtons = sceneReferences.inputController.GetJoystickButtonsUp();

            foreach (var button in heldJoystickButtons.ToList())
            {
                if (joystickButtonHoldDurations.ContainsKey(button))
                {
                    joystickButtonHoldDurations[button] += Time.deltaTime;
                }
                else
                {
                    joystickButtonHoldDurations[button] = 0f;
                }
            }

            foreach (var button in pressedButtons)
            {
                if (!heldJoystickButtons.Contains(button))
                {
                    heldJoystickButtons.Add(button);
                    joystickButtonHoldDurations[button] = 0f;
                }

                if (sceneReferences.inputController.IsStickButton(button)) //  sceneReferences.inputController.IsDpadButton(button) || sceneReferences.inputController.IsTriggerButton(button)
                {
                    releasedButtons.Add(button);
                }
            }

            foreach (var button in releasedButtons)
            {
                if (heldJoystickButtons.Contains(button))
                {
                    Debug.Log($"[ATM] Joystick button {button} released after {joystickButtonHoldDurations[button]:F2} seconds");
                    heldJoystickButtons.Remove(button);
                }
            }

            cachedActiveIndices.Clear();
            cachedActiveIndices.AddRange(activeStepIndices);

            foreach (int stepIndex in cachedActiveIndices)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState != StateEnum.Displaying ||
                   (step.interaction != InteractionTypeEnum.JoystickButton &&
                    step.interaction != InteractionTypeEnum.JoystickButtonHold))
                    continue;

                var requiredButtons = step.keyCodes
                    .Select(k => (int)k - (int)KeyCode.JoystickButton0)
                    .ToList();

                bool isLeftStickGroup = requiredButtons.All(IsLeftStickButton);
                bool isRightStickGroup = requiredButtons.All(IsRightStickButton);

                bool isCompleted = false;

                if (step.interaction == InteractionTypeEnum.JoystickButton)
                {
                    if (requiredButtons.All(b => joystickButtonHoldDurations.ContainsKey(b) &&
                             joystickButtonHoldDurations[b] < sceneReferences.inputController.minHoldDuration))

                    {
                        if (isLeftStickGroup)
                        {
                            isCompleted = releasedButtons.Any(IsLeftStickButton);
                        }
                        else if (isRightStickGroup)
                        {
                            isCompleted = releasedButtons.Any(IsRightStickButton);
                        }
                        else
                        {
                            isCompleted = requiredButtons.All(b => releasedButtons.Contains(b));
                        }
                    }
                }
                else if (step.interaction == InteractionTypeEnum.JoystickButtonHold)
                {
                    var relevantButtons = heldJoystickButtons
                        .Where(b => joystickButtonHoldDurations.ContainsKey(b) &&
                                  joystickButtonHoldDurations[b] >= sceneReferences.inputController.minHoldDuration)
                        .ToList();

                    if (isLeftStickGroup)
                    {
                        isCompleted = relevantButtons.Any(IsLeftStickButton);
                    }
                    else if (isRightStickGroup)
                    {
                        isCompleted = relevantButtons.Any(IsRightStickButton);
                    }
                    else
                    {
                        isCompleted = requiredButtons.All(b => relevantButtons.Contains(b));
                    }
                }

                if (isCompleted)
                {
                    Debug.Log($"[ATM] Step {stepIndex} - Joystick interaction completed ({step.interaction})");
                    CompleteStep(stepIndex);

                    foreach (var btn in requiredButtons)
                    {
                        if (heldJoystickButtons.Contains(btn))
                        {
                            heldJoystickButtons.Remove(btn);
                            joystickButtonHoldDurations.Remove(btn);
                        }
                    }
                }
            }
        }

        private bool IsLeftStickButton(int buttonIndex)
    => buttonIndex == sceneReferences.inputController.buttonMappings[6].buttonIndex ||
       buttonIndex == sceneReferences.inputController.buttonMappings[7].buttonIndex ||
       buttonIndex == sceneReferences.inputController.buttonMappings[8].buttonIndex ||
       buttonIndex == sceneReferences.inputController.buttonMappings[9].buttonIndex;

        private bool IsRightStickButton(int buttonIndex)
            => buttonIndex == sceneReferences.inputController.buttonMappings[10].buttonIndex ||
               buttonIndex == sceneReferences.inputController.buttonMappings[11].buttonIndex ||
               buttonIndex == sceneReferences.inputController.buttonMappings[12].buttonIndex ||
               buttonIndex == sceneReferences.inputController.buttonMappings[13].buttonIndex;

        private void CheckKeyComboSteps()
        {
            if (currentlyHeldKeys.Count < 2)
                return;

            cachedActiveIndices.Clear();
            cachedActiveIndices.AddRange(activeStepIndices);

            foreach (int stepIndex in cachedActiveIndices)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState != StateEnum.Displaying ||
                    step.interaction != InteractionTypeEnum.KeyCodeCombo ||
                    step.blockedByButton ||
                    step.blockedByTime)
                    continue;

                bool allKeysPressed = true;
                foreach (var requiredKey in step.keyCodes)
                {
                    if (!currentlyHeldKeys.Contains(requiredKey))
                    {
                        allKeysPressed = false;
                        break;
                    }
                }

                if (allKeysPressed)
                {
                    Debug.Log($"[ATM] Step {stepIndex} - KeyCodeCombo completed! Required keys: {string.Join(", ", step.keyCodes)}");
                    CompleteStep(stepIndex);

                }
            }
        }

        // Validates keyboard interactions for tutorial steps
        private void CheckKeyboardInteractions()
        {
            sceneReferences.inputController.UpdateKeyboardState();
            var pressedKeys = sceneReferences.inputController.GetCurrentPressedKeys();
            var autoReleasedKeys = new List<KeyCode>();

            foreach (var key in pressedKeys)
            {
                if (!keyPressStartTimes.ContainsKey(key))
                {
                    heldKeys.Add(key);
                    if (!currentlyHeldKeys.Contains(key))
                    {
                        currentlyHeldKeys.Add(key);
                    }
                    keyPressStartTimes[key] = Time.time;
                    keyPressDurations[key] = 0;
                }
            }

            foreach (var key in heldKeys)
            {
                keyPressDurations[key] += Time.deltaTime;
                if (keyPressDurations[key] > sceneReferences.inputController.minHoldDuration)
                {
                    autoReleasedKeys.Add(key);
                }
            }
            var releasedKeys = sceneReferences.inputController.GetReleasedKeys();
            foreach (var key in releasedKeys)
            {
                if (currentlyHeldKeys.Contains(key))
                {
                    currentlyHeldKeys.Remove(key);
                }
            }
            releasedKeys.AddRange(autoReleasedKeys);

            if (currentlyHeldKeys.Count >= 2)
            {
                CheckKeyComboSteps();
            }

            if (releasedKeys.Count == 0)
                return;

            List<KeyCode> doublePressedKeys = new List<KeyCode>();

            foreach (var key in releasedKeys)
            {
                float currentTime = Time.time;

                if (lastKeyPressTimes.TryGetValue(key, out float lastPressTime))
                {
                    if (currentTime - lastPressTime <= keyDoublePressTimeWindow)
                    {
                        Debug.Log($"[ATM] Detected double press for key: {key}");
                        doublePressedKeys.Add(key);

                        lastKeyPressTimes.Remove(key);
                        possibleDoublePressKeys.Remove(key);
                    }
                    else
                    {
                        lastKeyPressTimes[key] = currentTime;
                        possibleDoublePressKeys[key] = true;
                    }
                }
                else
                {
                    lastKeyPressTimes[key] = currentTime;
                    possibleDoublePressKeys[key] = true;
                }

                if (keyPressStartTimes.ContainsKey(key))
                {
                    keyPressStartTimes.Remove(key);
                    Debug.Log($"[ATM] Key {key} held for {keyPressDurations[key]} seconds");
                }
            }

            cachedActiveIndices.Clear();
            cachedActiveIndices.AddRange(activeStepIndices);

            foreach (int stepIndex in cachedActiveIndices)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState != StateEnum.Displaying || step.interaction == InteractionTypeEnum.ManuallyCall || step.blockedByButton || step.blockedByTime)
                    continue;

                if (step.interaction == InteractionTypeEnum.KeyCodeDoublePress && doublePressedKeys.Count > 0)
                {
                    foreach (var doublePressedKey in doublePressedKeys)
                    {
                        if (step.keyCodes.Contains(doublePressedKey))
                        {
                            Debug.Log($"[ATM] Step {stepIndex} - Double press for key {doublePressedKey} detected. Completing step.");
                            CompleteStep(stepIndex);
                            break;
                        }
                    }
                }
                else if (step.interaction == InteractionTypeEnum.KeyCode)
                {
                    foreach (var pressedKey in releasedKeys)
                    {
                        if (keyPressDurations.TryGetValue(pressedKey, out float duration) && duration < sceneReferences.inputController.minHoldDuration)
                        {
                            if (ProcessKeyPressForStep(stepIndex, step.keyCodes, pressedKey))
                            {
                                Debug.Log($"[ATM] Step {stepIndex} - All required keys have been pressed. Completing step.");
                                CompleteStep(stepIndex);
                                pressedKeysProgress.Remove(stepIndex);
                                break;
                            }
                        }
                    }
                }
                else if (step.interaction == InteractionTypeEnum.KeyCodeHold)
                {
                    foreach (var pressedKey in releasedKeys)
                    {
                        if (heldKeys.Contains(pressedKey))
                        {
                            if (keyPressDurations.TryGetValue(pressedKey, out float duration) && duration > sceneReferences.inputController.minHoldDuration)
                            {
                                if (ProcessKeyPressForStep(stepIndex, step.keyCodes, pressedKey))
                                {
                                    Debug.Log($"[ATM] Step {stepIndex} - All required keys have been held. Completing step.");
                                    CompleteStep(stepIndex);
                                    pressedKeysProgress.Remove(stepIndex);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            foreach (var key in releasedKeys)
            {
                if (heldKeys.Contains(key))
                {
                    heldKeys.Remove(key);
                }
            }
        }

        // Processes key press for specific tutorial step
        private bool ProcessKeyPressForStep(int stepIndex, List<KeyCode> requiredKeys, KeyCode pressedKey)
        {
            if (requiredKeys.Count == 1)
            {
                return pressedKey == requiredKeys[0];
            }

            if (!pressedKeysProgress.ContainsKey(stepIndex))
            {
                pressedKeysProgress[stepIndex] = new HashSet<KeyCode>();
            }

            if (requiredKeys.Contains(pressedKey) && !pressedKeysProgress[stepIndex].Contains(pressedKey))
            {
                pressedKeysProgress[stepIndex].Add(pressedKey);
                var progress = pressedKeysProgress[stepIndex].Count;
                var total = requiredKeys.Count;
                Debug.Log($"[ATM] Key {pressedKey} registered for step {stepIndex}. Progress: {progress}/{total} keys");
            }

            return pressedKeysProgress[stepIndex].Count == requiredKeys.Count;
        }


        // Checks and processes scroll interactions
        private void CheckScrollInteractions()
        {
            bool isScrollingUp = sceneReferences.inputController.IsScrollingUp();
            bool isScrollingDown = sceneReferences.inputController.IsScrollingDown();

            if (!isScrollingUp && !isScrollingDown) return;

            cachedActiveIndices.Clear();
            cachedActiveIndices.AddRange(activeStepIndices);

            foreach (int stepIndex in cachedActiveIndices)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState != StateEnum.Displaying || step.interaction == InteractionTypeEnum.ManuallyCall || step.blockedByButton || step.blockedByTime) continue;

                bool interactionPassed = false;

                if (step.interaction == InteractionTypeEnum.ScrollUp && isScrollingUp)
                {
                    interactionPassed = true;
                    Debug.Log($"[ATM] Step {stepIndex} - ScrollUp detected");
                }
                else if (step.interaction == InteractionTypeEnum.ScrollDown && isScrollingDown)
                {
                    interactionPassed = true;
                    Debug.Log($"[ATM] Step {stepIndex} - ScrollDown detected");
                }

                if (interactionPassed && CheckInteractionTarget(step))
                {
                    Debug.Log($"[ATM] Step {stepIndex} - Completing scroll step...");
                    CompleteStep(stepIndex);
                }
            }
        }


        // Processes input down event for current step
        private void HandleStepInputDown()
        {
            if (sceneReferences.inputController.GetRightClickDown())
            {
                isDoubleClickPossible = false;
                lastClickedObject = null;
                lastClickTime = Time.time;
                lastRightClickTime = Time.time;
            }
            else if (sceneReferences.inputController.GetMiddleClickDown())
            {
                isDoubleClickPossible = false;
                lastClickedObject = null;
                isRightDoubleClickPossible = false;
                lastRightClickedObject = null;
                lastMiddleClickTime = Time.time;
            }
            else if (sceneReferences.inputController.GetInputDown())
            {
                isRightDoubleClickPossible = false;
                lastRightClickedObject = null;
            }

            clickStartTime = Time.time;
            clickStartPosition = sceneReferences.inputController.GetInputPosition();
            ObjectTypeEnum detectedType;
            startClickObject = sceneReferences.inputController.GetClickedObject(out detectedType);
            isHolding = true;
            if (startClickObject)
            {
                clickedObjectStartPosition = startClickObject.transform.position;
            }
        }

        // Validates interaction type against expected behavior
        private bool CheckInteractionType(InteractionTypeEnum expectedInteraction, float inputDuration, GameObject startObj, GameObject endObj)
        {
            switch (expectedInteraction)
            {
                case InteractionTypeEnum.SwipeLeft:
                case InteractionTypeEnum.SwipeRight:
                case InteractionTypeEnum.SwipeUp:
                case InteractionTypeEnum.SwipeDown:
                    Vector2 endPosition = sceneReferences.inputController.GetInputPosition();

                    var detectedSwipe = sceneReferences.inputController.DetectSwipe(
                        clickStartPosition,
                        endPosition,
                        inputDuration
                    );

                    bool isCorrectSwipe = detectedSwipe.HasValue && detectedSwipe.Value == expectedInteraction;

                    if (isCorrectSwipe)
                    {
                        Debug.Log($"[ATM] Swipe detected: {detectedSwipe.Value}");
                    }

                    return isCorrectSwipe;

                case InteractionTypeEnum.Click:
                    bool notHoldGesture = !sceneReferences.inputController.IsHoldGesture(inputDuration);
                    endClickObject = startObj;
                    endObj = endClickObject;
                    bool sameObject = startObj == endObj;

                    isDoubleClickPossible = false;
                    lastClickedObject = startObj;
                    lastClickTime = Time.time;

                    return notHoldGesture && sameObject;

                case InteractionTypeEnum.RightClick:
                    if (sceneReferences.inputController.IsMobileDevice)
                    {
                        Debug.Log("[ATM] RightClick interaction attempted on mobile device");
                        return false;
                    }

                    notHoldGesture = !sceneReferences.inputController.IsHoldGesture(inputDuration);
                    sameObject = startObj == endObj;
                    bool isRightClick = sceneReferences.inputController.GetRightClickUp();

                    Debug.Log($"[ATM] RightClick check: notHoldGesture={notHoldGesture}, sameObject={sameObject}, isRightClick={isRightClick}");
                    return notHoldGesture && sameObject && isRightClick;

                case InteractionTypeEnum.DoubleClick:
                    if (sceneReferences.inputController.GetRightClickUp())
                    {
                        Debug.Log("[ATM] DoubleClick failed: right click detected");
                        return false;
                    }

                    float timeSinceLastClick = Time.time - lastClickTime;

                    bool isValidDoubleClick = isDoubleClickPossible &&
                                            timeSinceLastClick <= sceneReferences.tutorialMaker.doubleClickTimeWindow &&
                                            lastClickedObject == startObj;

                    isDoubleClickPossible = false;
                    lastClickedObject = null;

                    if (!isValidDoubleClick)
                    {
                        isDoubleClickPossible = true;
                        lastClickedObject = startObj;
                        lastClickTime = Time.time;
                        return false;
                    }

                    if (sceneReferences.inputController.IsHoldGesture(inputDuration))
                    {
                        Debug.Log("[ATM] DoubleClick failed: hold gesture detected");
                        return false;
                    }

                    sameObject = startObj == endObj;
                    Debug.Log($"[ATM] DoubleClick check completed: sameObject={sameObject}");
                    return sameObject;

                case InteractionTypeEnum.RightDoubleClick:
                    if (!sceneReferences.inputController.GetRightClickUp())
                    {
                        Debug.Log("[ATM] RightDoubleClick failed: not a right click");
                        return false;
                    }

                    float timeSinceLastRightClick = Time.time - lastRightClickTime;

                    bool isValidRightDoubleClick = isRightDoubleClickPossible &&
                                                 lastRightClickedObject == startObj &&
                                                 timeSinceLastRightClick <= sceneReferences.tutorialMaker.doubleClickTimeWindow;

                    Debug.Log($"[ATM] RightDoubleClick validation: possible={isRightDoubleClickPossible}, " +
                             $"sameObject={lastRightClickedObject == startObj}, " +
                             $"timeWindow={timeSinceLastRightClick <= sceneReferences.tutorialMaker.doubleClickTimeWindow}");

                    isRightDoubleClickPossible = false;
                    lastRightClickedObject = null;

                    if (!isValidRightDoubleClick)
                    {
                        isRightDoubleClickPossible = true;
                        lastRightClickedObject = startObj;
                        lastRightClickTime = Time.time;
                        Debug.Log("[ATM] First right click of potential double click");
                        return false;
                    }

                    if (sceneReferences.inputController.IsHoldGesture(inputDuration))
                    {
                        Debug.Log("[ATM] RightDoubleClick failed: hold gesture detected");
                        return false;
                    }

                    sameObject = startObj == endObj;
                    Debug.Log($"[ATM] RightDoubleClick completed: sameObject={sameObject}");
                    return sameObject;

                case InteractionTypeEnum.Hold:
                    return sceneReferences.inputController.IsHoldGesture(inputDuration) && (startObj == endObj);

                case InteractionTypeEnum.RightHold:
                    isRightClick = sceneReferences.inputController.GetRightClickUp();
                    return isRightClick && sceneReferences.inputController.IsHoldGesture(inputDuration) && startObj == endObj;

                case InteractionTypeEnum.DragAndDrop:
                    return sceneReferences.inputController.IsDragGesture(startObj, endObj, inputDuration);

                case InteractionTypeEnum.MiddleClick:
                    if (sceneReferences.inputController.IsMobileDevice)
                    {
                        Debug.Log("[ATM] MiddleClick interaction attempted on mobile device");
                        return false;
                    }

                    notHoldGesture = !sceneReferences.inputController.IsHoldGesture(inputDuration);
                    sameObject = startObj == endObj;
                    bool isMiddleClick = sceneReferences.inputController.GetMiddleClickUp();

                    Debug.Log($"[ATM] MiddleClick check: notHoldGesture={notHoldGesture}, sameObject={sameObject}, isMiddleClick={isMiddleClick}");
                    return notHoldGesture && sameObject && isMiddleClick;

                case InteractionTypeEnum.MiddleHold:
                    isMiddleClick = sceneReferences.inputController.GetMiddleClickUp();
                    return isMiddleClick && sceneReferences.inputController.IsHoldGesture(inputDuration) && startObj == endObj;
                case InteractionTypeEnum.PinchIn:
                case InteractionTypeEnum.PinchOut:
                case InteractionTypeEnum.PinchRotate:
                        return isPinchDetected && currentPinchType == expectedInteraction;
                 
                default:
                    return false;
            }
        }

        // Validates interaction target based on check type
        private bool CheckInteractionTarget(ClickData step)
        {
            if (step.interaction == InteractionTypeEnum.DragAndDrop &&
                step.checkInteraction == InteractionTargetEnum.AnyTarget)
            {
                Debug.LogWarning("[ATM] DragAndDrop interaction cannot be used with AnyTarget");
                return false;
            }

            if (step.checkInteraction == InteractionTargetEnum.AnyTarget)
            {
                return true;
            }

            switch (step.checkInteraction)
            {
                case InteractionTargetEnum.ByGameObject:
                    Debug.Log($"[ATM] Checking GameObjects: startClickObject={startClickObject?.name}");
                    return CheckGameObjects(step.GameObjects);

                case InteractionTargetEnum.ByTag:
                    Debug.Log($"[ATM] Checking Tags: startClickObject={startClickObject?.tag}");
                    return CheckTags(step.tags);

                case InteractionTargetEnum.ByLayer:
                    Debug.Log($"[ATM] Checking Layers: startClickObject={startClickObject?.layer}");
                    return CheckLayers(step.layers);

                default:
                    return false;
            }
        }

        private void CheckDrag()
        {
            if (endClickObject == startClickObject)
            {
                clickedObjectEndPosition = endClickObject.transform.position;
            }
        }

        // Validates GameObject interactions
        private bool CheckGameObjects(List<UnityEngine.Object> targetObjects)
        {
            if (targetObjects == null || targetObjects.Count == 0 || startClickObject == null)
                return false;

            if (startClickObject != (GameObject)targetObjects[0])
                return false;

            if (targetObjects.Count > 1 && endClickObject != null)
            {
                return endClickObject == (GameObject)targetObjects[1];
            }

            CheckDrag();
            return true;
        }

        // Validates tag-based interactions
        private bool CheckTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0 || startClickObject == null)
            {
                Debug.Log($"[ATM] CheckTags failed: tags null/empty={tags == null || tags.Count == 0}, " +
                          $"startObj null={startClickObject == null}");
                return false;
            }

            if (endClickObject != null && tags.Count > 1)
            {
                bool startTagMatch = startClickObject.tag == tags[0];
                bool endTagMatch = endClickObject.tag == tags[1];

                Debug.Log($"[ATM] CheckTags (DragAndDrop): startTag={startClickObject.tag}, expectedStartTag={tags[0]}, " +
                          $"endTag={endClickObject.tag}, expectedEndTag={tags[1]}");

                return startTagMatch && endTagMatch;
            }

            bool tagMatch = startClickObject.tag == tags[0];
            Debug.Log($"[ATM] CheckTags (Click): objectTag={startClickObject.tag}, expectedTag={tags[0]}");

            CheckDrag();
            return tagMatch;
        }

        // Validates layer-based interactions
        private bool CheckLayers(List<int> layers)
        {
            if (layers == null || layers.Count == 0 || startClickObject == null)
            {
                Debug.Log($"[ATM] CheckLayers failed: layers null/empty={layers == null || layers.Count == 0}, " +
                          $"startObj null={startClickObject == null}");
                return false;
            }

            if (endClickObject != null && layers.Count > 1)
            {
                bool startLayerMatch = startClickObject.layer == layers[0];
                bool endLayerMatch = endClickObject.layer == layers[1];

                Debug.Log($"[ATM] CheckLayers (DragAndDrop): startLayer={LayerMask.LayerToName(startClickObject.layer)}, expectedStartLayer={LayerMask.LayerToName(layers[0])}, " +
                          $"endLayer={LayerMask.LayerToName(endClickObject.layer)}, expectedEndLayer={LayerMask.LayerToName(layers[1])}");

                return startLayerMatch && endLayerMatch;
            }

            bool layerMatch = startClickObject.layer == layers[0];
            Debug.Log($"[ATM] CheckLayers (Click): objectLayer={LayerMask.LayerToName(startClickObject.layer)}, expectedLayer={LayerMask.LayerToName(layers[0])}");

            CheckDrag();
            return layerMatch;
        }

        #endregion

        #region Helpers
        private string GetSceneSpecificPath()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneReferences.currentDeviceATM))
            {
                return Path.Combine(Application.persistentDataPath, "TutorialProgress",
                                   $"tutorial_progress_{sceneName}_{sceneReferences.currentDeviceATM}.json");
            }
            else
            {
                return Path.Combine(Application.persistentDataPath, "TutorialProgress",
                                   $"tutorial_progress_{sceneName}.json");
            }
        }

        // Gets target information based on interaction type
        private string GetTargetInfo(ClickData step)
        {
            switch (step.checkInteraction)
            {
                case InteractionTargetEnum.ByGameObject:
                    return string.Join(", ", step.GameObjects.ConvertAll(obj => obj != null ? obj.name : "null"));

                case InteractionTargetEnum.ByTag:
                    return string.Join(", ", step.tags);

                case InteractionTargetEnum.ByLayer:
                    return string.Join(", ", step.layers.ConvertAll(layer => LayerMask.LayerToName(layer)));

                default:
                    return "?? ???????";
            }
        }

        #endregion

        #region ManualCalls
        // Starts specific tutorial step by index manually
        public void StartSpecificStep(int stepIndex, bool stopOtherSteps)
        {
            if (!ValidateStepIndex(stepIndex)) return;
            Debug.Log("[ATM] StartSpecificStep");
            var stepToStart = sceneReferences.tutorialMaker.stepSequence[stepIndex];
            if (stepToStart.stepState == StateEnum.Displaying)
            {
                Debug.LogWarning($"[ATM] Step {stepIndex} is already running!");
                return;
            }
            if (stopOtherSteps)
            {
                ResetDisplayingSteps();
            }
            if (stepToStart.executeInParallel)
            {
                StartParallelGroup(stepToStart.parallelGroupId);
            }
            else
            {
                InitializeStep(stepIndex);
            }
        }

        // Starts specific tutorial step by index
        private void StartStep(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < sceneReferences.tutorialMaker.stepSequence.Count)
            {
                Debug.Log($"[ATM] StartStep {stepIndex}");
                var stepToStart = sceneReferences.tutorialMaker.stepSequence[stepIndex];

                if (stepToStart.stepState == StateEnum.Displaying)
                {
                    Debug.LogWarning($"[ATM] Step {stepIndex} is already running!");
                    return;
                }

                if (stepToStart.startDelay > 0)
                {
                    if (!pendingSteps.ContainsKey(stepIndex))
                    {
                        stepToStart.blockedByTimeDelay = stepToStart.startDelay.ToString("F2");
                        pendingSteps.Add(stepIndex, stepToStart.startDelay);
                        Debug.Log($"[ATM] Step {stepIndex} queued with {stepToStart.startDelay} delay");
                    }
                    return;
                }

                if (stepToStart.executeInParallel)
                {
                    StartParallelGroup(stepToStart.parallelGroupId);
                }
                else
                {
                    InitializeStep(stepIndex, true);
                    if (!activeStepIndices.Contains(stepIndex))
                    {
                        activeStepIndices.Add(stepIndex);
                    }
                    ClearKeyboardInput();
                }
            }
        }

        // Starts specific tutorial step with delay
        private void StartStepAfterDelay(int stepIndex)
        {
            if (!ValidateStepIndex(stepIndex)) return;

            var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
            Debug.Log($"[ATM] Starting delayed step {stepIndex}");

            if (step.stepState == StateEnum.Displaying)
            {
                Debug.LogWarning($"[ATM] Step {stepIndex} is already running!");
                return;
            }

            if (step.executeInParallel)
            {
                StartParallelGroup(step.parallelGroupId);
            }
            else
            {
                InitializeStep(stepIndex, true);
                if (!activeStepIndices.Contains(stepIndex))
                {
                    activeStepIndices.Add(stepIndex);
                }
                ClearKeyboardInput();
            }
            step.blockedByTimeDelay = null;
        }

        // Starts a tutorial step with custom target GameObjects
        public void StartTutorialStepWithTargets(int stepIndex, List<GameObject> targetObjects, bool stopOtherSteps)
        {
            if (sceneReferences.tutorialMaker == null || sceneReferences.tutorialMaker.stepSequence == null ||
                stepIndex < 0 || stepIndex >= sceneReferences.tutorialMaker.stepSequence.Count)
            {
                Debug.LogError($"[ATM] Invalid step index: {stepIndex}");
                return;
            }

            sceneReferences.tutorialMaker.stepSequence[stepIndex].GameObjects = new List<UnityEngine.Object>(targetObjects);

            StartSpecificStep(stepIndex, stopOtherSteps);
        }

        // Starts parallel group of tutorial steps
        private void StartParallelGroup(int groupId)
        {
            for (int i = 0; i < sceneReferences.tutorialMaker.stepSequence.Count; i++)
            {
                var step = sceneReferences.tutorialMaker.stepSequence[i];
                if (step.executeInParallel &&
                    step.parallelGroupId == groupId &&
                    step.stepState == StateEnum.NotDoneYet &&
                    !activeStepIndices.Contains(i))
                {
                    InitializeStep(i);
                    activeStepIndices.Add(i);
                    ClearKeyboardInput();
                }
            }
        }

        // New ForceCompleteStep just validates the step index before calling CompleteStepInternal
        public void ForceCompleteStep(int stepIndex)
        {
            if (!ValidateStepIndex(stepIndex))
            {
                Debug.LogWarning($"[ATM] Cannot force complete step: Invalid step index {stepIndex}");
                return;
            }

            var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];

            if (step.stepState == StateEnum.Done)
            {
                Debug.LogWarning($"[ATM] Step {stepIndex} is already completed");
                return;
            }

            if (step.stepState == StateEnum.NotDoneYet)
            {
                step.stepState = StateEnum.Displaying;
                if (!activeStepIndices.Contains(stepIndex))
                {
                    activeStepIndices.Add(stepIndex);
                    ClearKeyboardInput();
                }
            }

            CompleteStepInternal(stepIndex);
        }

        // Resets all tutorial progress
        public void ResetProgress()
        {
            if (sceneReferences && sceneReferences.tutorialMaker?.stepSequence != null)
            {
                activeStepIndices.Clear();
                foreach (var step in sceneReferences.tutorialMaker.stepSequence)
                {
                    if (step.stepState != StateEnum.NotDoneYet)
                    {
                        step.stepState = StateEnum.NotDoneYet;
                        sceneReferences.visualManager.DisableVisualsForStep(step);
                    }
                }
                pendingSteps.Clear();
                ClearKeyboardInput();
                currentStepIndex = -1;
                sceneReferences.visualManager.DestroyAllVisuals();

                string currentSceneProgressPath = GetSceneSpecificPath();
                if (File.Exists(currentSceneProgressPath))
                {
                    File.Delete(currentSceneProgressPath);
                }

                waitingForManualStart = false;

                if (Application.isPlaying && CanStartTutorial())
                {
                    FindFirstUncompletedStep();
                }
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                Debug.Log($"[ATM] Resetting progress. Path to progress file: {currentSceneProgressPath}");
            }
        }

        // Update the existing ResetDisplayingSteps method
        public void ResetDisplayingSteps()
        {
            Debug.Log("[ATM] ResetDisplayingSteps");
            if (sceneReferences.tutorialMaker != null && sceneReferences.tutorialMaker.stepSequence != null)
            {
                foreach (var step in sceneReferences.tutorialMaker.stepSequence)
                {
                    if (step.stepState == StateEnum.Displaying)
                    {
                        step.stepState = StateEnum.NotDoneYet;
                        sceneReferences.visualManager.DisableVisualsForStep(step);
                    }
                }
                pendingSteps.Clear();
                activeStepIndices.Clear();
                ClearKeyboardInput();
                SaveProgress();
            }
        }

        // Completes all tutorial steps without exception
        public void CompleteTutorial()
        {
            if (sceneReferences.tutorialMaker == null ||
                sceneReferences.tutorialMaker.stepSequence == null)
            {
                Debug.LogError("[ATM] Cannot complete tutorial: TutorialMaker or step sequence is null");
                return;
            }

            foreach (var step in sceneReferences.tutorialMaker.stepSequence)
            {
                if (step.stepState != StateEnum.Done)
                {
                    step.stepState = StateEnum.Done;

                    step.blockedByButton = false;
                    step.blockedByTime = false;
                    step.fakeblockedByButton = false;
                }
            }

            activeStepIndices.Clear();
            ClearKeyboardInput();

            sceneReferences.visualManager.DisableAllVisuals();
            sceneReferences.visualManager.DestroyAllVisuals();

            SaveProgress();

            Debug.Log("[ATM] Tutorial completed successfully");
        }

        private void ClearKeyboardInput()
        {
            UpdateKeyboardInteractionCache();
            pressedKeysProgress.Clear();
            keyPressStartTimes.Clear();
            keyPressDurations.Clear();
            heldKeys.Clear();
            heldJoystickButtons.Clear();
            joystickButtonHoldDurations.Clear();
            currentlyHeldKeys.Clear();
            lastKeyPressTimes.Clear();
            possibleDoublePressKeys.Clear();
            isPinchDetected = false;
            currentPinchType = InteractionTypeEnum.ManuallyCall;
        }

        // Checks any active keyboard steps
        private void UpdateKeyboardInteractionCache()
        {
            hasKeyboardInteractionSteps = false;
            foreach (int stepIndex in activeStepIndices)
            {
                if (!ValidateStepIndex(stepIndex)) continue;

                var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
                if (step.stepState == StateEnum.Displaying &&
                   (step.interaction == InteractionTypeEnum.KeyCode ||
                    step.interaction == InteractionTypeEnum.KeyCodeHold ||
                    step.interaction == InteractionTypeEnum.KeyCodeCombo ||
                    step.interaction == InteractionTypeEnum.KeyCodeDoublePress))
                {
                    hasKeyboardInteractionSteps = true;
                    break;
                }
            }
        }

        #endregion

        #region Translation
        public void TranslateSpecificStep(int stepIndex, string textValue, TextToChange textField)
        {
            ClickData step = sceneReferences.tutorialMaker.stepSequence[stepIndex];
            if (step.localizationReference != null)
            {
                switch (textField)
                {
                    case TextToChange.PointerText:
                        step.localizationReference.PointerText = textValue;
                        break;
                    case TextToChange.GraphicText:
                        step.localizationReference.GraphicText = textValue;
                        break;
                    case TextToChange.WorldPointerText:
                        step.localizationReference.WorldPointerText = textValue;
                        break;
                    case TextToChange.WorldGraphicText:
                        step.localizationReference.WorldGraphicText = textValue;
                        break;
                }
                sceneReferences.visualManager.UpdateTranslation(step);
            }
        }
        #endregion

        #region Logs

        // Logs information about current step
        private void LogStepInfo(ClickData step)
        {
            string targetInfo = GetTargetInfo(step);
            Debug.Log($"[ATM] Go to step number {currentStepIndex}. Expected {step.interaction} recognition {step.checkInteraction}. Required objects/layers/tags: {targetInfo}");
        }
        #endregion

        #region Completion
        // This will be the base method that contains all completion logic
        private void CompleteStepInternal(int stepIndex)
        {
            var stepToComplete = sceneReferences.tutorialMaker.stepSequence[stepIndex];
            Debug.Log($"[ATM] Completing step {stepIndex}");

            stepToComplete.blockedByButton = false;
            stepToComplete.blockedByTime = false;
            stepToComplete.fakeblockedByButton = false;

            stepToComplete.stepState = StateEnum.Done;
            activeStepIndices.Remove(stepIndex);
            ClearKeyboardInput();

            sceneReferences.visualManager.DisableVisualsForStep(stepToComplete);

            if (stepToComplete.onStepComplete != null)
            {
                stepToComplete.onStepComplete.Invoke();
            }

            if (stepToComplete.executeInParallel)
            {
                bool allGroupCompleted = true;
                foreach (var step in sceneReferences.tutorialMaker.stepSequence)
                {
                    if (step.executeInParallel &&
                        step.parallelGroupId == stepToComplete.parallelGroupId &&
                        step.stepState != StateEnum.Done)
                    {
                        allGroupCompleted = false;
                        break;
                    }
                }

                if (allGroupCompleted)
                {
                    Debug.Log($"[ATM] Parallel group {stepToComplete.parallelGroupId} completed");
                    OnGroupCompleted();
                }
            }
            else if (activeStepIndices.Count == 0)
            {
                FindFirstUncompletedStep();
            }

            SaveProgress();
        }

        // Original CompleteStep now just validates conditions before calling CompleteStepInternal
        private void CompleteStep(int stepIndex)
        {
            var step = sceneReferences.tutorialMaker.stepSequence[stepIndex];

            if (step.stepState != StateEnum.Displaying)
            {
                Debug.LogWarning($"[ATM] Cannot complete step {stepIndex}: Step is not in Displaying state");
                return;
            }

            CompleteStepInternal(stepIndex);
        }

        // Handles completion of parallel step group
        private void OnGroupCompleted()
        {
            Debug.Log("[ATM] Group completed, searching for next step");
            FindFirstUncompletedStep();
        }

        #endregion

        #region Progress
        // Saves current tutorial progress
        private void SaveProgress()
        {
            try
            {
                var progressData = new Dictionary<int, StateEnum>();
                for (int i = 0; i < sceneReferences.tutorialMaker.stepSequence.Count; i++)
                {
                    progressData[i] = sceneReferences.tutorialMaker.stepSequence[i].stepState;
                    if (progressData[i] == StateEnum.Displaying)
                    {
                        progressData[i] = StateEnum.NotDoneYet;
                    }
                }

                string json = JsonUtility.ToJson(new SerializableDict<int, StateEnum>(progressData));
                File.WriteAllText(progressFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ATM] Failed to save tutorial progress: {e.Message}");
            }
        }

        // Loads saved tutorial progress
        private void LoadProgress()
        {
            try
            {
                string newPath = GetSceneSpecificPath();

                if (File.Exists(newPath))
                {
                    // Загружаем из нового файла
                    string json = File.ReadAllText(newPath);
                    var progressData = JsonUtility.FromJson<SerializableDict<int, StateEnum>>(json);

                    foreach (var pair in progressData.ToDictionary())
                    {
                        if (pair.Key < sceneReferences.tutorialMaker.stepSequence.Count)
                        {
                            sceneReferences.tutorialMaker.stepSequence[pair.Key].stepState = pair.Value;
                        }
                    }
                }
                else
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    string oldPath = Path.Combine(Application.persistentDataPath, "TutorialProgress",
                                                $"tutorial_progress_{sceneName}.json");

                    if (File.Exists(oldPath) && !string.IsNullOrEmpty(sceneReferences.currentDeviceATM))
                    {
                        string json = File.ReadAllText(oldPath);
                        var progressData = JsonUtility.FromJson<SerializableDict<int, StateEnum>>(json);

                        foreach (var pair in progressData.ToDictionary())
                        {
                            if (pair.Key < sceneReferences.tutorialMaker.stepSequence.Count)
                            {
                                sceneReferences.tutorialMaker.stepSequence[pair.Key].stepState = pair.Value;
                            }
                        }

                        SaveProgress();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ATM] Failed to load tutorial progress: {e.Message}");
            }
        }

        #endregion
    }

    #region Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(StepSequencePlayer))]
    public class StepSequencePlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            StepSequencePlayer player = (StepSequencePlayer)target;
            EditorGUILayout.Space();

            Color originalColor = GUI.backgroundColor;

            GUI.enabled = Application.isPlaying;
            GUI.backgroundColor = Application.isPlaying ? Color.gray : Color.gray * 0.7f;

            if (GUILayout.Button("Reset Tutor Progress", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Reset Tutorial Progress",
                    "Are you sure you want to reset all tutorial progress? This action cannot be undone.",
                    "Yes, Reset Progress", "Cancel"))
                {
                    player.ResetProgress();
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = originalColor;
        }
    }
#endif

    [Serializable]
    public class SerializableDict<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<TValue> values = new List<TValue>();

        public SerializableDict() { }

        // Creates dictionary from key-value pairs
        public SerializableDict(Dictionary<TKey, TValue> dict)
        {
            foreach (KeyValuePair<TKey, TValue> pair in dict)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // Converts lists back to dictionary
        public Dictionary<TKey, TValue> ToDictionary()
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict.Add(keys[i], values[i]);
            }
            return dict;
        }
    }
    #endregion
}