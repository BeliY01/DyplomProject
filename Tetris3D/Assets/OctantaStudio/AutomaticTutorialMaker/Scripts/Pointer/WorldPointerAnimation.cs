using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static AutomaticTutorialMaker.WorldPointerSettings;

namespace AutomaticTutorialMaker
{
    public class WorldPointerAnimation : MonoBehaviour
    {
        #region Variables
        [ReadOnly] public bool isHover = false;
        private ClickData currentStep; // Current step data for the tutorial sequence
        private TutorialSceneReferences sceneReferences; // References to essential scene objects
        [HideInInspector] public bool destroyCommand; // Flag to destroy the pointer object

        [Header("Settings")]
        public WorldPointerSettings settings; // Configuration settings for the pointer behavior

        [Header("Components")]
        [SerializeField] private List<SpriteRenderer> spritesToLerp; // List of sprite renderers for animation
        [SerializeField] private List<MeshRenderer> meshesToLerp; // List of mesh renderers for animation
        [SerializeField] private TMP_Text textElement; // Text element for displaying instruction text
        public GameObject frameCornerPrefab;
        private GameObject pointerObject; // Main pointer game object

        private int raycastLayerIndex = 2; // Layer index used for raycasting operations

        [ReadOnly] public List<UnityEngine.Object> targetObjects = new List<UnityEngine.Object>(); // List of objects to point at
        private Dictionary<GameObject, CachedObjectData> objectCache = new Dictionary<GameObject, CachedObjectData>(); // Cache for object data optimization
        private int currentTargetIndex = 0; // Index of the current target object
        private float delayTimer = 0f; // Timer for tracking delays between movements
        private bool isWaitingAtPosition = false; // Whether pointer is waiting at current position
        private Vector3 levitationOffset = Vector3.zero; // Offset for levitation animation
        private bool isIdleInitialized = false; // Whether idle animation has been initialized
        private Vector3 lastPointerIdlePosition; // Last recorded idle position
        private Vector3 lastPointerIdleScale; // Last recorded idle scale

        private Vector2 targetPosition; // Target position in screen space
        private Vector3 targetWorldPosition; // Target position in world space
        private Vector3 targetRealPosition; // Actual target position with offsets
        private Vector3 currentVelocity; // Current movement velocity
        private Vector3 currentRotationVelocity; // Current rotation velocity

        private bool isAnimating = false; // Whether pointer is currently animating
        private bool isReversing = false; // Whether animation is playing in reverse
        private bool isIdleActive = false; // Whether idle animation is active
        private float animationProgress = 0f; // Progress of current animation
        private bool canPlayIdle = false; // Whether idle animation can be played
        private bool hasReachedTarget = false; // Whether pointer has reached target position

        private Vector3 initialScale; // Initial scale of the pointer
        private List<Color32> initialSpriteColors = new List<Color32>(); // Initial colors of sprite renderers
        private List<Material> initialMeshMaterials = new List<Material>(); // Initial materials of mesh renderers
        private float timer; // General purpose timer
        private bool isMoving = true; // Whether pointer is in motion
        private Vector3 currentScaleVelocity; // Current scale change velocity
        private Quaternion initialTargetRotation; // Initial rotation of the target
        private float pointerLevitationTime = 0f; // Time tracker for levitation animation
        private float pointerPulseTime = 0f; // Time tracker for pulse animation
        private float blinkTimer; // Time tracker for blink animation
        private bool isObjectVisibleState = true; // Current visibility state
        private bool lastObjectState = true; // Previous visibility state

        private Color32 initialTextColor; // Initial color of the text element
        private Vector3 initialTextScale; // Initial scale of the text element
        private Vector3 initialTextLocalOffset; // Initial local position offset of text
        private Vector3 initialTextForward; // Initial forward direction of text
        private TextAlignmentOptions initialAlignment; // Initial text alignment option
        private bool isTextInitialized = false; // Whether text has been initialized

        private bool isOffScreen; // Whether pointer is currently off screen

        private float durationIndex = 1.05f;
        private float levitationSin = 1;
        private float almostOne = 0.99f;
        private int cornersCount = 4;
        private float halfIndex = 0.5f;
        private float doubleIndex = 2;
        private float turnDegrees = 180;
        private float finalRotation = 360f;

        // frame
        private GameObject[] corners = new GameObject[4];
        private Vector3 targetPointerPosition;

        private class CachedObjectData
        {
            public RectTransform rectTransform; // Cached RectTransform component
            public Renderer mainRenderer; // Cached main renderer component
            public Renderer[] childRenderers; // Cached array of child renderers
            public bool isUI; // Whether object is a UI element
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
        // Core initialization method that sets up the pointer with given step data and scene references
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
                settings = sceneReferences.NoneWorldPointerSettings;
            }

            // Save initial values
            initialScale = pointerObject.transform.localScale;

            GraphicsOptimization();

            TextProcessing();

            ReassignColors();

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
        private void SetupInitialState(WorldAnimationType animationType)
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
            if (isHover)
            {
                targetObj = targetObjects[targetObjects.Count - 1] as GameObject;
                if (targetObj != null)
                {
                    UpdatePointerTargetPosition(targetObj, ObjectTypeEnum.None);
                }
            }

            if (settings.pointerMode != PointerMode.RotateOnly)
            {
                SetInitialTargetPosition();
            }

            SetInitialTargetRotation();
            initialTargetRotation = pointerObject.transform.rotation;

            switch (animationType)
            {
                case WorldAnimationType.Zoom:
                case WorldAnimationType.ZoomAndRotate:
                    pointerObject.transform.localScale = isReversing ? initialScale : Vector3.zero;
                    break;

                case WorldAnimationType.Fade:
                    if (spritesToLerp != null && spritesToLerp.Count > 0)
                    {
                        foreach (var sprite in spritesToLerp)
                        {
                            if (sprite != null && sprite.gameObject.activeSelf)
                            {
                                Color spriteColor = sprite.color;
                                sprite.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, 0f);
                            }
                        }
                    }
                    if (meshesToLerp != null && meshesToLerp.Count > 0)
                    {
                        foreach (var mesh in meshesToLerp)
                        {
                            if (mesh != null && mesh.gameObject.activeSelf)
                            {
                                if (settings.transparentMaterial != null)
                                {
                                    mesh.material = settings.transparentMaterial;
                                }
                            }
                        }
                    }

                    if (textElement)
                    {
                        textElement.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, 0f);
                    }
                    break;

