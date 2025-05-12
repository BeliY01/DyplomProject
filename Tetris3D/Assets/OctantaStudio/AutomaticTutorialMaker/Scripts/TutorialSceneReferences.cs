using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace AutomaticTutorialMaker
{
    public class TutorialSceneReferences : MonoBehaviour
    {
        #region Variables

        [Header("Required Scene References")]
        public Camera mainCamera; // Main camera for rendering and raycasting
        public Canvas targetCanvas; // Canvas for UI elements
        public Transform targetWorldHolder; // For world elements spawn

        [Header("Localization and Rebinding")]
        public string currentDeviceATM = "UniversalDevice_ATM"; // In case there are 2+ ATM in one scene
        public InputStringsScriptableObject inputTextSettings; // Text strings
        [SerializeField] private bool autostart = true;

        [Header("UI Pointer Templates")]
        [Grayed] public PointerSettings NonePointerSettings; // Default empty pointer settings
        public PointerSettings defaultPointerSettings; // Default pointer configuration
        public GameObject defaultUiPointer; // Default UI pointer prefab
        public GameObject defaultUiPointerMouse; // Mouse pointer prefab
        public GameObject defaultUiPointerIris; // Iris pointer prefab

        [Header("UI Graphic Templates")]
        [Grayed] public GraphicSettings NoneGraphicSettings; // Default empty graphic settings
        public GraphicSettings defaultGraphicSettings; // Default graphic configuration
        public GameObject defaultUiGraphic; // Default UI graphic prefab
        public GameObject defaultUiGraphicText; // Text graphic prefab
        public GameObject defaultUiGraphicPopup; // Popup graphic prefab
        public GameObject defaultUiGraphicPopupButton; // Popup button prefab
        public GameObject defaultUiGraphicCoachmark; // Coachmark graphic prefab

        public GameObject defaultUiGraphicSlideBar; // Slide bar graphic prefab
        public GameObject defaultUiGraphicSwipeCircle; // Swipe circle graphic prefab
        public GameObject defaultUiGraphicTouchCircle; // Touch circle graphic prefab
        public GameObject defaultUiGraphicHoldCircle; // Hold circle graphic prefab
        public GameObject defaultJoystickUiGraphic; // Joystick graphic prefab

        [Header("UI Hover Templates")]
        public GameObject defaultUiHover; // Default hover effect prefab
        public GameObject defaultUiIrisHover; // Default iris hover effect prefab

        [Header("Additional UI Templates")]
        public GameObject defaultQuestionMark; // Question mark indicator prefab
        public GameObject backroundPrefab; // Background overlay prefab

        [Header("World Pointer Templates")]
        [Grayed] public WorldPointerSettings NoneWorldPointerSettings; // Default empty pointer settings
        public GameObject textStatic;
        public GameObject arrowDynamic;
        public GameObject arrowNavigation;
        public GameObject geoTag;
        public GameObject pointerSide;
        public GameObject roadSignStatic;

        public GameObject frameBasic;
        public GameObject geoTag2d;
        public GameObject pointerBelow;
        public GameObject pointerSide2d;
        public GameObject pointerAim;

        [Header("World Graphic Templates")]
        [Grayed] public WorldGraphicSettings NoneWorldGraphicSettings;

        [Header("Required Components")]
        [ReadOnly] public AutomaticTutorialMaker tutorialMaker; // Core tutorial manager
        [ReadOnly] public InputController inputController; // Input detection handler
        [ReadOnly] public StepSequencePlayer sequencePlayer; // Tutorial sequence manager
        [ReadOnly] public TutorialVisualManager visualManager; // Visual elements manager
        public enum TextToChange
        {
          PointerText,
            GraphicText,
            WorldPointerText,
            WorldGraphicText
        }


        #endregion

        #region Example Methods
        // // Select and press Ctrl + K + U to uncomment and test as it is
        //void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //        // Testing method with space press
        //        ChangeStepVisualText(0, "Bup", TextToChange.PointerText);
        //    }
        //    if (Input.GetKeyDown(KeyCode.Tab))
        //    {
        //        // Testing method with tab press
        //        TranslateAllTutorial(InputStringsScriptableObject.Language.Italian);
        //    }
        //}
        #endregion

        // Wrapper methods to call manually

        #region MethodsForManualCall

        // Methods for switching between ATM on scene. Turn off one and turn on the other
        public void TurnOffTutorial()
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot turn off tutorial: StepSequencePlayer is null!");
                return;
            }
            sequencePlayer.ResetDisplayingSteps();

            sequencePlayer.enabled = false;

            Debug.Log("[ATM] Tutorial disabled: " + currentDeviceATM);
        }

        public void TurnOnTutorial()
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot turn on tutorial: StepSequencePlayer is null!");
                return;
            }
            
                sequencePlayer.enabled = true;
                sequencePlayer.Initialize();
                Debug.Log("[ATM] Tutorial enabled: " + currentDeviceATM);
            
        }


        // Translate by choosing existing language
        public void TranslateAllTutorial(InputStringsScriptableObject.Language language)
        {
            if (inputTextSettings)
            {
                inputTextSettings.ChangeLanguage(language);
            }
        }

        // Translate by entering language (column header)
        public void TranslateAllTutorialByString(string language)
        {
            if (inputTextSettings)
            {
                inputTextSettings.ChangeLanguage(language);
            }
        }

        // Starts a specific tutorial step by its index and disables others
        public void ChangeStepVisualText(int stepIndex, string textValue, TextToChange textField)
        {
            sequencePlayer.TranslateSpecificStep(stepIndex, textValue, textField);
        }

        public void StartTutorialStep(int stepIndex)
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot start step: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.StartSpecificStep(stepIndex, true);
        }

        // Starts a specific tutorial step by its index and doesnt disable others
        public void AsyncStartTutorialStep(int stepIndex)
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot start step: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.StartSpecificStep(stepIndex, false);
        }

        // Starts a tutorial step with custom target GameObjects
        public void StartTutorialStepWithTargets(int stepIndex, List<GameObject> targetObjects, bool stopOtherSteps)
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot start step: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.StartTutorialStepWithTargets(stepIndex, targetObjects, stopOtherSteps);
        }

        // Forces completion of specified tutorial step
        public void ForceCompleteStep(int stepIndex)
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot force complete step: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.ForceCompleteStep(stepIndex);
        }

        // Forces completion of all tutorial
        public void ForceCompleteTutorial()
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot complete tutorial: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.CompleteTutorial();
        }

        // Resets all currently displaying tutorial steps
        public void ResetDisplayingTutorialSteps()
        {
            if (sequencePlayer == null)
            {
                Debug.LogError("[ATM] Cannot reset steps: StepSequencePlayer is null!");
                return;
            }

            sequencePlayer.ResetDisplayingSteps();
        }
        #endregion

        #region Initialization

        // Initializes all tutorial components and checks for required references
        private void Start()
        {
            //  StartCoroutine(DelayedStart());
            //}
            //private IEnumerator DelayedStart()
            //{
            //    yield return new WaitForSeconds(0.5f);

            ValidateComponents();
            InitializeComponents();

            if(!autostart)
            {
                TurnOffTutorial();
            }
        }

        // Validates presence of all required components
        private void ValidateComponents()
        {
            if (!tutorialMaker)
                Debug.LogError($"[ATM] Missing TutorialMaker component!");          
            if (!inputController)
                Debug.LogError($"[ATM] Missing InputController component!");
            if (!sequencePlayer)
                Debug.LogError($"[ATM] Missing SequencePlayer component!");
            if (!visualManager)
                Debug.LogError($"[ATM] Missing VisualManager component!");
        }

        // Sets up references and initializes all components
        private void InitializeComponents()
        {
            if (visualManager)
            {
                visualManager.sceneReferences = this;
                visualManager.Initialize();
            }
            if (tutorialMaker)
            {
                tutorialMaker.sceneReferences = this;
                tutorialMaker.Initialize();
            }
           
            if (inputController)
            {
                inputController.sceneReferences = this;
                inputController.Initialize();
            }
            if (sequencePlayer)
            {
                sequencePlayer.sceneReferences = this;
                sequencePlayer.Initialize();
            }
        }

        #endregion
    }
}