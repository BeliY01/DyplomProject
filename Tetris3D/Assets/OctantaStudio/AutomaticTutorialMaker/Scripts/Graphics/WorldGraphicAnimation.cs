using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static AutomaticTutorialMaker.WorldGraphicSettings;
using static AutomaticTutorialMaker.WorldPointerSettings;

namespace AutomaticTutorialMaker
{
    public class WorldGraphicAnimation : MonoBehaviour, ITutorialWorldGraphic
    {
        #region Variables
        private ClickData currentStep; // Current tutorial step data
        private TutorialSceneReferences sceneReferences; // References to tutorial scene objects
        [HideInInspector] public bool destroyCommand; // Flag to trigger pointer destruction

        [Header("Settings")]
        public WorldGraphicSettings settings; // Animation and visual settings

        [Header("Components")]
        [SerializeField] private List<SpriteRenderer> spritesToLerp;
        [SerializeField] private List<MeshRenderer> meshesToLerp;
        private GameObject graphicObject; // Main visual object

        [SerializeField] private TMP_Text textElement; // Text element for displaying instruction text

        private int raycastLayerIndex = 2;

        private Quaternion initialTargetRotation; // Initial rotation of the target
        private Color32 initialTextColor; // Initial color of the text element
        private Vector3 initialTextScale; // Initial scale of the text element
        private Vector3 initialTextLocalOffset; // Initial local position offset of text
        private Vector3 initialTextForward; // Initial forward direction of text
        private TextAlignmentOptions initialAlignment; // Initial text alignment option
        private Vector3 initialScale; // Initial scale of the pointer
        private List<Color32> initialSpriteColors = new List<Color32>(); // Initial colors of sprite renderers
        private List<Material> initialMeshMaterials = new List<Material>(); // Initial materials of mesh renderers

        private Vector3 lastIdleScale; // Last idle animation scale
        private float idleTime = 0f; // Time in idle animation
        private Vector3 lastIdlePosition; // Last idle animation position
        private bool lastObjectState = true; // Previous visibility state

        private bool isAnimating = false; // Animation state
        private bool isReversing = false; // Reverse animation state
        private float animationProgress = 0f; // Animation progress
        private bool destroyAfterAnimation = false; // Whether to destroy after animation

        // Animation control flags
        private bool isIdleActive = false; // Idle animation active state
        private bool canPlayIdle = false; // Whether idle animation can play
        private bool hasReachedTarget = false; // Whether target position is reached
        private bool isIdleInitialized = false; // Idle animation initialization state
        private float blinkTime = 0f; // Time in blink animation
        private bool isBlinking = true; // Blink animation state
        private float pointerLevitationTime = 0f; // Time tracker for levitation animation
        private float timer; // General purpose timer

        private Vector3 currentScaleVelocity; // Current scale change velocity
        private float finalRotation = 360f;
        private float almostOne = 0.99f;
        private float minDistance = 0.01f;
        private float oneAndHalf = 1.5f;
        private float turnDegrees = 180;

        private bool isOffScreen; // Whether pointer is currently off screen

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
            graphicObject = transform.gameObject;
            if (settings == null)
            {
                settings = sceneReferences.NoneWorldGraphicSettings;
            }

            // Save initial values
            initialScale = graphicObject.transform.localScale;

            GraphicsOptimization();

            TextProcessing();

            ReassignColors();

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
            Debug.Log("[ATM] World Graphic initialized. Local Scale: " + graphicObject.transform.localScale + "; Local Position: " + graphicObject.transform.localPosition);
        }
        private void SetupInitialState(WorldAnimationType animationType)
        {
            if (!graphicObject) return;

            initialTargetRotation = graphicObject.transform.rotation;

            switch (animationType)
            {
                case WorldAnimationType.Zoom:
                case WorldAnimationType.ZoomAndRotate:
                    graphicObject.transform.localScale = isReversing ? initialScale : Vector3.zero;

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
            }

            if (!isReversing)
            {
                graphicObject.SetActive(true);
            }
        }
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

            if (textElement && textElement.gameObject.activeSelf)
            {
                textElement.gameObject.layer = raycastLayerIndex;
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
                textTip = currentStep.localizationReference.WorldGraphicText;
            }

            if (textElement && textElement.gameObject.activeSelf)
            {
                initialTextScale = textElement.transform.localScale;
                initialAlignment = textElement.alignment;
                initialTextLocalOffset = textElement.transform.localPosition;
                initialTextForward = textElement.transform.forward;

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

        public void Hide()
        {
            isAnimating = true;
            isReversing = true;
            animationProgress = 0f;
            destroyAfterAnimation = true;

            if (settings.disappearAnimation == WorldAnimationType.None)
            {
                Destroy(graphicObject);
                return;
            }

            SetupInitialState(settings.disappearAnimation);
        }
        #endregion

        #region Update

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
                        case WorldAnimationType.ZoomAndRotate:
                        case WorldAnimationType.Zoom:
                        case WorldAnimationType.Fade:
                        case WorldAnimationType.None:
                            hasReachedTarget = true;
                            break;
                    }

