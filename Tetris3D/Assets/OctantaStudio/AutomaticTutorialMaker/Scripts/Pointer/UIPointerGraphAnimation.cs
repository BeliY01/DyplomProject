using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutomaticTutorialMaker
{
    // Manages pointer graphic animations and interactions in tutorial system
    public class UIPointerGraphAnimation : MonoBehaviour
    {
        #region Variables
        [ReadOnly] public bool isHover = false;
        private ClickData currentStep; // Current tutorial step data
        [ReadOnly] public TutorialSceneReferences sceneReferences; // References to tutorial scene objects
        [HideInInspector] public bool destroyCommand; // Flag to trigger pointer destruction

        [Header("Settings")]
        public PointerSettings settings; // Configuration settings for pointer behavior

        private GameObject mainGraphic; // Main graphic object for the pointer
        [Header("Components")]
        public TMP_Text textElement; // Text component for displaying instructions
        public List<Image> imagesToLerp = new List<Image>(); // Images to interpolate during animations
        private RectTransform textRect; // Rect transform of the text element
        private GameObject pointerObject; // Main visual object for the pointer

        [Header("Mouse Parts")]
        [SerializeField] private Transform leftClickIcon; // Icon for left mouse button click
        [SerializeField] private Transform rightClickIcon; // Icon for right mouse button click
        [SerializeField] private Transform scrollIcon; // Icon for mouse scroll

        [SerializeField][ReadOnly] private Transform currentInteractionIcon; // Currently active interaction icon

        private int raycastLayerIndex = 2; // Layer index used for raycasting operations

        private RectTransform canvasRectTransform; // Canvas rect transform for positioning
        private Dictionary<GameObject, RectTransform> cachedRectTransforms = new Dictionary<GameObject, RectTransform>(); // Cache for rect transforms
        private Dictionary<GameObject, Renderer> cachedRenderers = new Dictionary<GameObject, Renderer>(); // Cache for renderers
        private Dictionary<GameObject, Renderer[]> cachedChildRenderers = new Dictionary<GameObject, Renderer[]>(); // Cache for child renderers

        private Color32 transparentColor = new Color32(0, 0, 0, 0); // Fully transparent color

        private Vector3 initialScale; // Starting scale of pointer
        private List<Color32> initialColor = new List<Color32>(); // Initial colors of images
        private bool isAnimating = false; // Whether animation is in progress
        private bool isReversing = false; // Whether animation is playing in reverse
        private bool isIdleActive = false; // Whether idle animation is playing
        private float animationProgress = 0f; // Current animation time progress

        // Pointer idle animation timers
        private float pointerLevitationTime = 0f; // For pointer levitate
        private float pointerPulseTime = 0f; // For pointer pulse
        private float pointerFadeTime = 0f; // For pointer fade
        private float blinkTimer; // Timer for blinking effect
        private bool isObjectVisible; // Flag for object visibility during blinking

        // Object idle animation timers
        private float blinkTime = 0; // Blink timer for graphic
        private Vector3 startPosition; // Initial position for animations
        private Vector2 currentPosition; // Current position during animation

        private Vector2 currentVelocity; // Current movement velocity
        private RectTransform pointerRectTransform; // Pointer's RectTransform component
        private Vector2 targetPosition; // Target position for movement
       [ReadOnly] public List<UnityEngine.Object> targetObjects = new List<UnityEngine.Object>(); // Current objects to point at
        private Dictionary<GameObject, Renderer> targetRenderers = new Dictionary<GameObject, Renderer>(); // Cache for target renderers
        private Dictionary<GameObject, Renderer[]> targetChildRenderers = new Dictionary<GameObject, Renderer[]>(); // Cache for child renderers

        private int currentTargetIndex = 0; // Index of current target object
        private float delayTimer = 0f; // Timer for position delay
        private bool isWaitingAtPosition = false; // Whether pointer is waiting at position
        private Vector2 currentObjectPosition; // Current object position
        private Vector3 lastPointerIdlePosition; // Last idle position of pointer
        private Vector3 lastPointerIdleScale; // Last idle scale of pointer
        private Vector3 lastGraphicIdlePosition; // Last idle position of graphic
        private Vector3 lastGraphicIdleScale; // Last idle scale of graphic
        private bool isIdleInitialized = false; // Whether idle state is initialized

        private bool isMoving = true; // Whether pointer is currently moving

        // Initial values of graphic elements
        private Color32 initialTextColor; // Initial text color
        private Vector3 initialTextScale; // Initial text scale
        private Vector3 initialGraphicScale; // Initial graphic scale
        private Vector3 initialObjectScale; // Initial object scale
        private bool canPlayIdle = false; // Whether idle animation can be played
        private bool hasReachedTarget = false; // Whether pointer has reached target

        // Constants for double-click animation
        private const float doubleClickCycleTime = 0.8f; // Total cycle time for double-click
        private const float firstClickDuration = 0.1f; // Duration of first click
        private const float clickInterval = 0.1f; // Interval between clicks
        private const float secondClickDuration = 0.1f; // Duration of second click
        private const float postDoubleClickPause = 0.5f; // Pause after double-click

        // Alpha range constants
        private const float minAlpha = 0f; // Minimum alpha value
        private const float maxAlpha = 1f; // Maximum alpha value

        // Timing constants for click sequence
        private const float firstClickEnd = firstClickDuration;
        private const float intervalEnd = firstClickEnd + clickInterval;
        private const float secondClickEnd = intervalEnd + secondClickDuration;

        private Vector3 levitationOffset = Vector3.zero; // Offset for levitation animation
        private Vector2 initialTextOffset; // Initial text offset
        private TextAlignmentOptions initialAlignment; // Initial text alignment

        private float timer; // General-purpose timer
        private Dictionary<GameObject, CachedObjectData> objectCache = new Dictionary<GameObject, CachedObjectData>(); // Cache for object data

        private float almostZero = 0.01f;
        private float almostOne = 0.95f;
        private float extraOne = 1.05f;
        private float pointOne = 0.1f;
        private float halfIndex = 0.5f;
        private float doubleIndex = 2;
        private float pointThree = 0.3f;
        private float pointThree2 = 0.35f;

        // Caches only immutable components
        private class CachedObjectData
        {
            public RectTransform rectTransform;
            public Renderer mainRenderer;
            public Renderer[] childRenderers;
            public bool isUI;
        }

        #endregion

        #region MethodsForManualCall
        public void SetText(string textValue)
        {
            if (textElement && !string.IsNullOrEmpty(textValue))
            {
                textElement.text = textValue;
            }
        }
        #endregion

        #region Initialization
        // Initialize pointer animation parameters
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
            pointerObject = transform.gameObject;

            if (settings == null)
            {
                Debug.Log("[ATM] UIPointerSettings not found on prefab, adding default configuration.");
                settings = sceneReferences.NonePointerSettings;
            }

            startPosition = pointerObject.transform.localPosition;
            initialScale = pointerObject.transform.localScale;
            pointerRectTransform = GetComponent<RectTransform>();
            canvasRectTransform = sceneReferences.targetCanvas.GetComponent<RectTransform>();

            GraphicsOptimization();

            TextProcessing();

            ReassignColors();

            // Interaction icon
            SetupInteractionIcon(currentStep.interaction);

            if (leftClickIcon && leftClickIcon.gameObject.activeSelf)
            {
                leftClickIcon.gameObject.SetActive(false);
            }
            if (rightClickIcon && rightClickIcon.gameObject.activeSelf)
            {
                rightClickIcon.gameObject.SetActive(false);
            }
            if (scrollIcon && scrollIcon.gameObject.activeSelf)
            {
                scrollIcon.gameObject.SetActive(false);
            }
           
            if (currentStep != null && currentStep.minTimeAmount > 0 && !currentStep.blockedByTime)
            {
                currentStep.blockedByTime = true;
            }
        }

        // Regulates time-based step
        private void InitializeByTime()
        {
            currentStep.blockedByTime = false;
        }

        // Sets initial animation state based on type
        private void SetupInitialState(AnimationType animationType)
        {
            GameObject targetObj = targetObjects[currentTargetIndex] as GameObject;
            if (targetObj != null)
            {
                ObjectTypeEnum targetType = currentStep?.objectTypes != null &&
                    currentStep.objectTypes.Count > currentTargetIndex ?
                    currentStep.objectTypes[currentTargetIndex] :
                    ObjectTypeEnum.None;

                UpdatePointerTargetPosition(targetObj, targetType);
            }

            if (animationType == AnimationType.Slide)
            {
                ScreenCorner corner = isReversing ?
                    settings.slideEndCorner :
                    settings.slideStartCorner;

                currentPosition = GetScreenCornerPosition(corner);
                pointerRectTransform.anchoredPosition = currentPosition;
            }
            else
            {

                if (isHover)
                {
                    targetObj = targetObjects[targetObjects.Count - 1] as GameObject;
                    if (targetObj != null)
                    {
                        UpdatePointerTargetPosition(targetObj, ObjectTypeEnum.None);
                    }
                }
                pointerRectTransform.anchoredPosition = targetPosition;
                currentPosition = targetPosition;
            }

            switch (animationType)
            {
                case AnimationType.Zoom:
                    pointerObject.transform.localScale = isReversing ? initialScale : Vector3.zero;
                    break;

                case AnimationType.Fade:
                    SetColor(transparentColor);
                    if (textElement)
                    {
                        textElement.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, 0f);
                    }
                    break;
            }

            if (!isReversing)
            {
                gameObject.SetActive(true);
            }

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
            if (currentStep == null || settings == null || isHover)
            {
                return;
            }

            string textTip = "";
            if (currentStep.localizationReference != null)
            {
                textTip = currentStep.localizationReference.PointerText;
            }
            if (textElement && textElement.gameObject.activeSelf)
            {
                initialTextScale = textElement.transform.localScale;
                RectTransform textRect = textElement.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    initialTextOffset = textRect.anchoredPosition;
                    initialAlignment = textElement.alignment;
                }
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
                    Debug.LogError("[ATM] Error: PointerText is assigned, but a text element is not present.");
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

        // Resets pointer state when target objects are updated
        private void ResetPointerState()
        {
            currentTargetIndex = 0;
            delayTimer = 0f;
            isWaitingAtPosition = false;
            levitationOffset = Vector3.zero;
            isIdleInitialized = false;

            if (pointerRectTransform && targetObjects != null && targetObjects.Count > 0)
            {
                GameObject targetObj = targetObjects[0] as GameObject;
                if (targetObj != null)
                {
                    ObjectTypeEnum targetType = currentStep?.objectTypes != null &&
                            currentStep.objectTypes.Count > currentTargetIndex ?
                            currentStep.objectTypes[currentTargetIndex] :
                            ObjectTypeEnum.None;
                    UpdatePointerTargetPosition(targetObj, targetType);
                    pointerRectTransform.anchoredPosition = targetPosition;
                    lastPointerIdlePosition = pointerObject.transform.localPosition;
                }
            }
        }
        #endregion

        #region Update
        // Update animations each frame
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
                            float currentScale = pointerObject.transform.localScale.x;
                            float targetScale = initialScale.x;
                            hasReachedTarget = currentScale >= targetScale * almostOne;
                            break;

                        case AnimationType.Fade:
                            if (imagesToLerp.Count > 0 || textElement) 
                            {
                                if (!isAnimating || animationProgress >= settings.appearDuration * extraOne)
                                {
                                    hasReachedTarget = true;
                                }
                                else
                                {
                                    hasReachedTarget = false;
                                }

                                if (imagesToLerp.Count > 0)
                                {
                                    for (int i = 0; i < imagesToLerp.Count; i++)
                                    {
                                        float currentAlpha = imagesToLerp[i].color.a;
                                        float targetAlpha = initialColor[i].a;
                                    }
                                }
                                if (textElement)
                                {
                                    float currentAlpha = textElement.color.a;
                                    float targetAlpha = initialTextColor.a;
                                }
                            }
                            else
                            {
                                hasReachedTarget = true;
                            }
                            break;

                        case AnimationType.Slide:
                            float distanceToTarget = Vector2.Distance(pointerRectTransform.anchoredPosition, targetPosition);
                            hasReachedTarget = distanceToTarget <= pointOne;
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

                if (!isMoving || targetObjects == null || targetObjects.Count == 0)
                    return;

                UpdateTargetPosition();
                UpdatePointerMovement();
                PlayIdleAnimation();
                PlayMouseIdleAnimation();
            }
        }

        // Configures interaction icon based on interaction type
        private void SetupInteractionIcon(InteractionTypeEnum interactionType)
        {
            if (leftClickIcon) leftClickIcon.gameObject.SetActive(false);
            if (rightClickIcon) rightClickIcon.gameObject.SetActive(false);
            if (scrollIcon) scrollIcon.gameObject.SetActive(false);

            switch (interactionType)
            {
                case InteractionTypeEnum.Click:
                case InteractionTypeEnum.DoubleClick:
                case InteractionTypeEnum.Hold:
                case InteractionTypeEnum.DragAndDrop:
                case InteractionTypeEnum.Drag:
                    currentInteractionIcon = leftClickIcon;
                    break;

                case InteractionTypeEnum.RightDrag:
                case InteractionTypeEnum.RightClick:
                case InteractionTypeEnum.RightDoubleClick:
                case InteractionTypeEnum.RightHold:
                    currentInteractionIcon = rightClickIcon;
                    break;

                case InteractionTypeEnum.ScrollUp:
                case InteractionTypeEnum.ScrollDown:
                case InteractionTypeEnum.MiddleClick:
                case InteractionTypeEnum.MiddleHold:
                    currentInteractionIcon = scrollIcon;
                    break;

                default:
                    currentInteractionIcon = null;
                    break;
            }

            if (currentInteractionIcon)
            {
                currentInteractionIcon.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Animation
        // Handles mouse interaction icon animation for different interaction types
        private void PlayMouseIdleAnimation()
        {
            if (!canPlayIdle || currentStep == null || settings == null) return;

            if (currentStep.interaction != InteractionTypeEnum.Hold && currentStep.interaction != InteractionTypeEnum.RightHold && currentStep.interaction != InteractionTypeEnum.RightDrag && currentStep.interaction != InteractionTypeEnum.Drag && currentStep.interaction != InteractionTypeEnum.MiddleHold)
            {
                blinkTime += Time.deltaTime;

                if (currentStep.interaction == InteractionTypeEnum.DoubleClick ||
                    currentStep.interaction == InteractionTypeEnum.RightDoubleClick)
                {
                    float cycleTime = blinkTime % doubleClickCycleTime;

                    if (currentInteractionIcon != null)
                    {
                        if (cycleTime < firstClickEnd)
                        {
                            currentInteractionIcon.gameObject.SetActive(true);
                        }
                        else if (cycleTime < intervalEnd)
                        {
                            currentInteractionIcon.gameObject.SetActive(false);
                        }
                        else if (cycleTime < secondClickEnd)
                        {
                            currentInteractionIcon.gameObject.SetActive(true);
                        }
                        else
                        {
                            currentInteractionIcon.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (blinkTime >= settings.blinkInterval)
                    {
                        blinkTime = 0f;

                        if (currentInteractionIcon != null)
                        {
                            bool isCurrentlyActive = currentInteractionIcon.gameObject.activeSelf;
                            currentInteractionIcon.gameObject.SetActive(!isCurrentlyActive);
                        }
                    }
                }
            }
            else
            {
                if (currentInteractionIcon && !currentInteractionIcon.gameObject.activeSelf)
                {
                    currentInteractionIcon.gameObject.SetActive(true);
                }
            }
        }

        // Start pointer appearance animation
        public void StartAnimation()
        {
            isAnimating = true;
            isReversing = false;
            isIdleActive = true;
            animationProgress = 0f;
            canPlayIdle = false;
            hasReachedTarget = false;

            SetupInitialState(settings.appearAnimation);
            if (settings.appearAnimation == AnimationType.None)
            {
                gameObject.SetActive(true);
                isAnimating = false;
                canPlayIdle = true;
                hasReachedTarget = true;
            }
            isIdleInitialized = false;
            Debug.Log("[ATM] UI Pointer initialized. Local Scale: " + pointerObject.transform.localScale + "; Local Position: " + pointerObject.transform.localPosition);
        }

        // Start pointer disappearance animation
        public void FinishAnimation()
        {
            isAnimating = true;
            isReversing = true;
            isIdleActive = false;
            animationProgress = 0f;

            if (settings.disappearAnimation == AnimationType.None)
            {
                isAnimating = false;
            }
            else
            {
                destroyCommand = true;
            }
        }

        // Performs animation interpolation
        public void Animate()
        {
            float duration = isReversing ?
                settings.disappearDuration :
                settings.appearDuration;

            float t = Mathf.Clamp01(animationProgress / duration);

            AnimationType currentAnimation = isReversing ?
                settings.disappearAnimation :
                settings.appearAnimation;

            if (currentAnimation == AnimationType.Zoom)
            {
                pointerObject.transform.localScale = Vector3.Lerp(
                    isReversing ? initialScale : Vector3.zero,
                    isReversing ? Vector3.zero : initialScale,
                    t);
            }
            else if (currentAnimation == AnimationType.Fade)
            {
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
            }
            else if (currentAnimation == AnimationType.Slide)
            {
                Vector2 startPos = GetScreenCornerPosition(
                    isReversing ? settings.slideStartCorner :
                    settings.slideStartCorner
                );

                Vector2 endPos = isReversing ?
                    GetScreenCornerPosition(settings.slideEndCorner) :
                    targetPosition;

                currentPosition = Vector2.Lerp(startPos, endPos, t);
                pointerRectTransform.anchoredPosition = currentPosition;
            }

            animationProgress += Time.deltaTime;
            if (animationProgress >= duration)
            {
                isAnimating = false;
                if (destroyCommand)
                {
                    if (isReversing && currentAnimation == AnimationType.Slide)
                    {
                        Vector2 endCornerPos = GetScreenCornerPosition(
                            settings.slideEndCorner
                        );

                        if (Vector2.Distance(currentPosition, endCornerPos) < almostZero)
                        {
                            Destroy(gameObject);
                        }
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        // Process idle animations
        private void PlayIdleAnimation()
        {
            if (!canPlayIdle || currentStep == null || settings == null) return;

            if (!isIdleInitialized)
            {
                lastPointerIdlePosition = pointerObject.transform.localPosition;
                lastPointerIdleScale = pointerObject.transform.localScale;
                levitationOffset = Vector3.zero;
                isIdleInitialized = true;
            }

            if (settings.pointerIdleAnimation == IdleAnimationType.Levitate)
            {
                if (!hasReachedTarget) return;
                if (currentStep.interaction != InteractionTypeEnum.DragAndDrop)
                {
                    pointerLevitationTime += Time.deltaTime * settings.levitationSpeed;
                    float progress = (Mathf.Sin(pointerLevitationTime) + 1f) * halfIndex;

                    Vector3 minOffset = -settings.levitationDirection.normalized *
                                      settings.levitationRange;
                    Vector3 maxOffset = settings.levitationDirection.normalized *
                                      settings.levitationRange;

                    levitationOffset = Vector3.Lerp(minOffset, maxOffset, progress);

                    pointerObject.transform.localPosition = lastPointerIdlePosition + levitationOffset;
                }
            }
            else if (settings.pointerIdleAnimation == IdleAnimationType.Pulse)
            {
                pointerPulseTime += Time.deltaTime * settings.pulseSpeed;
                float scaleDelta = Mathf.Sin(pointerPulseTime) * settings.pulseDelta;
                Vector3 targetScale = initialScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);

                pointerObject.transform.localScale = Vector3.Lerp(
                    pointerObject.transform.localScale,
                    targetScale,
                    Time.deltaTime * settings.pulseSpeed
                );
            }
            else if (settings.pointerIdleAnimation == IdleAnimationType.Blink)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= settings.blinkInterval)
                {
                    blinkTimer = 0f;
                    isObjectVisible = !isObjectVisible;
                    if (imagesToLerp.Count > 0)
                    {
                        for (int i = 0; i < imagesToLerp.Count; i++)
                        {
                            imagesToLerp[i].gameObject.SetActive(isObjectVisible);
                        }
                    }
                    if (textElement && textElement.enabled)
                    {
                        textElement.gameObject.SetActive(isObjectVisible);
                    }
                }
            }
            else if (settings.pointerIdleAnimation == IdleAnimationType.Fade)
            {
                pointerFadeTime += Time.deltaTime;

                float entryProgress = Mathf.Min(pointerFadeTime / pointThree, 1f);

                float cycleProgress = (pointerFadeTime * doubleIndex * Mathf.PI) / settings.fadeDuration;
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
            }
        }

        #endregion

        #region Targets
        // Updates the list of target objects for the pointer
        public void UpdateTargetObjects(List<UnityEngine.Object> objects)
        {
            targetObjects = objects;
            objectCache.Clear();

            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj is GameObject gameObj)
                    {
                        CacheObjectData(gameObj);
                    }
                }
            }
            ResetPointerState();
        }

        // Caches data for a specific target object for performance optimization
        private void CacheObjectData(GameObject targetObj)
        {
            var cachedData = new CachedObjectData();
            var rectTransform = targetObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                cachedData.rectTransform = rectTransform;
                cachedData.isUI = true;
            }
            else if (!settings.usePivots)
            {
                cachedData.isUI = false;

                var mainRenderer = targetObj.GetComponent<Renderer>();
                if (mainRenderer != null)
                {
                    cachedData.mainRenderer = mainRenderer;
                }
                else
                {
                    cachedData.childRenderers = targetObj.GetComponentsInChildren<Renderer>();
                }
            }

            objectCache[targetObj] = cachedData;
        }

        // Updates target position for pointer movement
        private void UpdateTargetPosition()
        {
            if (targetObjects == null || targetObjects.Count == 0 || (targetObjects.Count > 0 && !targetObjects[currentTargetIndex]))
            {
                Debug.LogError("[ATM] The pointer was disabled because its target is no longer present in the scene.");
                FinishAnimation();
                return;
            }
            if (settings == null)
            {
                Debug.LogError("[ATM] The pointer has no settings.");
                return;
            }

            if (!isWaitingAtPosition && Vector2.Distance(pointerRectTransform.anchoredPosition, targetPosition) < settings.positionThreshold)
            {
                isWaitingAtPosition = true;
                delayTimer = 0f;
            }

            if (isWaitingAtPosition)
            {
                float currentDelay = currentTargetIndex == 0 ?
                   settings.delayOnFirstObject :
                   settings.defaultDelay;
                delayTimer += Time.deltaTime;

                if (delayTimer >= currentDelay)
                {
                    currentTargetIndex = (currentTargetIndex + 1) % targetObjects.Count;
                    isWaitingAtPosition = false;
                    delayTimer = 0f;
                }
            }

            GameObject targetObj = targetObjects[currentTargetIndex] as GameObject;
            
            if (targetObj != null)
            {
                int temporalIndex = currentTargetIndex;
                if (isHover)
                {
                    temporalIndex = currentStep.objectTypes.Count - 1;
                }
                ObjectTypeEnum targetType = currentStep?.objectTypes != null && currentStep.objectTypes.Count > temporalIndex ?
                    currentStep.objectTypes[temporalIndex] :
                    ObjectTypeEnum.None;
                UpdatePointerTargetPosition(targetObj, targetType);
            }
        }
        // Updates pointer target position based on object type
        private void UpdatePointerTargetPosition(GameObject targetObj, ObjectTypeEnum targetType)
        {
            if (targetType == ObjectTypeEnum.None)
            {
                targetType = sceneReferences.tutorialMaker.GetObjectType(targetObj);
            }

            if (pointerRectTransform.parent != sceneReferences.targetCanvas.transform)
            {
                pointerRectTransform.SetParent(sceneReferences.targetCanvas.transform);
            }

            if (targetType == ObjectTypeEnum.UI)
            {
                HandleUITarget(targetObj);
            }
            else if (targetType == ObjectTypeEnum._3D || targetType == ObjectTypeEnum._2D)
            {
                Handle3D2DTarget(targetObj);
            }
        }

        // Handles positioning for UI target objects
        private void HandleUITarget(GameObject targetObj)
        {

            if (!objectCache.TryGetValue(targetObj, out var cachedData) || !cachedData.isUI)
                return;


            Vector3[] corners = new Vector3[4];
            cachedData.rectTransform.GetWorldCorners(corners);
            Vector3 centerWorld = (corners[0] + corners[2]) / doubleIndex;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                sceneReferences.targetCanvas.worldCamera,
                centerWorld
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPoint,
                sceneReferences.targetCanvas.worldCamera,
                out Vector2 localPoint
            );

            targetPosition = localPoint;
        }

        // Calculates world position for 3D/2D target objects
        private Vector3 GetTargetWorldPosition(GameObject targetObj, CachedObjectData cachedData)
        {
            if (settings.usePivots)
            {
                return targetObj.transform.position;
            }

            if (cachedData.mainRenderer != null)
            {
                return cachedData.mainRenderer.bounds.center;
            }
            else if (cachedData.childRenderers != null && cachedData.childRenderers.Length > 0)
            {
                Bounds combinedBounds = cachedData.childRenderers[0].bounds;
                for (int i = 1; i < cachedData.childRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(cachedData.childRenderers[i].bounds);
                }
                return combinedBounds.center;
            }

            return targetObj.transform.position;
        }

        // Handles positioning for 3D and 2D target objects
        private void Handle3D2DTarget(GameObject targetObj)
        {
            if (!objectCache.TryGetValue(targetObj, out var cachedData) || cachedData.isUI)
                return;

            Vector3 worldPosition = GetTargetWorldPosition(targetObj, cachedData);
            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(worldPosition);

            bool isBehind = screenPoint.z < 0;
            if (isBehind)
            {
                screenPoint = -screenPoint;
            }
            bool isOffScreen = IsPointOffScreen(screenPoint);

            if (textElement)
            {
                if (settings.pointerIdleAnimation != IdleAnimationType.Blink)
                {
                    textElement.gameObject.SetActive(!isOffScreen && !isBehind);
                }
            }

            if (isOffScreen)
            {
                Vector2 edgePoint = GetScreenEdgePoint(new Vector2(screenPoint.x, screenPoint.y));

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    edgePoint,
                    sceneReferences.targetCanvas.worldCamera,
                    out Vector2 localPoint))
                {
                    targetPosition = localPoint;

                    if (settings.rotateWhenOffscreen)
                    {
                        Vector2 screenCenter = new Vector2(Screen.width * halfIndex, Screen.height * halfIndex);
                        Vector2 toTarget = (new Vector2(screenPoint.x, screenPoint.y) - screenCenter).normalized;
                        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

                        if (isBehind)
                        {
                            angle += 180f;
                        }

                        angle += settings.baseRotation;
                        ApplyRotation(angle);
                    }
                }
            }
            else
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    screenPoint,
                    sceneReferences.targetCanvas.worldCamera,
                    out Vector2 localPoint))
                {
                    targetPosition = localPoint;
                    HandleOnscreenRotation();
                }
            }
        }

        #endregion

        #region Movement
        // Updates pointer movement and smoothly interpolates to target position
        private void UpdatePointerMovement()
        {
            if (pointerRectTransform == null) return;
            pointerRectTransform.anchoredPosition = Vector2.SmoothDamp(
                pointerRectTransform.anchoredPosition,
                targetPosition,
                ref currentVelocity,
                settings.smoothTime,
                settings.moveSpeed
            );

            if (settings.pointerIdleAnimation == IdleAnimationType.Levitate)
            {
                lastPointerIdlePosition = pointerObject.transform.localPosition - levitationOffset;
            }

            if (!destroyCommand)
            {
                UpdateTextPosition();
            }
        }

        // Checks if a point is off the screen
        private bool IsPointOffScreen(Vector3 screenPoint)
        {
            float padding = settings.screenEdgePadding;
            return screenPoint.x < padding ||
                   screenPoint.x > Screen.width - padding ||
                   screenPoint.y < padding ||
                   screenPoint.y > Screen.height - padding;
        }

        // Calculates the point where an off-screen object intersects the screen edge
        private Vector2 GetScreenEdgePoint(Vector2 screenPoint)
        {
            Vector2 screenCenter = new Vector2(Screen.width * halfIndex, Screen.height * halfIndex);
            Vector2 direction = (screenPoint - screenCenter).normalized;

            float padding = settings.screenEdgePadding;
            float screenWidth = Screen.width - padding * doubleIndex;
            float screenHeight = Screen.height - padding * doubleIndex;

            float slope = direction.y / direction.x;
            float xEdge = Mathf.Abs(direction.x) < almostZero ?
                screenCenter.x :
                (direction.x > 0 ? screenWidth + padding : padding);
            float yEdge = Mathf.Abs(direction.y) < almostZero ?
                screenCenter.y :
                (direction.y > 0 ? screenHeight + padding : padding);

            float xIntersect = direction.x != 0 ? (xEdge - screenCenter.x) / direction.x : float.MaxValue;
            float yIntersect = direction.y != 0 ? (yEdge - screenCenter.y) / direction.y : float.MaxValue;

            if (Mathf.Abs(xIntersect) < Mathf.Abs(yIntersect))
            {
                return new Vector2(xEdge, screenCenter.y + slope * (xEdge - screenCenter.x));
            }
            else
            {
                return new Vector2(screenCenter.x + (yEdge - screenCenter.y) / slope, yEdge);
            }
        }

        // Calculates the screen corner position based on specified corner with an offset
        private Vector2 GetScreenCornerPosition(ScreenCorner corner)
        {
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
        //// Stops pointer movement
        //public void StopMovement()
        //{
        //    isMoving = false;
        //}

        //// Resumes pointer movement
        //public void StartMovement()
        //{
        //    isMoving = true;
        //}
        #endregion

        #region Rotation
        // Handles rotation for onscreen objects
        private void HandleOnscreenRotation()
        {
            Quaternion baseRotation = Quaternion.Euler(0, 0, settings.baseRotation);
            ApplyRotation(settings.baseRotation);
        }

        // Applies rotation to the pointer object
        private void ApplyRotation(float angle)
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

            if (settings.smoothRotation)
            {
                pointerObject.transform.localRotation = Quaternion.Lerp(
                    pointerObject.transform.localRotation,
                    targetRotation,
                    Time.deltaTime * settings.rotationSpeed
                );
                pointerObject.transform.localRotation = Quaternion.Lerp(
                 pointerObject.transform.localRotation,
                    targetRotation,
                    Time.deltaTime * settings.rotationSpeed
                );
            }
            else
            {
                pointerObject.transform.localRotation = targetRotation;
                pointerObject.transform.localRotation = targetRotation;
            }
        }

        #endregion

        #region Helpers
        // Set pointer color with alpha
        private void SetColor(Color32 color)
        {
            for (int i = 0; i < imagesToLerp.Count; i++)
            {
                imagesToLerp[i].color = color;
            }
        }
        #endregion

        #region TextBehaviour
        // Updates text position to ensure visibility within canvas boundaries
        private void UpdateTextPosition()
        {
            if (textElement == null) return;
            if (!textRect && textElement)
            {
                textRect = textElement.GetComponent<RectTransform>();
            }
            if (textRect == null || !textElement.enabled) return;

            textRect.anchoredPosition = initialTextOffset;
            textElement.alignment = initialAlignment;

            Vector3[] textCorners = new Vector3[4];
            textRect.GetWorldCorners(textCorners);

            for (int i = 0; i < 4; i++)
            {
                textCorners[i] = canvasRectTransform.InverseTransformPoint(textCorners[i]);
            }

            Vector2 canvasSize = canvasRectTransform.rect.size;
            float canvasHalfWidth = canvasSize.x * halfIndex;
            float canvasHalfHeight = canvasSize.y * halfIndex;

            float textLeft = textCorners.Min(c => c.x);
            float textRight = textCorners.Max(c => c.x);
            float textTop = textCorners.Max(c => c.y);
            float textBottom = textCorners.Min(c => c.y);

            bool needsRepositioning = false;
            Vector2 newOffset = initialTextOffset;
            TextAlignmentOptions newAlignment = initialAlignment;
            bool isVerticallyRepositioned = false;

            if (textTop > canvasHalfHeight)
            {
                newOffset.y = -Mathf.Abs(initialTextOffset.y);
                isVerticallyRepositioned = true;
                needsRepositioning = true;
            }
            else if (textBottom < -canvasHalfHeight)
            {
                newOffset.y = Mathf.Abs(initialTextOffset.y);
                isVerticallyRepositioned = true;
                needsRepositioning = true;
            }

            if (isVerticallyRepositioned)
            {
                newOffset.x = 0f;
                newAlignment = IsTopAligned(newAlignment) ? TextAlignmentOptions.Top :
                              IsBottomAligned(newAlignment) ? TextAlignmentOptions.Bottom :
                              TextAlignmentOptions.Center;
            }
            else if (textRight > canvasHalfWidth)
            {
                newOffset.x = -Mathf.Abs(initialTextOffset.x);
                if (IsRightAligned(initialAlignment))
                    newAlignment = ConvertToLeftAlignment(initialAlignment);
                else if (IsLeftAligned(initialAlignment))
                    newAlignment = ConvertToRightAlignment(initialAlignment);
                needsRepositioning = true;
            }
            else if (textLeft < -canvasHalfWidth)
            {
                newOffset.x = Mathf.Abs(initialTextOffset.x);
                if (IsRightAligned(initialAlignment))
                    newAlignment = ConvertToLeftAlignment(initialAlignment);
                else if (IsLeftAligned(initialAlignment))
                    newAlignment = ConvertToRightAlignment(initialAlignment);
                needsRepositioning = true;
            }

            if (needsRepositioning)
            {
                textRect.anchoredPosition = newOffset;
                textElement.alignment = newAlignment;
            }
        }

        // Checks if text alignment is right-aligned
        private bool IsRightAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Right ||
                   alignment == TextAlignmentOptions.TopRight ||
                   alignment == TextAlignmentOptions.BottomRight;
        }

        // Checks if text alignment is left-aligned
        private bool IsLeftAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Left ||
                   alignment == TextAlignmentOptions.TopLeft ||
                   alignment == TextAlignmentOptions.BottomLeft;
        }

        // Checks if text alignment is top-aligned
        private bool IsTopAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.TopLeft ||
                   alignment == TextAlignmentOptions.Top ||
                   alignment == TextAlignmentOptions.TopRight;
        }

        // Checks if text alignment is bottom-aligned
        private bool IsBottomAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.BottomLeft ||
                   alignment == TextAlignmentOptions.Bottom ||
                   alignment == TextAlignmentOptions.BottomRight;
        }

        // Converts alignment to left-aligned variant
        private TextAlignmentOptions ConvertToLeftAlignment(TextAlignmentOptions alignment)
        {
            if (IsTopAligned(alignment)) return TextAlignmentOptions.TopLeft;
            if (IsBottomAligned(alignment)) return TextAlignmentOptions.BottomLeft;
            return TextAlignmentOptions.Left;
        }

        // Converts alignment to right-aligned variant
        private TextAlignmentOptions ConvertToRightAlignment(TextAlignmentOptions alignment)
        {
            if (IsTopAligned(alignment)) return TextAlignmentOptions.TopRight;
            if (IsBottomAligned(alignment)) return TextAlignmentOptions.BottomRight;
            return TextAlignmentOptions.Right;
        }

        // Converts alignment to top-aligned variant
        private TextAlignmentOptions ConvertToTopAlignment(TextAlignmentOptions alignment)
        {
            if (IsLeftAligned(alignment)) return TextAlignmentOptions.TopLeft;
            if (IsRightAligned(alignment)) return TextAlignmentOptions.TopRight;
            return TextAlignmentOptions.Top;
        }

        // Converts alignment to bottom-aligned variant
        private TextAlignmentOptions ConvertToBottomAlignment(TextAlignmentOptions alignment)
        {
            if (IsLeftAligned(alignment)) return TextAlignmentOptions.BottomLeft;
            if (IsRightAligned(alignment)) return TextAlignmentOptions.BottomRight;
            return TextAlignmentOptions.Bottom;
        }

        #endregion
    }
}