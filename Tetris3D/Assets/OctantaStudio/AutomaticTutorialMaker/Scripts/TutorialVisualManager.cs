using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AutomaticTutorialMaker
{
    public class TutorialVisualManager : MonoBehaviour
    {
        #region Variables
     [Grayed] public TutorialSceneReferences sceneReferences; // References to scene components

        // Storage for pointer visuals
        private Dictionary<ClickData, Dictionary<GameObject, ITutorialVisual>> activePointers
            = new Dictionary<ClickData, Dictionary<GameObject, ITutorialVisual>>();

        // Storage for graphic visuals
        private Dictionary<ClickData, ITutorialGraphic> activeGraphics
            = new Dictionary<ClickData, ITutorialGraphic>();

        // Maps visuals to GameObjects
        private Dictionary<ITutorialVisual, GameObject> visualGameObjects
            = new Dictionary<ITutorialVisual, GameObject>();

        // Storage for hover effects
        private Dictionary<ClickData, ITutorialHover> activeHovers
            = new Dictionary<ClickData, ITutorialHover>();

        private BackgroundManager backgroundManager; // Manager for UI background overlay

        // World tips
        private Dictionary<ClickData, Dictionary<GameObject, ITutorialWorldVisual>> activeWorldPointers
           = new Dictionary<ClickData, Dictionary<GameObject, ITutorialWorldVisual>>();

        private Dictionary<ClickData, ITutorialWorldGraphic> activeWorldGraphics
            = new Dictionary<ClickData, ITutorialWorldGraphic>();

        private Dictionary<ClickData, ITutorialWorldHover> activeWorldHovers
            = new Dictionary<ClickData, ITutorialWorldHover>();

        private Dictionary<ITutorialWorldVisual, GameObject> visualWorldGameObjects
            = new Dictionary<ITutorialWorldVisual, GameObject>();

        #endregion

        #region Initialization
        // Initializes tutorial visual manager
        public void Initialize()
        {
            CleanupAllVisuals();
            activePointers.Clear();
            activeGraphics.Clear();
            visualGameObjects.Clear();
            activeHovers.Clear();

            activeWorldPointers.Clear();
            activeWorldGraphics.Clear();
            visualWorldGameObjects.Clear();
            activeWorldHovers.Clear();

            backgroundManager = new BackgroundManager(sceneReferences);
            backgroundManager.Initialize();
        }
        #endregion

        #region BackroungLogic
        // Updates visuals each frame
        private void Update()
        {
            backgroundManager?.Update();
        }

        // Requests background overlay display
        internal void RequestBackground(Color backgroundColor, float fadeInDuration)
        {
            backgroundManager?.RequestBackground(sceneReferences.backroundPrefab, backgroundColor, fadeInDuration);
        }

        // Releases background overlay
        internal void ReleaseBackground(float fadeOutDuration)
        {
            backgroundManager?.ReleaseBackground(fadeOutDuration);
        }
        #endregion

        #region VisualsInitialization
        // Updates all visuals for a tutorial step
        public void UpdateVisuals(ClickData step)
        {
            if (step == null) return;

            if ((step.pointerPrefab != null || step.hoverPrefab != null || step.worldPointerPrefab != null || step.worldHoverPrefab != null) &&
                step.checkInteraction != InteractionTargetEnum.AnyTarget)
            {
                UpdateTargetObjects(step);
            }

            if (step.hoverPrefab != null && step.checkInteraction != InteractionTargetEnum.AnyTarget)
            {
                UpdateHoverVisual(step);
            }

            if (step.graphicPrefab != null)
            {
                UpdateGraphicVisual(step);
            }

            if (step.pointerPrefab != null && step.checkInteraction != InteractionTargetEnum.AnyTarget)
            {
                UpdatePointerVisual(step);
            }

            if (step.worldHoverPrefab != null && step.checkInteraction != InteractionTargetEnum.AnyTarget)
            {
                UpdateWorldHoverVisual(step);
            }

            if (step.worldGraphicPrefab != null)
            {
                UpdateWorldGraphicVisual(step);
            }

            if (step.worldPointerPrefab != null && step.checkInteraction != InteractionTargetEnum.AnyTarget)
            {
                UpdateWorldPointerVisual(step);
            }
        }

        // Updates target objects for interaction
        private void UpdateTargetObjects(ClickData step)
        {
            if (step.checkInteraction == InteractionTargetEnum.ByGameObject)
            {
                if (step.GameObjects == null || step.GameObjects.Count == 0)
                {
                    Debug.LogError("[ATM] No target objects assigned for ByGameObject interaction type");
                }
                return;
            }

            if (step.GameObjects == null)
            {
                step.GameObjects = new List<UnityEngine.Object>();
            }
            else
            {
                step.GameObjects.Clear();
            }

            if (step.checkInteraction == InteractionTargetEnum.ByTag)
            {
                if (step.tags == null || step.tags.Count == 0)
                {
                    Debug.LogError("[ATM] No tags specified for ByTag interaction type");
                    return;
                }

                for (int i = 0; i < step.tags.Count; i++)
                {
                    string tag = step.tags[i];
                    ObjectTypeEnum targetType = step.objectTypes != null && i < step.objectTypes.Count
                        ? step.objectTypes[i]
                        : (step.objectTypes != null && step.objectTypes.Count > 0
                            ? step.objectTypes[step.objectTypes.Count - 1]
                            : ObjectTypeEnum.None);

                    GameObject[] taggedObjects;
                    bool foundObject = false;

                    // Untagged exeption
                    if (tag == "Untagged")
                    {
                        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                        taggedObjects = allObjects.Where(obj => obj.CompareTag("Untagged")).ToArray();
                        Debug.Log($"[ATM] Found {taggedObjects.Length} objects with tag Untagged (special handling)");
                    }
                    else
                    {
                        taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                        Debug.Log($"[ATM] Found {taggedObjects.Length} objects with tag {tag}");
                    }

                    foreach (GameObject obj in taggedObjects)
                    {
                        if (IsObjectOfType(obj, targetType) && !step.GameObjects.Contains(obj))
                        {
                            step.GameObjects.Add(obj);
                            foundObject = true;
                            break;
                        }
                    }

                    if (!foundObject)
                    {
                        Debug.LogError($"[ATM] No object found with tag {tag} of type {targetType}");
                    }
                }
            }
            else if (step.checkInteraction == InteractionTargetEnum.ByLayer)
            {
                if (step.layers == null || step.layers.Count == 0)
                {
                    Debug.LogError("[ATM] No layers specified for ByLayer interaction type");
                    return;
                }

                for (int i = 0; i < step.layers.Count; i++)
                {
                    int layer = step.layers[i];
                    ObjectTypeEnum targetType = step.objectTypes != null && i < step.objectTypes.Count
                        ? step.objectTypes[i]
                        : (step.objectTypes != null && step.objectTypes.Count > 0
                            ? step.objectTypes[step.objectTypes.Count - 1]
                            : ObjectTypeEnum.None);

                    bool foundObject = false;
                    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.layer == layer && IsObjectOfType(obj, targetType) && !step.GameObjects.Contains(obj))
                        {
                            step.GameObjects.Add(obj);
                            foundObject = true;
                            break;
                        }
                    }

                    if (!foundObject)
                    {
                        Debug.LogError($"[ATM] No object found in layer {LayerMask.LayerToName(layer)} of type {targetType}");
                    }
                }
            }
        }

        // Checks if object matches specified type
        private bool IsObjectOfType(GameObject obj, ObjectTypeEnum type)
        {
            switch (type)
            {
                case ObjectTypeEnum._3D:
                    return obj.GetComponent<MeshFilter>() != null || obj.GetComponent<MeshRenderer>() != null;
                case ObjectTypeEnum._2D:
                    return obj.GetComponent<SpriteRenderer>() != null || obj.GetComponent<Collider2D>() != null;
                case ObjectTypeEnum.UI:
                    return obj.GetComponent<RectTransform>() != null;
                case ObjectTypeEnum.None:
                    return true;
                default:
                    return false;
            }
        }

        // Updates hover visual for a step
        private void UpdateHoverVisual(ClickData step)
        {
            if (activeHovers.TryGetValue(step, out ITutorialHover existingHover))
            {
                existingHover.Show(step);
                return;
            }

            var newHover = new HoverVisual();
            newHover.Initialize(sceneReferences);
            activeHovers[step] = newHover;
            newHover.Show(step);
        }

        private void UpdateWorldHoverVisual(ClickData step)
        {
            if (activeWorldHovers.TryGetValue(step, out ITutorialWorldHover existingHover))
            {
                existingHover.Show(step);
                return;
            }

            var newHover = new WorldHoverVisual();
            newHover.Initialize(sceneReferences);
            activeWorldHovers[step] = newHover;
            newHover.Show(step);
        }

        // Updates pointer visual for a step
        private void UpdatePointerVisual(ClickData step)
        {
            if (!activePointers.ContainsKey(step))
            {
                activePointers[step] = new Dictionary<GameObject, ITutorialVisual>();
            }

            if (step.checkInteraction == InteractionTargetEnum.AnyTarget || step.pointerPrefab == null)
            {
                if (activePointers[step].Count > 0)
                {
                    foreach (var visual in activePointers[step].Values.ToList())
                    {
                        visual.Hide();
                        if (visualGameObjects.ContainsKey(visual))
                        {
                            visualGameObjects.Remove(visual);
                        }
                    }
                    activePointers[step].Clear();
                }
                return;
            }

            bool needsNewVisual = true;
            ITutorialVisual existingVisual = null;

            if (activePointers[step].Count > 0)
            {
                existingVisual = activePointers[step].Values.First();
                if (visualGameObjects.TryGetValue(existingVisual, out GameObject visualObj))
                {
                    needsNewVisual = visualObj.name != (step.pointerPrefab.name + "(Clone)");
                }
            }

            if (needsNewVisual && existingVisual != null)
            {
                existingVisual.Hide();
                activePointers[step].Clear();
                if (visualGameObjects.ContainsKey(existingVisual))
                {
                    visualGameObjects.Remove(existingVisual);
                }
            }

            if (needsNewVisual)
            {
                var newVisual = new PointerGraphicVisual();
                newVisual.Initialize(sceneReferences, this);
                activePointers[step][step.pointerPrefab] = newVisual;

                if (newVisual.pointerInstance != null)
                {
                    visualGameObjects[newVisual] = newVisual.pointerInstance;
                }

                newVisual.Show(step);
            }
            else if (existingVisual != null)
            {
                existingVisual.Show(step);
            }
        }

        private void UpdateWorldPointerVisual(ClickData step)
        {
            if (!activeWorldPointers.ContainsKey(step))
            {
                activeWorldPointers[step] = new Dictionary<GameObject, ITutorialWorldVisual>();
            }

            if (step.checkInteraction == InteractionTargetEnum.AnyTarget || step.worldPointerPrefab == null)
            {
                if (activeWorldPointers[step].Count > 0)
                {
                    foreach (var visual in activeWorldPointers[step].Values.ToList())
                    {
                        visual.Hide();
                        if (visualWorldGameObjects.ContainsKey(visual))
                        {
                            visualWorldGameObjects.Remove(visual);
                        }
                    }
                    activeWorldPointers[step].Clear();
                }
                return;
            }

            bool needsNewVisual = true;
            ITutorialWorldVisual existingVisual = null;

            if (activeWorldPointers[step].Count > 0)
            {
                existingVisual = activeWorldPointers[step].Values.First();
                if (visualWorldGameObjects.TryGetValue(existingVisual, out GameObject visualObj))
                {
                    needsNewVisual = visualObj.name != (step.worldPointerPrefab.name + "(Clone)");
                }
            }

            if (needsNewVisual && existingVisual != null)
            {
                existingVisual.Hide();
                activeWorldPointers[step].Clear();
                if (visualWorldGameObjects.ContainsKey(existingVisual))
                {
                    visualWorldGameObjects.Remove(existingVisual);
                }
            }

            if (needsNewVisual)
            {
                var newVisual = new WorldPointerVisual();
                newVisual.Initialize(sceneReferences, this);
                activeWorldPointers[step][step.worldPointerPrefab] = newVisual;

                if (newVisual.pointerInstance != null)
                {
                    visualWorldGameObjects[newVisual] = newVisual.pointerInstance;
                }

                newVisual.Show(step);
            }
            else if (existingVisual != null)
            {
                existingVisual.Show(step);
            }
        }

        // Updates graphic visual for a step
        private void UpdateGraphicVisual(ClickData step)
        {
            if (activeGraphics.TryGetValue(step, out ITutorialGraphic existingGraphic))
            {
                existingGraphic.UpdatePosition();
                return;
            }

            GameObject graphicInstance = Instantiate(step.graphicPrefab, sceneReferences.targetCanvas.transform);

            CheckAndDisableIncorrectAnimationComponent<UIGraphicAnimation>(graphicInstance, "UIGraphicAnimation");

            var graphicAnimation = graphicInstance.GetComponent<UIGraphicAnimation>();

            if (graphicAnimation == null)
            {
                graphicAnimation = graphicInstance.AddComponent<UIGraphicAnimation>();
            }

            graphicAnimation.Initialize(step, sceneReferences);
            activeGraphics[step] = graphicAnimation;
            graphicAnimation.Show(step);
        }

        private void UpdateWorldGraphicVisual(ClickData step)
        {
            if (activeWorldGraphics.TryGetValue(step, out ITutorialWorldGraphic existingGraphic))
            {
                return;
            }

            GameObject graphicInstance = Instantiate(step.worldGraphicPrefab, sceneReferences.targetWorldHolder);

            CheckAndDisableIncorrectAnimationComponent<WorldGraphicAnimation>(graphicInstance, "WorldGraphicAnimation");

            var graphicAnimation = graphicInstance.GetComponent<WorldGraphicAnimation>();

            if (graphicAnimation == null)
            {
                graphicAnimation = graphicInstance.AddComponent<WorldGraphicAnimation>();
            }

            graphicAnimation.Initialize(step, sceneReferences);
            activeWorldGraphics[step] = graphicAnimation;
            graphicAnimation.Show(step);
        }

        public void CheckAndDisableIncorrectAnimationComponent<T>(GameObject instance, string expectedType) where T : MonoBehaviour
        {
            var components = instance.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if ((component is UIGraphicAnimation ||
                     component is WorldGraphicAnimation ||
                     component is UIPointerGraphAnimation ||
                     component is WorldPointerAnimation) &&
                    !(component is T))
                {
                    component.enabled = false;
                    instance.SetActive(false);
                    Debug.LogError($"[ATM] Found incorrect visual component {component.GetType().Name} on {instance.name}. Expected {expectedType}. The incorrect component has been disabled.");
                }
            }
        }
        #endregion

        #region Translation
        // Translates all active tutorial visuals
        public void UpdateTranslation(ClickData step)
        {
            if (step.localizationReference != null)
            {
                if (activePointers.TryGetValue(step, out var pointers))
                {
                    foreach (var visual in pointers.Values)
                    {
                        visual.Translate(step.localizationReference.PointerText);
                    }
                }

                if (activeWorldPointers.TryGetValue(step, out var worldPointers))
                {
                    foreach (var visual in worldPointers.Values)
                    {
                        visual.Translate(step.localizationReference.WorldPointerText);
                    }
                }

                if (activeGraphics.TryGetValue(step, out var graphic))
                {
                    graphic.SetText(step.localizationReference.GraphicText);
                }

                if (activeWorldGraphics.TryGetValue(step, out var worldGraphic))
                {
                    worldGraphic.SetText(step.localizationReference.WorldGraphicText);
                }
            }
        }
        #endregion

        #region VisualsDisable
        // Disables visuals for specific tutorial step
        public void DisableVisualsForStep(ClickData step)
        {
            if (step == null) return;

            step.blockedByTime = false;

            if (activePointers.TryGetValue(step, out var pointers))
            {
                foreach (var visual in pointers.Values)
                {
                    visual.Hide();
                }
                activePointers.Remove(step);
            }

            if (activeWorldPointers.TryGetValue(step, out var worldpointers))
            {
                foreach (var visual in worldpointers.Values)
                {
                    visual.Hide();
                }
                activeWorldPointers.Remove(step);
            }

            if (activeGraphics.TryGetValue(step, out var graphic))
            {
                var graphicAnimation = graphic as UIGraphicAnimation;
                if (graphicAnimation != null && graphicAnimation.settings != null &&
                    graphicAnimation.backgroundColor.a > 0)
                {
                    backgroundManager?.ReleaseBackground(graphicAnimation.settings.disappearDuration);
                }

                graphic.Hide();
                activeGraphics.Remove(step);
            }

            if (activeWorldGraphics.TryGetValue(step, out var worldgraphic))
            {
                var graphicAnimation = graphic as WorldGraphicAnimation;             

                worldgraphic.Hide();
                activeGraphics.Remove(step);
            }

            if (activeHovers.TryGetValue(step, out var hover))
            {
                hover.Hide();
                activeHovers.Remove(step);
            }

            if (activeWorldHovers.TryGetValue(step, out var worldhover))
            {
                worldhover.Hide();
                activeWorldHovers.Remove(step);
            }
        }

        // Disables all active tutorial visuals
        public void DisableAllVisuals()
        {
            foreach (var stepPointers in activePointers.Values)
            {
                foreach (var visual in stepPointers.Values)
                {
                    visual.Hide();
                }
            }
            activePointers.Clear();

            foreach (var stepPointers in activeWorldPointers.Values)
            {
                foreach (var visual in stepPointers.Values)
                {
                    visual.Hide();
                }
            }
            activeWorldPointers.Clear();

            foreach (var graphic in activeGraphics.Values)
            {
                graphic.Hide();
            }
            activeGraphics.Clear();

            foreach (var graphic in activeWorldGraphics.Values)
            {
                graphic.Hide();
            }
            activeWorldGraphics.Clear();

            foreach (var hover in activeHovers.Values)
            {
                hover.Hide();
            }
            activeHovers.Clear();

            foreach (var hover in activeWorldHovers.Values)
            {
                hover.Hide();
            }
            activeWorldHovers.Clear();
        }

        // Cleans up all visual objects
        private void CleanupAllVisuals()
        {
            if (visualGameObjects != null)
            {
                foreach (var visualObj in visualGameObjects.Values.ToList())
                {
                    if (visualObj != null && visualObj)
                    {
                        Destroy(visualObj);
                    }
                }
                visualGameObjects.Clear();
            }
            if (visualWorldGameObjects != null)
            {
                foreach (var visualObj in visualWorldGameObjects.Values.ToList())
                {
                    if (visualObj != null && visualObj)
                    {
                        Destroy(visualObj);
                    }
                }
                visualWorldGameObjects.Clear();
            }

            if (activePointers != null)
            {
                foreach (var stepVisuals in activePointers.Values)
                {
                    if (stepVisuals != null)
                    {
                        foreach (var visual in stepVisuals.Values.ToList())
                        {
                            if (visual != null)
                            {
                                try
                                {
                                    visual.Destroy();
                                }
                                catch (MissingReferenceException)
                                {
                                }
                            }
                        }
                    }
                }
                activePointers.Clear();
            }
            if (activeWorldPointers != null)
            {
                foreach (var stepVisuals in activeWorldPointers.Values)
                {
                    if (stepVisuals != null)
                    {
                        foreach (var visual in stepVisuals.Values.ToList())
                        {
                            if (visual != null)
                            {
                                try
                                {
                                    visual.Destroy();
                                }
                                catch (MissingReferenceException)
                                {
                                }
                            }
                        }
                    }
                }
                activeWorldPointers.Clear();
            }


            if (activeGraphics != null)
            {
                foreach (var graphic in activeGraphics.Values.ToList())
                {
                    if (graphic != null)
                    {
                        try
                        {
                            graphic.Destroy();
                        }
                        catch (MissingReferenceException)
                        {

                        }
                    }
                }
                activeGraphics.Clear();
            }
            if (activeWorldGraphics != null)
            {
                foreach (var graphic in activeWorldGraphics.Values.ToList())
                {
                    if (graphic != null)
                    {
                        try
                        {
                            graphic.Destroy();
                        }
                        catch (MissingReferenceException)
                        {

                        }
                    }
                }
                activeWorldGraphics.Clear();
            }

            if (activeHovers != null)
            {
                foreach (var hover in activeHovers.Values.ToList())
                {
                    if (hover != null)
                    {
                        try
                        {
                            hover.Destroy();
                        }
                        catch (MissingReferenceException)
                        {

                        }
                    }
                }
                activeHovers.Clear();
            }
            if (activeWorldHovers != null)
            {
                foreach (var hover in activeWorldHovers.Values.ToList())
                {
                    if (hover != null)
                    {
                        try
                        {
                            hover.Destroy();
                        }
                        catch (MissingReferenceException)
                        {

                        }
                    }
                }
                activeWorldHovers.Clear();
            }
        }

        // Destroys all visual objects
        public void DestroyAllVisuals()
        {
            CleanupAllVisuals();
        }

        // Cleanup on component disable
        private void OnDisable()
        {
            CleanupAllVisuals();
        }

        // Cleanup on component destroy
        private void OnDestroy()
        {
            CleanupAllVisuals();
        }
        #endregion
    }

    #region Interfaces
    public interface ITutorialGraphic
    {
        void Initialize(ClickData step, TutorialSceneReferences references); // Initializes graphic
        void SetText(string textValue);
        void Show(ClickData step); // Shows graphic
        void Hide(); // Hides graphic
        void UpdatePosition(); // Updates graphic position
        void Destroy(); // Destroys graphic
    }
    public interface ITutorialWorldGraphic
    {
        void Initialize(ClickData step, TutorialSceneReferences references); // Initializes graphic
        void SetText(string textValue);
        void Show(ClickData step); // Shows graphic
        void Hide(); // Hides graphic
        void Destroy(); // Destroys graphic
    }

    public interface ITutorialVisual
    {
        void Initialize(TutorialSceneReferences references, TutorialVisualManager visuals); // Initializes visual
        void Show(ClickData step); // Shows visual
        void Hide(); // Hides visual
        void Translate(string textValue);
        void Destroy(); // Destroys visual
    }

    public interface ITutorialWorldVisual
    {
        void Initialize(TutorialSceneReferences references, TutorialVisualManager visuals); // Initializes visual
        void Translate(string textValue);
        void Show(ClickData step); // Shows visual
        void Hide(); // Hides visual
        void Destroy(); // Destroys visual
    }

    public interface ITutorialHover
    {
        void Initialize(TutorialSceneReferences references); // Initializes hover effect
        void Show(ClickData step); // Shows hover effect
        void Hide(); // Hides hover effect
        void Destroy(); // Destroys hover effect
    }

    public interface ITutorialWorldHover
    {
        void Initialize(TutorialSceneReferences references); // Initializes hover effect
        void Show(ClickData step); // Shows hover effect
        void Hide(); // Hides hover effect
        void Destroy(); // Destroys hover effect
    }

    #endregion

    #region PointerHelperClasses
    public class PointerGraphicVisual : ITutorialVisual
    {
        private TutorialVisualManager visualManager;
        private TutorialSceneReferences sceneReferences; // References to scene components
        internal GameObject pointerInstance; // Instance of pointer visual
        private UIPointerGraphAnimation pointerAnimation; // Animation controller for pointer

        // Initializes pointer visual with references
        public void Initialize(TutorialSceneReferences references, TutorialVisualManager visuals)
        {
            sceneReferences = references;
            visualManager = visuals;
        }

        public void Translate(string textValue)
        {
            pointerAnimation.SetText(textValue);
        }

        // Shows pointer visual for step
        public void Show(ClickData step)
        {
            if (step.pointerPrefab == null)
            {
                Debug.Log("[ATM] No pointer prefab assigned for this tutorial step.");
                return;
            }

            if (pointerInstance == null)
            {
                pointerInstance = GameObject.Instantiate(step.pointerPrefab,
                                                       sceneReferences.targetCanvas.transform);

                visualManager.CheckAndDisableIncorrectAnimationComponent<UIPointerGraphAnimation>(pointerInstance, "UIPointerGraphAnimation");

                pointerAnimation = pointerInstance.GetComponent<UIPointerGraphAnimation>();
                if (pointerAnimation == null)
                {
                    Debug.Log("[ATM] UIPointerGraphAnimation not found on prefab, adding default configuration.");
                    pointerAnimation = pointerInstance.AddComponent<UIPointerGraphAnimation>();
                }
                pointerAnimation.Initialize(step, sceneReferences);
            }

            if (pointerAnimation != null)
            {
                pointerAnimation.UpdateTargetObjects(step.GameObjects);
                pointerAnimation.StartAnimation();
            }
        }

        // Hides pointer visual
        public void Hide()
        {
            if (pointerAnimation != null)
            {
                pointerAnimation.FinishAnimation();
            }
        }

        // Destroys pointer visual
        public void Destroy()
        {
            if (pointerInstance != null)
            {
                GameObject.Destroy(pointerInstance);
                pointerInstance = null;
                pointerAnimation = null;
            }
        }
    }
    public class WorldPointerVisual : ITutorialWorldVisual
    {
        private TutorialVisualManager visualManager;
        private TutorialSceneReferences sceneReferences; // References to scene components
        internal GameObject pointerInstance; // Instance of pointer visual
        private WorldPointerAnimation pointerAnimation; // Animation controller for pointer

        public void Initialize(TutorialSceneReferences references, TutorialVisualManager visuals)
        {
            sceneReferences = references;
            visualManager = visuals;
        }

        public void Translate(string textValue)
        {
            pointerAnimation.SetText(textValue);
        }

        // Shows pointer visual for step
        public void Show(ClickData step)
        {
            if (step.worldPointerPrefab == null)
            {
                Debug.Log("[ATM] No world pointer prefab assigned for this tutorial step.");
                return;
            }

            if (pointerInstance == null)
            {
                pointerInstance = GameObject.Instantiate(step.worldPointerPrefab,
                                                      sceneReferences.targetWorldHolder);

                visualManager.CheckAndDisableIncorrectAnimationComponent<WorldPointerAnimation>(pointerInstance, "WorldPointerAnimation");

                pointerAnimation = pointerInstance.GetComponent<WorldPointerAnimation>();
                if (pointerAnimation == null)
                {
                    Debug.Log("[ATM] WorldPointerAnimation not found on prefab, adding default configuration.");
                    pointerAnimation = pointerInstance.AddComponent<WorldPointerAnimation>();
                }
                pointerAnimation.Initialize(step, sceneReferences);
            }

            if (pointerAnimation != null)
            {
                pointerAnimation.UpdateTargetObjects(step.GameObjects);
                pointerAnimation.StartAnimation();
            }
        }

        // Hides pointer visual
        public void Hide()
        {
            if (pointerAnimation != null)
            {
                pointerAnimation.FinishAnimation();
            }
        }

        // Destroys pointer visual
        public void Destroy()
        {
            if (pointerInstance != null)
            {
                GameObject.Destroy(pointerInstance);
                pointerInstance = null;
                pointerAnimation = null;
            }
        }
    }
    public class HoverVisual : ITutorialHover
    {
        private TutorialSceneReferences sceneReferences; // References to scene components
        internal GameObject hoverInstance; // Instance of hover visual
        private UIPointerGraphAnimation hoverAnimation; // Animation controller for hover

        // Initializes hover visual with references
        public void Initialize(TutorialSceneReferences references)
        {
            sceneReferences = references;
        }

        // Shows hover visual for step
        public void Show(ClickData step)
        {
            if (step.hoverPrefab == null)
            {
                Debug.Log("[ATM] No hover prefab assigned for this tutorial step.");
                return;
            }

            if (hoverInstance == null)
            {
                hoverInstance = GameObject.Instantiate(step.hoverPrefab,
                                                     sceneReferences.targetCanvas.transform);

                hoverAnimation = hoverInstance.GetComponent<UIPointerGraphAnimation>();
                if (hoverAnimation == null)
                {
                    Debug.Log("[ATM] UIPointerGraphAnimation not found on hover prefab, adding default configuration.");
                    hoverAnimation = hoverInstance.AddComponent<UIPointerGraphAnimation>();
                }
                hoverAnimation.isHover = true;
                hoverAnimation.Initialize(step, sceneReferences);
            }

            if (hoverAnimation != null)
            {
                var targetObject = GetHoverTarget(step);
                if (targetObject != null)
                {
                    var targetList = new List<UnityEngine.Object> { targetObject };
                    hoverAnimation.UpdateTargetObjects(targetList);
                    hoverAnimation.StartAnimation();
                    hoverInstance.SetActive(true);
                }
                else
                {
                    hoverInstance.SetActive(false);
                }
            }
        }

        // Hides hover visual
        public void Hide()
        {
            if (hoverAnimation != null)
            {
                hoverAnimation.FinishAnimation();
            }
        }

        // Destroys hover visual
        public void Destroy()
        {
            if (hoverInstance != null)
            {
                GameObject.Destroy(hoverInstance);
                hoverInstance = null;
                hoverAnimation = null;
            }
        }

        // Gets target object for hover effect
        private UnityEngine.Object GetHoverTarget(ClickData step)
        {
            return step.GameObjects != null && step.GameObjects.Count > 0
                ? step.GameObjects[step.GameObjects.Count - 1]
                : null;
        }
    }
    public class WorldHoverVisual : ITutorialWorldHover
    {
        private TutorialSceneReferences sceneReferences; // References to scene components
        internal GameObject hoverInstance; // Instance of hover visual
        private WorldPointerAnimation hoverAnimation; // Animation controller for hover

        // Initializes hover visual with references
        public void Initialize(TutorialSceneReferences references)
        {
            sceneReferences = references;
        }

        // Shows hover visual for step
        public void Show(ClickData step)
        {
            if (step.worldHoverPrefab == null)
            {
                Debug.Log("[ATM] No world hover prefab assigned for this tutorial step.");
                return;
            }

            if (hoverInstance == null)
            {
                hoverInstance = GameObject.Instantiate(step.worldHoverPrefab,
                                                     sceneReferences.targetWorldHolder);

                hoverAnimation = hoverInstance.GetComponent<WorldPointerAnimation>();
                if (hoverAnimation == null)
                {
                    Debug.Log("[ATM] WorldPointerAnimation not found on hover prefab, adding default configuration.");
                    hoverAnimation = hoverInstance.AddComponent<WorldPointerAnimation>();
                }
                hoverAnimation.isHover = true;
                hoverAnimation.Initialize(step, sceneReferences);
            }

            if (hoverAnimation != null)
            {
                var targetObject = GetHoverTarget(step);
                if (targetObject != null)
                {
                    var targetList = new List<UnityEngine.Object> { targetObject };
                    hoverAnimation.UpdateTargetObjects(targetList);
                    hoverAnimation.StartAnimation();
                    hoverInstance.SetActive(true);
                }
                else
                {
                    hoverInstance.SetActive(false);
                }
            }
        }

        // Hides hover visual
        public void Hide()
        {
            if (hoverAnimation != null)
            {
                hoverAnimation.FinishAnimation();
            }
        }

        // Destroys hover visual
        public void Destroy()
        {
            if (hoverInstance != null)
            {
                GameObject.Destroy(hoverInstance);
                hoverInstance = null;
                hoverAnimation = null;
            }
        }

        // Gets target object for hover effect
        private UnityEngine.Object GetHoverTarget(ClickData step)
        {
            return step.GameObjects != null && step.GameObjects.Count > 0
                ? step.GameObjects[step.GameObjects.Count - 1]
                : null;
        }
    }
    #endregion
}