                    canPlayIdle = hasReachedTarget;
                }
            }
            if (!isReversing && isIdleActive && canPlayIdle)
            {
                isOffScreen = IsPointerOffScreen();

                UpdatePosition();
                PlayIdleAnimation();
                HandleRotation();

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

        #region Animation

        private void PlayIdleAnimation()
        {
            if (currentStep == null || settings == null) return;

            if (!graphicObject || !isIdleInitialized)
            {
                lastIdlePosition = graphicObject.transform.localPosition;
                lastIdleScale = graphicObject.transform.localScale;
                isIdleInitialized = true;
                return;
            }

            switch (settings.graphicIdleAnimation)
            {
                case IdleWorldGraphicAnimationType.Levitate:
                    idleTime += Time.deltaTime * settings.levitationSpeed;
                    float levitateProgress = (Mathf.Sin(idleTime) + 1f) * 0.5f;
                    Vector3 levitateOffset = settings.levitationDirection * settings.levitationRange * levitateProgress;
                    graphicObject.transform.localPosition = lastIdlePosition + levitateOffset;
                    break;

                case IdleWorldGraphicAnimationType.Pulse:
                    idleTime += Time.deltaTime * settings.pulseSpeed;
                    float scaleDelta = Mathf.Sin(idleTime) * settings.pulseDelta;
                    Vector3 targetScale = initialScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                    graphicObject.transform.localScale = Vector3.Lerp(
                        graphicObject.transform.localScale,
                        targetScale,
                        Time.deltaTime * settings.pulseSpeed
                    );
                    break;

                case IdleWorldGraphicAnimationType.Blink:
                    if (!isOffScreen)
                    {
                        blinkTime += Time.deltaTime;
                        if (blinkTime >= settings.blinkInterval)
                        {
                            blinkTime = 0f;
                            isBlinking = !isBlinking;
                            TemporarilyTurnOff();
                        }
                    }
                    break;
                case IdleWorldGraphicAnimationType.RotateSpin:
                    pointerLevitationTime += Time.deltaTime * settings.spinSpeed;
                    float spinRotation = pointerLevitationTime;

                    graphicObject.transform.localRotation = initialTargetRotation *
                        Quaternion.AngleAxis(spinRotation, settings.spinAxis.normalized);
                    break;
            }
        }

        private void Animate()
        {
            if (!graphicObject) return;

            float duration = isReversing ? settings.disappearDuration : settings.appearDuration;
            float t = Mathf.Clamp01(animationProgress / duration);

            WorldAnimationType currentAnimation = isReversing ? settings.disappearAnimation : settings.appearAnimation;

            bool targetReached = false;
            Vector3 targetScale;

            switch (currentAnimation)
            {
                case WorldAnimationType.Zoom:
                    Vector3 currentScale = graphicObject.transform.localScale;
                    targetScale = isReversing ? Vector3.zero : initialScale;
                    graphicObject.transform.localScale = Vector3.Lerp(
                        isReversing ? initialScale : Vector3.zero,
                        targetScale,
                        t
                    );
                    targetReached = Vector3.Distance(currentScale, targetScale) < minDistance;
                    break;

                case WorldAnimationType.ZoomAndRotate:
                    currentScale = graphicObject.transform.localScale;
                    targetScale = isReversing ? Vector3.zero : initialScale;
                    graphicObject.transform.localScale = Vector3.Lerp(
                        isReversing ? initialScale : Vector3.zero,
                        targetScale,
                        t
                    );

                    float totalRotation = settings.spinRevolutions * finalRotation;
                    float rotationAngle = Mathf.Lerp(0, totalRotation, t);
                    graphicObject.transform.rotation = initialTargetRotation * Quaternion.AngleAxis(rotationAngle, settings.spinAxis.normalized);

                    targetReached = Vector3.Distance(currentScale, targetScale) < minDistance && Mathf.Approximately(rotationAngle % finalRotation, 0f);
                    break;


                case WorldAnimationType.Fade:

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

                    break;
                default:
                    targetReached = true;
                    break;
            }

            animationProgress += Time.deltaTime;

            if (targetReached || animationProgress >= duration * oneAndHalf)
            {
                isAnimating = false;
                if (destroyAfterAnimation)
                {
                    Destroy(graphicObject);
                }

                if (!isReversing && !destroyAfterAnimation)
                {
                    switch (currentAnimation)
                    {
                        case WorldAnimationType.ZoomAndRotate:
                        case WorldAnimationType.Zoom:
                            graphicObject.transform.localScale = initialScale;
                            break;

                        case WorldAnimationType.Fade:
                            break;
                    }
                }
            }
        }

        #endregion

        #region Movement
        private void UpdatePosition()
        {
            if (settings.placementBehaviour == PlacementBehaviour.FrontOfCamera)
            {
                Vector3 cameraForward = sceneReferences.mainCamera.transform.forward;
                Vector3 basePosition = sceneReferences.mainCamera.transform.position + cameraForward * settings.frontCameraDistance;

                float viewportMarginX = settings.marginPixels / sceneReferences.mainCamera.pixelWidth;
                float viewportMarginY = settings.marginPixels / sceneReferences.mainCamera.pixelHeight;

                Vector2 viewportPoint = GetViewportPointForAnchor(settings.frontAnchorPosition);

                bool isCorner = (viewportPoint.x != 0.5f && viewportPoint.y != 0.5f);

                if (isCorner)
                {
                    if (viewportPoint.x == 0) 
                        viewportPoint.x += viewportMarginX;
                    else if (viewportPoint.x == 1) 
                        viewportPoint.x -= viewportMarginX;

                    if (viewportPoint.y == 0)
                        viewportPoint.y += viewportMarginY;
                    else if (viewportPoint.y == 1) 
                        viewportPoint.y -= viewportMarginY;
                }
                else
                {
                    if (viewportPoint.x == 0) 
                        viewportPoint.x += viewportMarginX;
                    else if (viewportPoint.x == 1) 
                        viewportPoint.x -= viewportMarginX;
                    else if (viewportPoint.y == 0) 
                        viewportPoint.y += viewportMarginY;
                    else if (viewportPoint.y == 1) 
                        viewportPoint.y -= viewportMarginY;
                }

                Vector3 targetPosition = sceneReferences.mainCamera.ViewportToWorldPoint(
                    new Vector3(viewportPoint.x, viewportPoint.y, settings.frontCameraDistance)
                );

                graphicObject.transform.position = targetPosition;
            }
        }

        Vector2 GetViewportPointForAnchor(AnchorPosition anchor)
        {
            switch (anchor)
            {
                case AnchorPosition.TopLeft:
                    return new Vector2(0, 1);
                case AnchorPosition.TopCenter:
                    return new Vector2(0.5f, 1);
                case AnchorPosition.TopRight:
                    return new Vector2(1, 1);
                case AnchorPosition.MiddleLeft:
                    return new Vector2(0, 0.5f);
                case AnchorPosition.MiddleCenter:
                    return new Vector2(0.5f, 0.5f);
                case AnchorPosition.MiddleRight:
                    return new Vector2(1, 0.5f);
                case AnchorPosition.BottomLeft:
                    return new Vector2(0, 0);
                case AnchorPosition.BottomCenter:
                    return new Vector2(0.5f, 0);
                case AnchorPosition.BottomRight:
                    return new Vector2(1, 0);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        // Checks if pointer is off screen
        private bool IsPointerOffScreen()
        {
            if (settings.defaultEdgeBehaviour == EdgeGraphicBehaviour.None)
            {
                return false;
            }

            Vector3 screenPoint = sceneReferences.mainCamera.WorldToScreenPoint(graphicObject.transform.position);
            float padding = settings.screenEdgePadding;
            return screenPoint.z < 0 ||
                   screenPoint.x < padding ||
                   screenPoint.x > Screen.width - padding ||
                   screenPoint.y < padding ||
                   screenPoint.y > Screen.height - padding;
        }

        #endregion

        #region Rotation
        private void HandleRotation()
        {
            if (isOffScreen)
            {
                if (settings.defaultEdgeBehaviour == EdgeGraphicBehaviour.DisableOnExit)
                {
                    isBlinking = false;
                    TemporarilyTurnOff();
                    return;
                }
            }
            else
            {
                if (!isBlinking && settings.graphicIdleAnimation != IdleWorldGraphicAnimationType.Blink)
                {
                    isBlinking = true;
                }
            }

            TemporarilyTurnOff();

            if (settings.graphicIdleAnimation != IdleWorldGraphicAnimationType.RotateSpin)
            {
                if (settings.faceToCamera)
                {
                    Vector3 camForward = sceneReferences.mainCamera.transform.forward;
                    Vector3 camRight = sceneReferences.mainCamera.transform.right;
                    Vector3 up = Vector3.Cross(camForward, camRight);
                    graphicObject.transform.rotation = Quaternion.LookRotation(-camForward, up);
                    graphicObject.transform.Rotate(Vector3.up, turnDegrees);
                }
            }
        }

        #endregion

        #region Helpers

        public void Destroy()
        {
            if (graphicObject)
            {
                Destroy(graphicObject);
            }
        }

        private void TemporarilyTurnOff()
        {
            if (isBlinking != lastObjectState)
            {
                if (spritesToLerp.Count > 0)
                {
                    for (int i = 0; i < spritesToLerp.Count; i++)
                    {
                        if (spritesToLerp[i] != null)
                        {
                            spritesToLerp[i].enabled = isBlinking;
                        }
                    }
                }

                if (meshesToLerp.Count > 0)
                {
                    for (int i = 0; i < meshesToLerp.Count; i++)
                    {
                        if (meshesToLerp[i] != null)
                        {
                            meshesToLerp[i].enabled = isBlinking;
                        }
                    }
                }


                if (textElement && textElement.enabled)
                {
                    textElement.gameObject.SetActive(isBlinking);
                }

                lastObjectState = isBlinking;
            }
        }

        #endregion
    }
}