using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace AutomaticTutorialMaker
{
    public class UIGraphicAnimation : MonoBehaviour, ITutorialGraphic
    {
        #region Variables
        private TutorialSceneReferences sceneReferences; // Scene component references
        private ClickData currentStep; // Current tutorial step
        private RectTransform rectTransform; // RectTransform component for positioning

        [Header("Settings")]
        public GraphicSettings settings; // Animation and visual settings
        public Color backgroundColor = new Color(0, 0, 0, 0); // Background color with alpha

        [Header("Positioning")]
        public bool useCurrentAnchors = false; // Whether to use current anchor settings
        public CanvasAnchor anchorPosition = CanvasAnchor.Center; // Position anchor point
        public Vector2 offset = Vector2.zero; // Position offset

        [Header("Components")]
        public TMP_Text textElement; // Text display component
        public List<Image> imagesToLerp = new List<Image>(); // Images for animation

        [Header("Button To Press First")]
        [SerializeField] private GameObject confirmButton; // Confirmation button

        [Header("Swipe Part To Rotate")]
        [SerializeField] private GameObject gestureGraphic; // Swipe gesture indicator
        private GameObject mainGraphic; // Main graphic container

        [Header("Joystick Part")]
        [SerializeField] private JoystickTipController joystickGraphic;

        private int raycastLayerIndex = 2; // Layer index used for raycasting operations

        private bool isAnimating = false; // Animation state
        private bool isReversing = false; // Reverse animation state
        private float animationProgress = 0f; // Animation progress
        private Vector3 initialScale; // Initial scale value
        private List<Color32> initialColor = new List<Color32>(); // Initial colors
        private Color32 initialTextColor; // Initial text color
        private bool destroyAfterAnimation = false; // Whether to destroy after animation

        private float idleTime = 0f; // Time in idle animation
        private float graphicFadeTime = 0f; // Time in fade animation
        private Vector3 lastIdlePosition; // Last idle animation position
        private Vector3 lastIdleScale; // Last idle animation scale
        private bool isIdleInitialized = false; // Idle animation initialization state
        private float blinkTime = 0f; // Time in blink animation
        private bool isBlinking = true; // Blink animation state

        private Vector2 currentPosition; // Current graphic position
        private Vector2 targetPosition; // Target graphic position
        private RectTransform canvasRectTransform; // Canvas RectTransform reference

        // Animation control flags
        private bool isIdleActive = false; // Idle animation active state
        private bool canPlayIdle = false; // Whether idle animation can play
        private bool hasReachedTarget = false; // Whether target position is reached

        private float timer; // General purpose timer
        private bool hasRequestedBackground = false; // Background request state

        private float almostZer = 0.01f;
        private float almostZero = 0.05f;
        private float halfIndex = 0.5f;
        private float doubleIndex = 2;
        private float almostOne = 0.95f;
        private float extraOne = 1.05f;
        private float pointThree = 0.3f;
        private float pointThree2 = 0.35f;

        #endregion

        #region MethodsForManualCall
        // Local Confirm Button can use this method too
        public void FinishStep()
        {
            if (sceneReferences)
            {
                sceneReferences.ForceCompleteStep(sceneReferences.tutorialMaker.stepSequence.IndexOf(currentStep));
            }
        }

        // Handles button confirmation initialization
        public void InitializeByConfirm()
        {
            if (confirmButton)
            {
                currentStep.fakeblockedByButton = false;
                sceneReferences.visualManager.DisableVisualsForStep(currentStep);
                Debug.Log("[ATM] Step unlocked by button.");
            }
        }

        public void SetText(string textValue)
        {
            if (textElement && !string.IsNullOrEmpty(textValue))
            {
                textElement.text = textValue;
            }
        }
        #endregion

        #region Initialization
        // Initializes graphic animation with tutorial step
        public void Initialize(ClickData step, TutorialSceneReferences references)
        {
            currentStep = step;
            sceneReferences = references;
            if (currentStep == null || sceneReferences == null)
            {
                Debug.LogError("[ATM] Tip initialization failed.");
                this.enabled = false;
                return;
            }
            mainGraphic = transform.gameObject;
            if (settings == null)
            {
                settings = sceneReferences.NoneGraphicSettings;
            }

            SetupSwipeDirection(step.interaction);

            rectTransform = GetComponent<RectTransform>();
            canvasRectTransform = references.targetCanvas.GetComponent<RectTransform>();

            initialScale = mainGraphic.transform.localScale;

            TextProcessing();

            ReassignColors();

            GraphicsOptimization();

            if(joystickGraphic && (currentStep.interaction == InteractionTypeEnum.JoystickButton || currentStep.interaction == InteractionTypeEnum.JoystickButtonHold) && currentStep.keyCodes.Count > 0)
            {
                KeyCode keyCode = step.keyCodes[0];
                int buttonIndex = (int)keyCode - (int)KeyCode.JoystickButton0;
                joystickGraphic.GetTipType(buttonIndex, sceneReferences);                
            }

            if (rectTransform != null)
            {
                currentPosition = rectTransform.anchoredPosition;
                targetPosition = currentPosition;
            }

            ApplyAnchorPosition();

            if (backgroundColor.a > 0)
            {
                sceneReferences.visualManager.RequestBackground(backgroundColor, settings.appearDuration);
                hasRequestedBackground = true;
            }

            if (confirmButton && confirmButton.activeSelf)
            {
                step.blockedByButton = true;
                step.fakeblockedByButton = true;
            }
            if (currentStep != null && currentStep.minTimeAmount > 0 && !currentStep.blockedByTime)
            {
                currentStep.blockedByTime = true;
            }
        }

        // Initializes time-based step completion
        private void InitializeByTime()
        {
            currentStep.blockedByTime = false;
        }

        private void GraphicsOptimization()
        {
            if (imagesToLerp != null && imagesToLerp.Count > 0)
            {
                foreach (var sprite in imagesToLerp)
                {
                    if (sprite != null && sprite.gameObject.activeSelf)
                    {
                        sprite.gameObject.layer = raycastLayerIndex;
                        sprite.raycastTarget = false;
                    }
                }
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                textElement.gameObject.layer = raycastLayerIndex;
                textElement.raycastTarget = false;
            }
        }

        // Handles text input
        private void TextProcessing()
        {
            if (currentStep == null || settings == null)
            {
                return;
            }

            string textTip = "";
            if (currentStep.localizationReference != null)
            {
                textTip = currentStep.localizationReference.GraphicText;
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                RectTransform textRect = textElement.GetComponent<RectTransform>();               
                if (!string.IsNullOrEmpty(textTip))
                {
                    SetText(textTip);
                }
                else
                {
                    textElement.gameObject.SetActive(false);
                    textElement.enabled = false;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(textTip))
                {
                    Debug.LogError("[ATM] Error: Graphic Text is assigned, but a text element is not present.");
                }
            }
        }

        private void ReassignColors()
        {
            // Save initial sprite colors
            initialColor.Clear();
            if (imagesToLerp != null)
            {
                foreach (var sprite in imagesToLerp)
                {
                    if (sprite != null)
                    {
                        initialColor.Add(sprite.color);
                    }
                }
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                initialTextColor = textElement.color;
            }
        }

        // Sets up swipe gesture direction
        private void SetupSwipeDirection(InteractionTypeEnum interactionType)
        {
            if (!gestureGraphic) return;

            switch (interactionType)
            {
                case InteractionTypeEnum.SwipeDown:
                    gestureGraphic.transform.localRotation *= Quaternion.Euler(0, 0, -90);
                    break;
                case InteractionTypeEnum.SwipeUp:
                    gestureGraphic.transform.localRotation *= Quaternion.Euler(0, 0, 90);
                    break;
                case InteractionTypeEnum.SwipeLeft:
                    Vector3 currentScale = gestureGraphic.transform.localScale;
                    gestureGraphic.transform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);
                    break;
            }

        }
        // Handles graphic update per frame
        public void Show(ClickData step)
        {
            currentStep = step;
            isAnimating = true;
            isReversing = false;
            animationProgress = 0f;
            destroyAfterAnimation = false;
            isIdleActive = true;
            canPlayIdle = false;
            hasReachedTarget = false;

            SetupInitialState(settings.appearAnimation);

            Debug.Log("[ATM] UI Graphic initialized. Local Scale: " + mainGraphic.transform.localScale + "; Local Position: " + mainGraphic.transform.localPosition);
        }

        // Sets up initial animation state
        private void SetupInitialState(AnimationType animationType)
        {
            if (!mainGraphic) return;

            if (!isReversing)
            {
                targetPosition = rectTransform.anchoredPosition;
            }

            switch (animationType)
            {
                case AnimationType.Zoom:
                    mainGraphic.transform.localScale = isReversing ? initialScale : Vector3.zero;
                    break;

                case AnimationType.Fade:
                    for (int i = 0; i < imagesToLerp.Count; i++)
                    {
                        imagesToLerp[i].color = new Color32(initialColor[i].r, initialColor[i].g, initialColor[i].b, 0);
                    }
                    if (textElement)
                    {
                        textElement.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, 0f);
                    }
                    break;

                case AnimationType.Slide:
                    if (!isReversing)
                    {
                        currentPosition = GetScreenCornerPosition(settings.slideStartCorner);
                        rectTransform.anchoredPosition = currentPosition;
                    }
                    break;
            }

            if (!isReversing)
            {
                mainGraphic.SetActive(true);
            }
        }

        // Hides the graphic with specified disappear animation
        public void Hide()
        {
            if (hasRequestedBackground)
            {
                sceneReferences.visualManager.ReleaseBackground(settings.disappearDuration);
                hasRequestedBackground = false;
            }

            isAnimating = true;
            isReversing = true;
            animationProgress = 0f;
            destroyAfterAnimation = true;

            if (settings.disappearAnimation == AnimationType.None)
            {
                Destroy(mainGraphic);
                return;
            }

            if (settings.disappearAnimation == AnimationType.Slide)
            {
                currentPosition = rectTransform.anchoredPosition;
                targetPosition = GetScreenCornerPosition(settings.slideEndCorner);
            }
            SetupInitialState(settings.disappearAnimation);
        }

        #endregion

        #region Movement

        // Reapplies anchor positioning
        public void UpdatePosition()
        {
            ApplyAnchorPosition();
        }

        // Applies anchor position settings to graphic
        private void ApplyAnchorPosition()
        {
            if (rectTransform == null) return;

            if (!useCurrentAnchors)
            {
                rectTransform.anchorMin = new Vector2(halfIndex, halfIndex);
                rectTransform.anchorMax = new Vector2(halfIndex, halfIndex);
                rectTransform.pivot = new Vector2(halfIndex, halfIndex);

                if (sceneReferences?.targetCanvas == null) return;

                Vector2 canvasSize = sceneReferences.targetCanvas.GetComponent<RectTransform>().rect.size;
                Vector2 position = Vector2.zero;

                switch (anchorPosition)
                {
                    case CanvasAnchor.TopLeft:
                        position = new Vector2(-canvasSize.x / doubleIndex, canvasSize.y / doubleIndex);
                        break;
                    case CanvasAnchor.TopCenter:
                        position = new Vector2(0, canvasSize.y / doubleIndex);
                        break;
                    case CanvasAnchor.TopRight:
                        position = new Vector2(canvasSize.x / doubleIndex, canvasSize.y / doubleIndex);
                        break;
                    case CanvasAnchor.MiddleLeft:
                        position = new Vector2(-canvasSize.x / doubleIndex, 0);
                        break;
                    case CanvasAnchor.Center:
                        position = Vector2.zero;
                        break;
                    case CanvasAnchor.MiddleRight:
                        position = new Vector2(canvasSize.x / doubleIndex, 0);
                        break;
                    case CanvasAnchor.BottomLeft:
                        position = new Vector2(-canvasSize.x / doubleIndex, -canvasSize.y / doubleIndex);
                        break;
                    case CanvasAnchor.BottomCenter:
                        position = new Vector2(0, -canvasSize.y / doubleIndex);
                        break;
                    case CanvasAnchor.BottomRight:
                        position = new Vector2(canvasSize.x / doubleIndex, -canvasSize.y / doubleIndex);
                        break;
                }

                position += offset;
                rectTransform.anchoredPosition = position;
            }
            else
            {
                rectTransform.anchoredPosition += offset;
            }
        }

        // Helper method to get screen corner position for slide animation
        private Vector2 GetScreenCornerPosition(ScreenCorner corner)
        {
            if (canvasRectTransform == null) return Vector2.zero;

            Vector2 canvasSize = canvasRectTransform.rect.size;
            float offset = settings.screenEdgeOffset;

            switch (corner)
            {
                case ScreenCorner.TopLeft:
                    return new Vector2(-canvasSize.x / doubleIndex - offset, canvasSize.y / doubleIndex + offset);
                case ScreenCorner.TopCenter:
                    return new Vector2(0, canvasSize.y / doubleIndex + offset);
                case ScreenCorner.TopRight:
                    return new Vector2(canvasSize.x / doubleIndex + offset, canvasSize.y / doubleIndex + offset);
                case ScreenCorner.MiddleLeft:
                    return new Vector2(-canvasSize.x / doubleIndex - offset, 0);
                case ScreenCorner.MiddleRight:
                    return new Vector2(canvasSize.x / doubleIndex + offset, 0);
                case ScreenCorner.BottomLeft:
                    return new Vector2(-canvasSize.x / doubleIndex - offset, -canvasSize.y / doubleIndex - offset);
                case ScreenCorner.BottomCenter:
                    return new Vector2(0, -canvasSize.y / doubleIndex - offset);
                case ScreenCorner.BottomRight:
                default:
                    return new Vector2(canvasSize.x / doubleIndex + offset, -canvasSize.y / doubleIndex - offset);
            }
        }
        #endregion

        #region Update
        // Updates graphic state every frame
        private void Update()
        {
            if (currentStep == null || sceneReferences == null)
            {
                Debug.LogError("[ATM] Tip initialization failed.");
                this.enabled = false;
                return;
            }
            if (isAnimating)
            {
                Animate();

                if (!isReversing)
                {
                    switch (settings.appearAnimation)
                    {
                        case AnimationType.Zoom:
                            float currentScale = mainGraphic.transform.localScale.x;
                            float targetScale = initialScale.x;
                            hasReachedTarget = currentScale >= targetScale * almostOne;
                            break;

                        case AnimationType.Fade:
                            if (imagesToLerp.Count > 0 || textElement) // && settings.graphicIdleAnimation != IdleAnimationType.Fade)
                            {
                                if (!isAnimating || animationProgress >= settings.appearDuration * extraOne)
                                {
                                    hasReachedTarget = true;
                                }
                                else
                                {
                                    hasReachedTarget = false;
                                }                               
                            }
                            else
                            {
                                hasReachedTarget = true;
                            }
                            break;

                        case AnimationType.Slide:
                            float distanceToTarget = Vector2.Distance(rectTransform.anchoredPosition, targetPosition);
                            hasReachedTarget = distanceToTarget <= (settings.screenEdgeOffset * almostZero);
                            break;

                        case AnimationType.None:
                            hasReachedTarget = true;
                            break;
                    }

                    canPlayIdle = hasReachedTarget;
                }
            }
            if (!isReversing && isIdleActive && canPlayIdle)
            {
                PlayIdleAnimation();

                if (currentStep != null && currentStep.minTimeAmount > 0 && currentStep.blockedByTime)
                {
                    if (timer < currentStep.minTimeAmount)
                    {
                        timer += Time.deltaTime;
                    }
                    else
                    {
                        InitializeByTime();
                    }
                }

            }
        }
        #endregion

        #region Animations
        // Processes animation updates
        private void Animate()
        {
            if (!mainGraphic) return;

            float duration = isReversing ? settings.disappearDuration : settings.appearDuration;
            float t = Mathf.Clamp01(animationProgress / duration);

            AnimationType currentAnimation = isReversing ? settings.disappearAnimation : settings.appearAnimation;

            bool targetReached = false;

            switch (currentAnimation)
            {
                case AnimationType.Zoom:
                    Vector3 currentScale = mainGraphic.transform.localScale;
                    Vector3 targetScale = isReversing ? Vector3.zero : initialScale;
                    mainGraphic.transform.localScale = Vector3.Lerp(
                        isReversing ? initialScale : Vector3.zero,
                        targetScale,
                        t
                    );
                    targetReached = Vector3.Distance(currentScale, targetScale) < almostZer;
                    break;

                case AnimationType.Fade:
                    if (imagesToLerp.Count > 0)
                    {
                        for (int i = 0; i < imagesToLerp.Count; i++)
                        {
                            Color currentColor = imagesToLerp[i].color;

                            if (!isReversing)
                            {
                                Color32 startColor = new Color32(initialColor[i].r, initialColor[i].g, initialColor[i].b, 0);
                                imagesToLerp[i].color = Color32.Lerp(startColor, initialColor[i], t);
                            }
                            else
                            {
                                Color32 targetColor = new Color(
                                    currentColor.r,
                                    currentColor.g,
                                    currentColor.b,
                                    0f
                                );
                                Color32 startColor = new Color32(initialColor[i].r, initialColor[i].g, initialColor[i].b, 0);

                                imagesToLerp[i].color = Color32.Lerp(
                                  isReversing ? initialColor[i] : startColor,
                                  targetColor,
                                  t
                              );
                            }
                        }
                    }
                    if (textElement)
                    {
                        Color textCurrentColor = textElement.color;
                        if (!isReversing)
                        {
                            Color32 startColor = new Color32(
                                initialTextColor.r,
                                initialTextColor.g,
                                initialTextColor.b,
                                0
                            );
                            textElement.color = Color32.Lerp(startColor, initialTextColor, t);
                        }
                        else
                        {
                            Color32 targetColor = new Color(
                                   textCurrentColor.r,
                                   textCurrentColor.g,
                                   textCurrentColor.b,
                                   0f
                               );
                            Color32 startColor = new Color32(initialTextColor.r, initialTextColor.g, initialTextColor.b, 0);

                            textElement.color = Color32.Lerp(
                              isReversing ? initialTextColor : startColor,
                              targetColor,
                              t
                          );
                        }
                    }

                    break;

                case AnimationType.Slide:
                    currentPosition = Vector2.Lerp(currentPosition, targetPosition, t);
                    rectTransform.anchoredPosition = currentPosition;
                    targetReached = Vector2.Distance(currentPosition, targetPosition) < almostZer;
                    break;

                default:
                    targetReached = true;
                    break;
            }

            animationProgress += Time.deltaTime;

            if (targetReached || animationProgress >= duration * 1.5f)
            {
                isAnimating = false;
                if (destroyAfterAnimation)
                {
                    if (isReversing && currentAnimation == AnimationType.Slide)
                    {
                        float distanceToTarget = Vector2.Distance(currentPosition, targetPosition);
                        if (distanceToTarget < almostZer)
                        {
                            Destroy(mainGraphic);
                        }
                    }
                    else
                    {
                        Destroy(mainGraphic);
                    }
                }

                if (!isReversing && !destroyAfterAnimation)
                {
                    switch (currentAnimation)
                    {
                        case AnimationType.Zoom:
                            mainGraphic.transform.localScale = initialScale;
                            break;

                        case AnimationType.Fade:
                            break;

                        case AnimationType.Slide:
                            rectTransform.anchoredPosition = targetPosition;
                            break;
                    }
                }
            }
        }

        // Executes idle animation updates
        private void PlayIdleAnimation()
        {
            if (currentStep == null || settings == null) return;

            if (!mainGraphic || !isIdleInitialized)
            {
                lastIdlePosition = mainGraphic.transform.localPosition;
                lastIdleScale = mainGraphic.transform.localScale;
                isIdleInitialized = true;
                return;
            }

            switch (settings.graphicIdleAnimation)
            {
                case IdleAnimationType.Levitate:
                    idleTime += Time.deltaTime * settings.levitationSpeed;
                    float levitateProgress = (Mathf.Sin(idleTime) + 1f) * halfIndex;
                    Vector3 levitateOffset = settings.levitationDirection * settings.levitationRange * levitateProgress;
                    mainGraphic.transform.localPosition = lastIdlePosition + levitateOffset;
                    break;

                case IdleAnimationType.Pulse:
                    idleTime += Time.deltaTime * settings.pulseSpeed;
                    float scaleDelta = Mathf.Sin(idleTime) * settings.pulseDelta;
                    Vector3 targetScale = initialScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                    mainGraphic.transform.localScale = Vector3.Lerp(
                        mainGraphic.transform.localScale,
                        targetScale,
                        Time.deltaTime * settings.pulseSpeed
                    );
                    break;

                case IdleAnimationType.Fade:
                    graphicFadeTime += Time.deltaTime;

                    float entryProgress = Mathf.Min(graphicFadeTime / pointThree, 1f);

                    float cycleProgress = (graphicFadeTime * doubleIndex * Mathf.PI) / settings.fadeDuration;
                    float fadeMultiplier = pointThree + (Mathf.Sin(cycleProgress) + 1f) * pointThree2;

                    fadeMultiplier = Mathf.Lerp(1f, fadeMultiplier, entryProgress);

                    if (imagesToLerp.Count > 0)
                    {
                        for (int i = 0; i < imagesToLerp.Count; i++)
                        {
                            byte targetAlpha = (byte)(initialColor[i].a * fadeMultiplier);
                            Color32 targetColor = new Color32(
                                initialColor[i].r,
                                initialColor[i].g,
                                initialColor[i].b,
                                targetAlpha
                            );
                            imagesToLerp[i].color = targetColor;
                        }
                    }

                    if (textElement)
                    {
                        byte targetTextAlpha = (byte)(initialTextColor.a * fadeMultiplier);
                        Color32 targetTextColor = new Color32(
                            initialTextColor.r,
                            initialTextColor.g,
                            initialTextColor.b,
                            targetTextAlpha
                        );
                        textElement.color = targetTextColor;
                    }
                    break;

                case IdleAnimationType.Blink:

                    blinkTime += Time.deltaTime;
                    if (blinkTime >= settings.blinkInterval)
                    {
                        blinkTime = 0f;
                        isBlinking = !isBlinking;
                        if (imagesToLerp.Count > 0)
                        {
                            for (int i = 0; i < imagesToLerp.Count; i++)
                            {
                                imagesToLerp[i].gameObject.SetActive(isBlinking);
                            }
                        }

                        if (textElement && textElement.enabled)
                        {
                            textElement.gameObject.SetActive(isBlinking);
                        }
                    }
                    break;
            }
        }

        #endregion

        #region Helpers
        // Destroys the main graphic object
        public void Destroy()
        {
            if (mainGraphic)
            {
                Destroy(mainGraphic);
            }
        }
        #endregion
    }
}