                case WorldAnimationType.None:
                    break;
            }

            if (!isReversing)
            {
                gameObject.SetActive(true);
            }
        }

        // Sets initial target position on startup
        private void SetInitialTargetPosition()
        {
            if (settings.pointerMode == PointerMode.RotateOnly)
                return;

            targetPointerPosition = targetWorldPosition;

            switch (settings.pointerMode)
            {
                case PointerMode.PositionOnly:
                    break;

                case PointerMode.Geotag:

                    UpdateGeotagPosition();
                    targetPointerPosition = targetRealPosition;
                    break;

                case PointerMode.Facade:
                    UpdateFacadePosition();
                    targetPointerPosition = targetRealPosition;
                    break;

                case PointerMode.RoadSign:
                    UpdateRoadSignPosition();
                    targetPointerPosition = targetRealPosition;
                    break;

                case PointerMode.Side:
                    UpdateSidePosition();
                    targetPointerPosition = targetRealPosition;
                    break;

                case PointerMode.Ground:
                    UpdateGroundPosition();
                    targetPointerPosition = targetRealPosition;
                    break;
                case PointerMode.Frame:
                    if (frameCornerPrefab == null)
                    {
                        Debug.LogError("[ATM] Frame corner prefab is missing.");
                        break;
                    }

                    if (!objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var frameCachedData))
                    {
                        Debug.LogWarning("[ATM] Target object not found in cache for frame mode.");
                        break;
                    }

                    bool is2D = frameCornerPrefab.TryGetComponent<SpriteRenderer>(out _);
                    InitializeCorners(is2D);
                    ReassignColors();

                    ReFrame(frameCachedData);

                    break;
            }

            pointerObject.transform.position = targetPointerPosition;
            lastPointerIdlePosition = targetPointerPosition;
        }

        // Optimizes graphics components and sets up rendering layers
        private void GraphicsOptimization()
        {
            if (spritesToLerp != null && spritesToLerp.Count > 0)
            {
                foreach (var sprite in spritesToLerp)
                {
                    if (sprite != null && sprite.gameObject.activeSelf)
                    {
                        sprite.gameObject.layer = raycastLayerIndex;

                        if (sprite.TryGetComponent(out Collider2D collider2D))
                        {
                            Destroy(collider2D);
                        }

                        if (sprite.TryGetComponent(out Collider collider3D))
                        {
                            Destroy(collider3D);
                        }
                    }
                }
            }

            if (meshesToLerp != null && meshesToLerp.Count > 0)
            {
                foreach (var mesh in meshesToLerp)
                {
                    if (mesh != null && mesh.gameObject.activeSelf)
                    {
                        mesh.gameObject.layer = raycastLayerIndex;
                        mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        if (mesh.TryGetComponent(out Collider collider))
                        {
                            Destroy(collider);
                        }
                    }
                }
            }

            if (textElement)
            {
                textElement.gameObject.layer = raycastLayerIndex;
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
                textTip = currentStep.localizationReference.WorldPointerText;
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                initialTextScale = textElement.transform.localScale;
                initialAlignment = textElement.alignment;
                initialTextLocalOffset = textElement.transform.localPosition;
                initialTextForward = textElement.transform.forward;
                isTextInitialized = true;

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
            initialSpriteColors.Clear();
            if (spritesToLerp != null)
            {
                foreach (var sprite in spritesToLerp)
                {
                    if (sprite != null)
                    {
                        initialSpriteColors.Add(sprite.color);
                    }
                }
            }

            // Save initial mesh materials
            initialMeshMaterials.Clear();
            if (meshesToLerp != null)
            {
                foreach (var mesh in meshesToLerp)
                {
                    if (mesh != null && mesh.material != null)
                    {
                        initialMeshMaterials.Add(new Material(mesh.material));
                    }
                }
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                initialTextColor = textElement.color;
            }
        }

        private void ReFrame(WorldPointerAnimation.CachedObjectData frameCachedData)
        {
            Debug.Log("[ATM] Reframe");
            Vector3 center = Vector3.zero;
            if (frameCachedData.isUI && frameCachedData.rectTransform != null)
            {
                Debug.LogError("[ATM] WorldFrame for UI objects is not supported yet.");
                // center = HandleUIFrame(frameCachedData);
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i].SetActive(false);
                }
            }
            else if (frameCachedData.mainRenderer != null || (frameCachedData.childRenderers?.Length > 0))
            {
                center = HandleRendererFrame(frameCachedData);
            }

            ApplyCornerRotation(pointerObject.transform.rotation);
            
                targetPointerPosition = center;
            
        }


        // Resets pointer state to initial values
        private void ResetPointerState()
        {
            currentTargetIndex = 0;
            delayTimer = 0f;
            isWaitingAtPosition = false;
            levitationOffset = Vector3.zero;
            isIdleInitialized = false;

            if (targetObjects != null && targetObjects.Count > 0)
            {
                GameObject targetObj = targetObjects[0] as GameObject;
                if (targetObj != null)
                {
                    ObjectTypeEnum targetType = currentStep?.objectTypes != null &&
                            currentStep.objectTypes.Count > currentTargetIndex ?
                            currentStep.objectTypes[currentTargetIndex] :
                            ObjectTypeEnum.None;
                    UpdatePointerTargetPosition(targetObj, targetType);

                    lastPointerIdlePosition = pointerObject.transform.position;
                }
            }
        }

        #endregion

        #region Update
        // Updates animation state and handles pointer behavior each frame
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
                        case WorldAnimationType.None:
                        case WorldAnimationType.Fade:
                            hasReachedTarget = true;
                            break;
                        default:
                            if (!isAnimating || animationProgress >= settings.appearDuration * durationIndex)
                            {
                                hasReachedTarget = true;
                            }
                            else
                            {
                                hasReachedTarget = false;
                            }
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
            }

            if (!isReversing && !hasReachedTarget && settings.appearAnimation == WorldAnimationType.ZoomAndRotate)
            {
            
            }
            else
            {
                UpdatePointerRotation();
            }
            UpdateTextOrientation();
            UpdateTextPosition();
            UpdateTextVisibility();
        }

        #endregion

        #region Animations

        // Initiates appear animation sequence
        public void StartAnimation()
        {

            isAnimating = true;
            isReversing = false;
            isIdleActive = true;
            animationProgress = 0f;
            pointerLevitationTime = 0f;
            pointerPulseTime = 0f;
            canPlayIdle = false;
            hasReachedTarget = false;

            SetupInitialState(settings.appearAnimation);
            if (settings.appearAnimation == WorldAnimationType.None)
            {
                gameObject.SetActive(true);
                isAnimating = false;
                canPlayIdle = true;
                hasReachedTarget = true;
            }
            isIdleInitialized = false;
            Debug.Log("[ATM] UI Pointer initialized. Local Scale: " + pointerObject.transform.localScale + "; Local Position: " + pointerObject.transform.localPosition);

        }

        // Handles idle animation behavior based on settings
        private void PlayIdleAnimation()
        {
            if (!canPlayIdle || currentStep == null || settings == null) return;

            if (!isIdleInitialized)
            {
                lastPointerIdlePosition = pointerObject.transform.localPosition;
                lastPointerIdleScale = pointerObject.transform.localScale;
                levitationOffset = Vector3.zero;
                pointerLevitationTime = 0f;
                pointerPulseTime = 0f;
                isIdleInitialized = true;
            }

            if (settings.pointerIdleAnimation == IdleWorldAnimationType.Levitate)
            {
                if (!hasReachedTarget) return;
                if (currentStep.interaction != InteractionTypeEnum.DragAndDrop)
                {
                    pointerLevitationTime += Time.deltaTime * settings.levitationSpeed;
                    float progress = (Mathf.Sin(pointerLevitationTime) + levitationSin) * halfIndex;

                    Vector3 minOffset = -settings.levitationDirection.normalized *
                                      settings.levitationRange;
                    Vector3 maxOffset = settings.levitationDirection.normalized *
                                      settings.levitationRange;

                    levitationOffset = Vector3.Lerp(minOffset, maxOffset, progress);

                    pointerObject.transform.localPosition = lastPointerIdlePosition + levitationOffset;
                }
            }
            else if (settings.pointerIdleAnimation == IdleWorldAnimationType.Pulse)
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
            else if (settings.pointerIdleAnimation == IdleWorldAnimationType.Blink && !isOffScreen)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= settings.blinkInterval)
                {
                    blinkTimer = 0f;
                    isObjectVisibleState = !isObjectVisibleState;
                    TemporarilyTurnOff();
                }
            }
            else if (settings.pointerIdleAnimation == IdleWorldAnimationType.Orbit)
            {
                pointerLevitationTime += Time.deltaTime * settings.orbitSpeed;
                float angle = pointerLevitationTime * Mathf.Deg2Rad;

                Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, settings.orbitAxis);

                Vector3 orbitOffset = rotation * (Vector3.right * settings.orbitRadius);

                levitationOffset = orbitOffset;
            }
            else if (settings.pointerIdleAnimation == IdleWorldAnimationType.Bounce)
            {
                if (!hasReachedTarget) return;

                pointerLevitationTime += Time.deltaTime * settings.bounceSpeed;
                float bounceOffset = Mathf.Abs(Mathf.Sin(pointerLevitationTime)) * settings.bounceHeight;

                levitationOffset = Vector3.up * bounceOffset;
            }
            else if (settings.pointerIdleAnimation == IdleWorldAnimationType.RotateSpin && !isOffScreen)
            {
                pointerLevitationTime += Time.deltaTime * settings.spinSpeed;
                float spinRotation = pointerLevitationTime;

                pointerObject.transform.localRotation = initialTargetRotation *
                    Quaternion.AngleAxis(spinRotation, settings.spinAxis.normalized);
            }
        }

        // Performs animation interpolation
        private void Animate()
        {
            float duration = isReversing ?
                settings.disappearDuration :
                settings.appearDuration;

            float t = Mathf.Clamp01(animationProgress / duration);

            WorldAnimationType currentAnimation = isReversing ?
                settings.disappearAnimation :
                settings.appearAnimation;

            if (currentAnimation == WorldAnimationType.Zoom)
            {
                Vector3 currentScale = pointerObject.transform.localScale;
                Vector3 targetScale = isReversing ? Vector3.zero : initialScale;
                pointerObject.transform.localScale = Vector3.Lerp(
                    isReversing ? initialScale : Vector3.zero,
                    targetScale,
                    t
                );
            }
            else if (currentAnimation == WorldAnimationType.ZoomAndRotate)
            {
                Vector3 currentScale = pointerObject.transform.localScale;
                Vector3 targetScale = isReversing ? Vector3.zero : initialScale;
                pointerObject.transform.localScale = Vector3.Lerp(
                    isReversing ? initialScale : Vector3.zero,
                    targetScale,
                    t
                );
                float totalRotation = settings.spinRevolutions * finalRotation;
                float rotationAngle = Mathf.Lerp(0, totalRotation, t);
                pointerObject.transform.rotation = initialTargetRotation * Quaternion.AngleAxis(rotationAngle, settings.spinAxis.normalized);

            }
            else if (currentAnimation == WorldAnimationType.Fade)
            {

                for (int i = 0; i < spritesToLerp.Count; i++)
                {
                    if (spritesToLerp[i] != null && spritesToLerp[i].gameObject.activeSelf)
                    {
                        Color currentColor = spritesToLerp[i].color;

                        if (!isReversing)
                        {
                            Color32 startColor = new Color32(initialSpriteColors[i].r, initialSpriteColors[i].g, initialSpriteColors[i].b, 0);
                            spritesToLerp[i].color = Color32.Lerp(startColor, initialSpriteColors[i], t);
                            if (t >= almostOne)
                            {
                                spritesToLerp[i].color = initialSpriteColors[i];
                            }
                        }
                        else
                        {
                            Color32 targetColor = new Color(
                                currentColor.r,
                                currentColor.g,
                                currentColor.b,
                                0f
                            );
                            Color32 startColor = new Color32(initialSpriteColors[i].r, initialSpriteColors[i].g, initialSpriteColors[i].b, 0);

                            spritesToLerp[i].color = Color32.Lerp(
                              isReversing ? initialSpriteColors[i] : startColor,
                              targetColor,
                              t
                          );
                        }
                    }
                }

                for (int i = 0; i < meshesToLerp.Count; i++)
                {
                    if (meshesToLerp[i] != null && meshesToLerp[i].gameObject.activeSelf)
                    {
                        if (!isReversing)
                        {
                            meshesToLerp[i].material.Lerp(settings.transparentMaterial, initialMeshMaterials[i], t);
                            if (t >= almostOne)
                            {
                                meshesToLerp[i].material = new Material(initialMeshMaterials[i]);
                            }
                        }
                        else
                        {
                            meshesToLerp[i].material.Lerp(initialMeshMaterials[i], settings.transparentMaterial, t);
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

            animationProgress += Time.deltaTime;
            if (animationProgress >= duration)
            {
                isAnimating = false;
                if (destroyCommand)
                {
                    Destroy(pointerObject);
                }
            }

        }

        // Initiates disappear animation sequence
        public void FinishAnimation()
        {
            isAnimating = true;
            isReversing = true;
            isIdleActive = false;
            animationProgress = 0f;

            if (settings.disappearAnimation == WorldAnimationType.None)
            {
                isAnimating = false;
                Destroy(gameObject);
            }
            else
            {
                destroyCommand = true;
            }
        }
        #endregion

        #region Targets

        // Updates the list of objects that pointer should target
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

        // Caches object data for performance optimization
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

        // Updates pointer target position based on object type
        private void UpdatePointerTargetPosition(GameObject targetObj, ObjectTypeEnum targetType)
        {
            if (targetType == ObjectTypeEnum.None)
            {
                targetType = sceneReferences.tutorialMaker.GetObjectType(targetObj);
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

        // Calculates target world position considering object bounds
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

        // Handles UI target positioning
        private void HandleUITarget(GameObject targetObj)
        {
            if (!objectCache.TryGetValue(targetObj, out var cachedData) || !cachedData.isUI)
                return;

            Vector3[] corners = new Vector3[cornersCount];
            cachedData.rectTransform.GetLocalCorners(corners);
            int cornersHalf = (int)(cornersCount * halfIndex);
            Vector2 localPoint = (corners[0] + corners[cornersHalf]) / cornersHalf;

            Camera canvasCamera = sceneReferences.targetCanvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                cachedData.rectTransform.TransformPoint(localPoint)
            );

            Ray ray = sceneReferences.mainCamera.ScreenPointToRay(screenPoint);
            targetWorldPosition = ray.GetPoint(settings.behindUiDistance);
        }

        // Handles 2D/3D target positioning
        private void Handle3D2DTarget(GameObject targetObj)
        {
            if (!objectCache.TryGetValue(targetObj, out var cachedData) || cachedData.isUI)
                return;

            targetWorldPosition = GetTargetWorldPosition(targetObj, cachedData);
        }

        // Updates current target position each frame
        private void UpdateTargetPosition()
        {
            if (targetObjects == null || targetObjects.Count == 0 ||
                (targetObjects.Count > 0 && !targetObjects[currentTargetIndex]))
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
            if (!isWaitingAtPosition &&
                Vector3.Distance(pointerObject.transform.position, targetRealPosition) < settings.positionThreshold)
            {
                isWaitingAtPosition = true;
                delayTimer = 0f;
            }
            if (!isWaitingAtPosition && settings.pointerMode == PointerMode.RotateOnly)
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

                if (delayTimer >= currentDelay && !isOffScreen)
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

                ObjectTypeEnum targetType = currentStep?.objectTypes != null &&
                    currentStep.objectTypes.Count > temporalIndex ?
                    currentStep.objectTypes[temporalIndex] :
                    ObjectTypeEnum.None;
                UpdatePointerTargetPosition(targetObj, targetType);
            }
        }
        #endregion

        #region Movement

        // Updates pointer movement based on current mode
        private void UpdatePointerMovement()
        {
            if (isOffScreen && !IsTargetOffScreen())
            {
                isOffScreen = false;
            }
            else
            {
                isOffScreen = IsPointerOffScreen();
            }

            if (isOffScreen)
            {
                HandleOffscreenPointer();
            }
            else
            {
                switch (settings.pointerMode)
                {
                    case PointerMode.PositionOnly:
                        UpdateNonePosition();
                        break;
                    case PointerMode.Geotag:
                        UpdateGeotagPosition();
                        break;
                    case PointerMode.RoadSign:
                        UpdateRoadSignPosition();
                        break;
                    case PointerMode.Facade:
                        UpdateFacadePosition();
                        break;
                    case PointerMode.Side:
                        UpdateSidePosition();
                        break;
                    case PointerMode.Ground:
                        UpdateGroundPosition();
                        break;
                    case PointerMode.Frame:
                        UpdateFramePosition();
                        break;
                    case PointerMode.RotateOnly:

                        break;
                }
            }
            if (settings.pointerIdleAnimation == IdleWorldAnimationType.Levitate)
            {
                lastPointerIdlePosition = pointerObject.transform.position - levitationOffset;
            }
            if (settings.pointerIdleAnimation == IdleWorldAnimationType.Orbit ||
        settings.pointerIdleAnimation == IdleWorldAnimationType.Bounce)
            {
                pointerObject.transform.position += levitationOffset;
                lastPointerIdlePosition = pointerObject.transform.position - levitationOffset;
            }
        }

        // Smoothly updates position to target
        private void UpdatePosition(Vector3 targetPos)
        {
            pointerObject.transform.position = Vector3.SmoothDamp(
             pointerObject.transform.position,
              targetPos,
              ref currentVelocity,
              settings.smoothTime,
              settings.moveSpeed
          );
        }

        // Various position update methods for different pointer modes
        private void UpdateNonePosition()
        {
            targetRealPosition = targetWorldPosition;
            UpdatePosition(targetRealPosition);
        }

        private void UpdateFramePosition()
        {
            targetRealPosition = targetWorldPosition;
            UpdatePosition(targetRealPosition);
        }


        private void UpdateGeotagPosition()
        {
            if (!objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData))
                return;

            Vector3 offsetPosition = targetWorldPosition;
            if (cachedData.mainRenderer != null)
            {
                float objectHeight = cachedData.mainRenderer.bounds.size.y;
                offsetPosition += new Vector3(0, objectHeight, 0) + settings.geotagOffset;
            }
            else if (cachedData.childRenderers != null && cachedData.childRenderers.Length > 0)
            {
                Bounds combinedBounds = cachedData.childRenderers[0].bounds;
                for (int i = 1; i < cachedData.childRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(cachedData.childRenderers[i].bounds);
                }
                offsetPosition += new Vector3(0, combinedBounds.size.y, 0) + settings.geotagOffset;
            }
            else
            {
                offsetPosition += settings.geotagOffset;
            }

            targetRealPosition = offsetPosition;
            UpdatePosition(targetRealPosition);
        }

        private void UpdateFacadePosition()
        {
            if (sceneReferences == null || sceneReferences.mainCamera == null)
                return;


            Vector3 offset = settings.facadeOffset;
            offset.z = 0;
            Vector3 cameraPosition = sceneReferences.mainCamera.transform.position;
            Vector3 directionFromCamera = (targetWorldPosition - cameraPosition).normalized;
            targetRealPosition = targetWorldPosition - (directionFromCamera * settings.facadeOffset.z) + offset;
            UpdatePosition(targetRealPosition);
        }

        private void UpdateRoadSignPosition()
        {
            if (sceneReferences == null || sceneReferences.mainCamera == null)
                return;

            Vector3 cameraPosition = sceneReferences.mainCamera.transform.position;
            Vector3 cameraForward = sceneReferences.mainCamera.transform.forward;
            Vector3 basePosition = cameraPosition + cameraForward * settings.roadSignOffset.z;

            if (settings.roadSignFixedFrontCamera)
            {
                targetRealPosition = basePosition + new Vector3(settings.roadSignOffset.x, settings.roadSignOffset.y, 0);
            }
            else
            {
                Vector3 directionToTarget = (targetWorldPosition - cameraPosition).normalized;
                float distanceToTarget = Vector3.Distance(cameraPosition, targetWorldPosition);

                Vector3 targetRealPositionClamped = cameraPosition + directionToTarget * (distanceToTarget * 0.5f) + new Vector3(settings.roadSignOffset.x, settings.roadSignOffset.y, 0);
                targetRealPositionClamped.z = Mathf.Min(targetRealPosition.z, cameraPosition.z + settings.roadSignOffset.z);
                targetRealPosition = targetRealPositionClamped;
            }

            UpdatePosition(targetRealPosition);
        }

        private void UpdateSidePosition()
        {
            if (!objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData))
                return;

            Vector3 offsetPosition = targetWorldPosition + settings.sideOffset;
            targetRealPosition = offsetPosition;
            UpdatePosition(targetRealPosition);
        }

        private void UpdateGroundPosition()
        {
            if (!objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData))
                return;

            Vector3 offsetPosition = targetWorldPosition;

            if (cachedData.mainRenderer != null)
            {
                float objectHeight = cachedData.mainRenderer.bounds.size.y;
                offsetPosition -= Vector3.up * (objectHeight * halfIndex);
            }
            else if (cachedData.childRenderers != null && cachedData.childRenderers.Length > 0)
            {
                Bounds combinedBounds = cachedData.childRenderers[0].bounds;
                for (int i = 1; i < cachedData.childRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(cachedData.childRenderers[i].bounds);
                }
                float objectHeight = combinedBounds.size.y;
                offsetPosition -= Vector3.up * (objectHeight * halfIndex);
            }
            targetRealPosition = offsetPosition + settings.groundOffset;

            UpdatePosition(targetRealPosition);
        }


        // Checks if pointer is off screen
        private bool IsPointerOffScreen()
        {
            if (settings.defaultEdgeBehaviour == EdgeBehaviour.None)
            {
                return false;
            }

            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(pointerObject.transform.position);
            float padding = settings.screenEdgePadding;
            return screenPoint.z < 0 ||
                   screenPoint.x < padding ||
                   screenPoint.x > Screen.width - padding ||
                   screenPoint.y < padding ||
                   screenPoint.y > Screen.height - padding;
        }

        // Checks if target is off screen
        private bool IsTargetOffScreen()
        {
            GameObject currentTarget = targetObjects[currentTargetIndex] as GameObject;
            if (currentTarget == null) return true;

            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(GetTargetWorldPosition(currentTarget, objectCache[currentTarget]));
            float padding = settings.screenEdgePadding;

            return screenPoint.z < 0 ||
                   screenPoint.x < padding ||
                   screenPoint.x > Screen.width - padding ||
                   screenPoint.y < padding ||
                   screenPoint.y > Screen.height - padding;
        }

        // Handles pointer behavior when off screen
        private void HandleOffscreenPointer()
        {
            if (settings.pointerMode == PointerMode.RoadSign && settings.roadSignFixedFrontCamera)
            {
                return;
            }
            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(targetWorldPosition);
            bool isBehind = screenPoint.z < 0;

            if (isBehind)
            {
                screenPoint *= -1;
            }

            Vector2 screenCenter = new Vector2(Screen.width * halfIndex, Screen.height * halfIndex);
            Vector2 direction = (new Vector2(screenPoint.x, screenPoint.y) - screenCenter).normalized;
            float padding = settings.screenEdgePadding;

            float screenWidth = Screen.width - padding * doubleIndex;
            float screenHeight = Screen.height - padding * doubleIndex;

            float xEdge = direction.x > 0 ? screenWidth + padding : padding;
            float yEdge = direction.y > 0 ? screenHeight + padding : padding;

            float xIntersect = direction.x != 0 ? (xEdge - screenCenter.x) / direction.x : float.MaxValue;
            float yIntersect = direction.y != 0 ? (yEdge - screenCenter.y) / direction.y : float.MaxValue;

            Vector2 edgePoint;
            if (Mathf.Abs(xIntersect) < Mathf.Abs(yIntersect))
            {
                edgePoint = new Vector2(xEdge, screenCenter.y + direction.y * xIntersect);
            }
            else
            {
                edgePoint = new Vector2(screenCenter.x + direction.x * yIntersect, yEdge);
            }

            Vector3 targetScreenPoint = sceneReferences.mainCamera.WorldToScreenPoint(targetWorldPosition);
            edgePoint.x = Mathf.Clamp(targetScreenPoint.x, padding, Screen.width - padding);
            edgePoint.y = Mathf.Clamp(targetScreenPoint.y, padding, Screen.height - padding);

            Ray ray = sceneReferences.mainCamera.ScreenPointToRay(edgePoint);
            float distanceToScreen = Vector3.Distance(sceneReferences.mainCamera.transform.position, targetWorldPosition);

            targetRealPosition = ray.GetPoint(distanceToScreen);

            UpdatePosition(targetRealPosition);
        }
        #endregion

        #region Rotation

        // Updates pointer rotation based on current mode
        private void UpdatePointerRotation()
        {
            if (pointerObject == null || sceneReferences == null || sceneReferences.mainCamera == null)
                return;

            if (isOffScreen)
            {
                if (settings.defaultEdgeBehaviour == EdgeBehaviour.DisableOnExit)
                {
                    isObjectVisibleState = false;
                    TemporarilyTurnOff();
                    return;
                }
                else if (settings.pointerMode == PointerMode.Geotag || settings.pointerMode == PointerMode.Facade)
                {
                    UpdateTargetOnlyRotation();
                    return;
                }
            }
            else
            {
                if (!isObjectVisibleState && settings.pointerIdleAnimation != IdleWorldAnimationType.Blink)
                {
                    isObjectVisibleState = true;
                }
            }

            TemporarilyTurnOff();

            switch (settings.pointerMode)
            {
                case PointerMode.PositionOnly:
                    UpdateCameraOnlyRotation();
                    break;
                case PointerMode.Geotag:
                case PointerMode.Facade:
                    UpdateGeotagRotation();
                    break;
                case PointerMode.Side:
                    UpdateSideRotation();
                    break;
                case PointerMode.Ground:
                    break;
                case PointerMode.Frame:
                    UpdateFrameRotation();
                    break;
                case PointerMode.RoadSign:
                    UpdateRoadSignRotation();
                    break;
                case PointerMode.RotateOnly:
                    UpdateTargetOnlyRotation();
                    break;
            }
        }

        // Various rotation update methods for different pointer modes
        private void UpdateCameraOnlyRotation()
        {
            if (settings.faceToCamera && settings.pointerIdleAnimation != IdleWorldAnimationType.RotateSpin)
            {
                Vector3 camForward = sceneReferences.mainCamera.transform.forward;
                Vector3 camRight = sceneReferences.mainCamera.transform.right;
                Vector3 up = Vector3.Cross(camForward, camRight);
                pointerObject.transform.rotation = Quaternion.LookRotation(-camForward, up);
                pointerObject.transform.Rotate(Vector3.up, turnDegrees);
            }
        }

        private void NoCameraRotation()
        {
            Vector3 directionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);
            pointerObject.transform.rotation = Quaternion.RotateTowards(
                pointerObject.transform.rotation,
                targetRotation,
                settings.rotationSpeed * Time.deltaTime
            );
        }

        private void UpdateTargetOnlyRotation()
        {
            if (settings.faceToCamera)
            {
                Vector3 directionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);
                Vector3 directionToCamera = (sceneReferences.mainCamera.transform.position - pointerObject.transform.position).normalized;
                Quaternion lookAtCamera = Quaternion.LookRotation(-directionToCamera, targetRotation * Vector3.up);
                Quaternion finalRotation = Quaternion.Lerp(targetRotation, lookAtCamera, halfIndex);

                pointerObject.transform.rotation = Quaternion.RotateTowards(
                    pointerObject.transform.rotation,
                    finalRotation,
                    settings.rotationSpeed * Time.deltaTime
                );
            }
            else if (settings.pointerIdleAnimation != IdleWorldAnimationType.RotateSpin)
            {
                NoCameraRotation();
            }
        }

        private void UpdateGeotagRotation()
        {
            if (objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData) && cachedData.isUI)
            {
                pointerObject.transform.rotation = Quaternion.Euler(
 pointerObject.transform.rotation.eulerAngles.x,
 sceneReferences.mainCamera.transform.rotation.eulerAngles.y,
 pointerObject.transform.rotation.eulerAngles.z
);
            }
            else if (settings.pointerIdleAnimation != IdleWorldAnimationType.RotateSpin)
            {
                if (settings.faceToCamera)
                {
                    Vector3 camForward = sceneReferences.mainCamera.transform.forward;
                    Vector3 camRight = sceneReferences.mainCamera.transform.right;
                    Vector3 up = Vector3.Cross(camForward, camRight);
                    pointerObject.transform.rotation = Quaternion.LookRotation(-camForward, up);
                    pointerObject.transform.Rotate(Vector3.up, turnDegrees);
                }
                else
                {
                    NoCameraRotation();
                }
            }
        }

        private void UpdateFrameRotation()
        {
            if (objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData) && cachedData.isUI)
            {
                pointerObject.transform.rotation = sceneReferences.mainCamera.transform.rotation;
            }
            else
            {
                GameObject currentTarget = targetObjects[currentTargetIndex] as GameObject;
                Quaternion baseRotation = settings.faceToCamera && sceneReferences?.mainCamera != null
               ? sceneReferences.mainCamera.transform.rotation
               : currentTarget.transform.rotation;

                if (settings.pointerIdleAnimation != IdleWorldAnimationType.RotateSpin)
                {
                    pointerObject.transform.rotation = baseRotation;
                }
            }
        }

        private void UpdateSideRotation()
        {
            if (objectCache.TryGetValue(targetObjects[currentTargetIndex] as GameObject, out var cachedData) && cachedData.isUI)
            {
                pointerObject.transform.rotation = Quaternion.Euler(
 sceneReferences.mainCamera.transform.rotation.eulerAngles.x,
sceneReferences.mainCamera.transform.rotation.eulerAngles.y,
 pointerObject.transform.rotation.eulerAngles.z
);

            }
            else if (settings.pointerIdleAnimation != IdleWorldAnimationType.RotateSpin)
            {
                if (settings.faceToCamera)
                {
                    Vector3 directionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;
                    Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);
                    Vector3 directionToCamera = (sceneReferences.mainCamera.transform.position - pointerObject.transform.position).normalized;
                    Quaternion lookAtCamera = Quaternion.LookRotation(-directionToCamera, targetRotation * Vector3.up);
                    Quaternion finalRotation = Quaternion.Lerp(targetRotation, lookAtCamera, halfIndex);
                    pointerObject.transform.rotation = Quaternion.RotateTowards(
                        pointerObject.transform.rotation,
                        finalRotation,
                        settings.rotationSpeed * Time.deltaTime
                    );
                }

                else
                {
                    NoCameraRotation();
                }
            }
        }

        private void UpdateRoadSignRotation()
        {
            if (pointerObject == null || sceneReferences == null || sceneReferences.mainCamera == null)
                return;

            Vector3 directionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;

            Vector3 cameraForward = sceneReferences.mainCamera.transform.forward;
            Vector3 cameraRight = sceneReferences.mainCamera.transform.right;
            Vector3 cameraUp = sceneReferences.mainCamera.transform.up;

            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);

            Quaternion tiltX = Quaternion.AngleAxis(settings.roadTiltAngles.x, cameraRight);
            Quaternion tiltY = Quaternion.AngleAxis(settings.roadTiltAngles.y, cameraUp);
            Quaternion tiltZ = Quaternion.AngleAxis(settings.roadTiltAngles.z, cameraForward);

            Quaternion targetRotation = tiltZ * tiltY * tiltX * baseRotation;

            pointerObject.transform.rotation = Quaternion.RotateTowards(
                pointerObject.transform.rotation,
                targetRotation,
                settings.rotationSpeed * Time.deltaTime
            );
        }

        // Sets initial target rotation on startup
        private void SetInitialTargetRotation()
        {
            if (sceneReferences?.mainCamera == null)
                return;

            switch (settings.pointerMode)
            {
                case PointerMode.PositionOnly:
                    break;
                case PointerMode.Frame:
                case PointerMode.Geotag:
                case PointerMode.Facade:
                    if (settings.faceToCamera)
                    {
                        Vector3 directionToCamera = (sceneReferences.mainCamera.transform.position - pointerObject.transform.position).normalized;
                        Vector3 upVector = sceneReferences.mainCamera.transform.up;
                        pointerObject.transform.rotation = Quaternion.LookRotation(-directionToCamera, upVector);
                    }
                    else
                    {
                        Vector3 direction2 = (targetWorldPosition - pointerObject.transform.position).normalized;
                        pointerObject.transform.rotation = Quaternion.FromToRotation(Vector3.down, direction2);
                    }
                    break;

                case PointerMode.RoadSign:

                    Vector3 directionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;
                    Vector3 cameraForward = sceneReferences.mainCamera.transform.forward;
                    Vector3 cameraRight = sceneReferences.mainCamera.transform.right;
                    Vector3 cameraUp = sceneReferences.mainCamera.transform.up;

                    Quaternion baseRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);

                    Quaternion tiltX = Quaternion.AngleAxis(settings.roadTiltAngles.x, cameraRight);
                    Quaternion tiltY = Quaternion.AngleAxis(settings.roadTiltAngles.y, cameraUp);
                    Quaternion tiltZ = Quaternion.AngleAxis(settings.roadTiltAngles.z, cameraForward);

                    pointerObject.transform.rotation = tiltZ * tiltY * tiltX * baseRotation;

                    break;

                case PointerMode.Side:
                    if (settings.faceToCamera)
                    {
                        Vector3 sideDirectionToTarget = (targetWorldPosition - pointerObject.transform.position).normalized;
                        Vector3 sideCameraForward = sceneReferences.mainCamera.transform.forward;
                        Vector3 projectedDirection = Vector3.ProjectOnPlane(sideDirectionToTarget, sideCameraForward).normalized;
                        pointerObject.transform.rotation = Quaternion.FromToRotation(Vector3.down, projectedDirection);
                    }
                    else
                    {
                        Vector3 direction2 = (targetWorldPosition - pointerObject.transform.position).normalized;
                        pointerObject.transform.rotation = Quaternion.FromToRotation(Vector3.down, direction2);
                    }
                    break;

                case PointerMode.Ground:
                    break;

                case PointerMode.RotateOnly:
                    Vector3 direction = (targetWorldPosition - pointerObject.transform.position).normalized;
                    pointerObject.transform.rotation = Quaternion.FromToRotation(Vector3.down, direction);
                    break;
            }

        }


        #endregion

        #region TextBehaviour

        // Text-related update methods
        private void UpdateTextOrientation()
        {
            if (!textElement || !isTextInitialized) return;

            Vector3 cameraPos = sceneReferences.mainCamera.transform.position;
            Vector3 cameraForward = sceneReferences.mainCamera.transform.forward;

            textElement.transform.rotation = sceneReferences.mainCamera.transform.rotation;

            Vector3 desiredPosition = pointerObject.transform.position +
                Vector3.Scale(initialTextLocalOffset, pointerObject.transform.localScale);

            textElement.transform.position = desiredPosition;
        }
        private void UpdateTextPosition()
        {
            if (!textElement || !isTextInitialized) return;

            Vector3 textWorldPos = textElement.transform.position;
            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(textWorldPos);

            bool isTextBehind = screenPoint.z < 0;
            if (isTextBehind || isOffScreen)
            {
                textElement.gameObject.SetActive(false);
                return;
            }

            float padding = settings.screenEdgePadding;
            Rect screenRect = new Rect(padding, padding, Screen.width - padding * doubleIndex, Screen.height - padding * doubleIndex);

            Vector3 textSize = textElement.GetRenderedValues(false);
            Vector2 screenTextSize = new Vector2(textSize.x, textSize.y);

            bool needsRepositioning = false;
            Vector3 newPosition = textElement.transform.position;
            TextAlignmentOptions newAlignment = initialAlignment;

            if (screenPoint.x - screenTextSize.x / doubleIndex < screenRect.xMin)
            {
                needsRepositioning = true;
                if (IsRightAligned(initialAlignment))
                    newAlignment = ConvertToLeftAlignment(initialAlignment);
                newPosition = pointerObject.transform.position - initialTextLocalOffset;
            }
            else if (screenPoint.x + screenTextSize.x / doubleIndex > screenRect.xMax)
            {
                needsRepositioning = true;
                if (IsLeftAligned(initialAlignment))
                    newAlignment = ConvertToRightAlignment(initialAlignment);
                newPosition = pointerObject.transform.position - initialTextLocalOffset;
            }

            if (screenPoint.y + screenTextSize.y > screenRect.yMax)
            {
                needsRepositioning = true;
                if (IsBottomAligned(newAlignment))
                    newAlignment = ConvertToTopAlignment(newAlignment);
                newPosition = pointerObject.transform.position - new Vector3(0, initialTextLocalOffset.y, 0);
            }
            else if (screenPoint.y - screenTextSize.y < screenRect.yMin)
            {
                needsRepositioning = true;
                if (IsTopAligned(newAlignment))
                    newAlignment = ConvertToBottomAlignment(newAlignment);

                newPosition = pointerObject.transform.position - new Vector3(0, -initialTextLocalOffset.y, 0);
            }

            if (needsRepositioning)
            {
                textElement.transform.position = newPosition;
                textElement.alignment = newAlignment;
            }
            else
            {
                Vector3 desiredPosition = pointerObject.transform.position +
                    Vector3.Scale(initialTextLocalOffset, pointerObject.transform.localScale);
                textElement.transform.position = desiredPosition;
                textElement.alignment = initialAlignment;
            }

        }

        // Text alignment helper methods
        private bool IsRightAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Right ||
                   alignment == TextAlignmentOptions.TopRight ||
                   alignment == TextAlignmentOptions.BottomRight ||
                   alignment == TextAlignmentOptions.MidlineRight ||
                   alignment == TextAlignmentOptions.BaselineRight;
        }

        private bool IsLeftAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Left ||
                   alignment == TextAlignmentOptions.TopLeft ||
                   alignment == TextAlignmentOptions.BottomLeft ||
                   alignment == TextAlignmentOptions.MidlineLeft ||
                   alignment == TextAlignmentOptions.BaselineLeft;
        }

        private bool IsTopAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Top ||
                   alignment == TextAlignmentOptions.TopLeft ||
                   alignment == TextAlignmentOptions.TopRight;
        }

        private bool IsBottomAligned(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.Bottom ||
                   alignment == TextAlignmentOptions.BottomLeft ||
                   alignment == TextAlignmentOptions.BottomRight;
        }

        // Text alignment conversion methods
        private TextAlignmentOptions ConvertToLeftAlignment(TextAlignmentOptions alignment)
        {
            if (IsTopAligned(alignment)) return TextAlignmentOptions.TopLeft;
            if (IsBottomAligned(alignment)) return TextAlignmentOptions.BottomLeft;
            return TextAlignmentOptions.Left;
        }

        private TextAlignmentOptions ConvertToRightAlignment(TextAlignmentOptions alignment)
        {
            if (IsTopAligned(alignment)) return TextAlignmentOptions.TopRight;
            if (IsBottomAligned(alignment)) return TextAlignmentOptions.BottomRight;
            return TextAlignmentOptions.Right;
        }

        private TextAlignmentOptions ConvertToTopAlignment(TextAlignmentOptions alignment)
        {
            if (IsLeftAligned(alignment)) return TextAlignmentOptions.TopLeft;
            if (IsRightAligned(alignment)) return TextAlignmentOptions.TopRight;
            return TextAlignmentOptions.Top;
        }
        private TextAlignmentOptions ConvertToBottomAlignment(TextAlignmentOptions alignment)
        {
            if (IsLeftAligned(alignment)) return TextAlignmentOptions.BottomLeft;
            if (IsRightAligned(alignment)) return TextAlignmentOptions.BottomRight;
            return TextAlignmentOptions.Bottom;
        }
        private void UpdateTextVisibility()
        {
            if (!textElement || !isTextInitialized) return;

            bool isPointerOffScreen = IsPointerOffScreen();
            if (isPointerOffScreen && settings.pointerIdleAnimation != IdleWorldAnimationType.Blink)
            {
                textElement.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(textElement.transform.position);
            bool isTextBehind = screenPoint.z < 0;

            bool shouldBeVisible = !isTextBehind &&
                                 (settings.pointerIdleAnimation != IdleWorldAnimationType.Blink || isObjectVisibleState);

            if (textElement.gameObject.activeSelf != shouldBeVisible)
            {
                textElement.gameObject.SetActive(shouldBeVisible);
            }
        }
        #endregion

        #region Frame

        // Frame logic

        void InitializeCorners(bool is2D)
        {
            for (int i = 0; i < cornersCount; i++)
            {
                corners[i] = Instantiate(frameCornerPrefab, transform);
                if (is2D)
                    spritesToLerp.Add(corners[i].GetComponent<SpriteRenderer>());
                else
                    meshesToLerp.Add(corners[i].GetComponent<MeshRenderer>());
            }
        }

        Vector3 HandleUIFrame(WorldPointerAnimation.CachedObjectData frameCachedData)
        {
            return Vector3.zero;
        }

        Vector3 AdjustCenterForUI(Vector3 center)
        {
            Vector3 screenCenter = RectTransformUtility.WorldToScreenPoint(sceneReferences.targetCanvas.worldCamera, center);
            Ray ray = sceneReferences.mainCamera.ScreenPointToRay(screenCenter);
            return ray.GetPoint(settings.behindUiDistance);
        }

        void AdjustWorldCorners(Vector3[] worldCorners, Vector3 center)
        {
            float heightOffset = pointerObject.transform.position.y - center.y;
            for (int i = 0; i < cornersCount; i++)
            {
                Vector3 screenCorner = RectTransformUtility.WorldToScreenPoint(sceneReferences.targetCanvas.worldCamera, worldCorners[i]);
                Ray cornerRay = sceneReferences.mainCamera.ScreenPointToRay(screenCorner);
                worldCorners[i] = cornerRay.GetPoint(settings.behindUiDistance);
                worldCorners[i].y += heightOffset;
                worldCorners[i].z = pointerObject.transform.position.z;
            }
        }

        Vector3 HandleRendererFrame(WorldPointerAnimation.CachedObjectData frameCachedData)
        {
            Bounds bounds = GetBounds(frameCachedData);
            Vector3 center = bounds.center;

            float maxX = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x)) + settings.frameOffset;
            float maxY = Mathf.Max(Mathf.Abs(bounds.min.y - center.y), Mathf.Abs(bounds.max.y - center.y)) + settings.frameOffset;

            pointerObject.transform.position = targetWorldPosition;
            PositionCorners(new[]
            {
        center + new Vector3(-maxX,  maxY, 0), // Top Left
        center + new Vector3( maxX,  maxY, 0), // Top Right
        center + new Vector3(-maxX, -maxY, 0), // Bottom Left
        center + new Vector3( maxX, -maxY, 0)  // Bottom Right
    });

            return center;
        }

        Bounds GetBounds(WorldPointerAnimation.CachedObjectData data)
        {
            if (data.mainRenderer != null)
                return data.mainRenderer.bounds;

            Bounds bounds = data.childRenderers[0].bounds;
            for (int i = 1; i < data.childRenderers.Length; i++)
                bounds.Encapsulate(data.childRenderers[i].bounds);

            return bounds;
        }

        void PositionCorners(Vector3[] positions)
        {
            Vector3[] scales = { Vector3.one, new Vector3(-1, 1, 1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1) };
            for (int i = 0; i < cornersCount; i++)
            {
                corners[i].transform.position = positions[i];
                corners[i].transform.localScale = scales[i];
            }
        }

        void ApplyCornerRotation(Quaternion rotation)
        {
            foreach (var corner in corners)
            {
                corner.transform.rotation = rotation;
            }
        }
        #endregion

        #region Helpers

        // Toggles object visibility
        private void TemporarilyTurnOff()
        {
            if (isObjectVisibleState != lastObjectState)
            {
                if (spritesToLerp.Count > 0)
                {
                    for (int i = 0; i < spritesToLerp.Count; i++)
                    {
                        if (spritesToLerp[i] != null)
                        {
                            spritesToLerp[i].enabled = isObjectVisibleState;
                        }
                    }
                }

                if (meshesToLerp.Count > 0)
                {
                    for (int i = 0; i < meshesToLerp.Count; i++)
                    {
                        if (meshesToLerp[i] != null)
                        {
                            meshesToLerp[i].enabled = isObjectVisibleState;
                        }
                    }
                }

                if (textElement && textElement.enabled)
                {
                    textElement.gameObject.SetActive(isObjectVisibleState);
                }

                for (int i = 0; i < corners.Length; i++)
                {
                    if (corners[i] != null)
                    {
                        corners[i].SetActive(isObjectVisibleState);
                    }
                }


                lastObjectState = isObjectVisibleState;
            }
        }
       
        #endregion
    }
}