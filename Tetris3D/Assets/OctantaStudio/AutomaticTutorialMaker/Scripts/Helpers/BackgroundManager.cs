using UnityEngine;
using UnityEngine.UI;

namespace AutomaticTutorialMaker
{
    internal class BackgroundManager
    {
        private readonly TutorialSceneReferences sceneReferences; // Scene references container
        private GameObject backgroundInstance; // Background overlay instance
        private Image backgroundImage; // Background image component
        private int activeRequests = 0; // Count of active background requests
        private Color targetColor = Color.clear; // Target background color
        private bool isAnimating = false; // Animation state
        private float animationProgress = 0f; // Animation progress
        private bool isFadingOut = false; // Fade out state

        private float FADE_TIME; // Duration of fade animation
        private bool destroyAfterAnimation = false; // Whether to destroy after animation

        // Initializes background manager with scene references
        internal BackgroundManager(TutorialSceneReferences references)
        {
            sceneReferences = references;
        }

        // Initializes background state
        internal void Initialize()
        {
            CleanupBackground();
            activeRequests = 0;
            targetColor = Color.clear;
        }

        // Updates background animation each frame
        internal void Update()
        {
            if (isAnimating)
            {
                UpdateAnimation();
            }
        }

        // Requests background display with color and fade
        internal void RequestBackground(GameObject prefab, Color color, float fadeInDuration)
        {
            if (color.a <= 0) return;

            activeRequests++;

            if (targetColor.a < color.a)
            {
                targetColor = color;
            }

            FADE_TIME = fadeInDuration;

            if (backgroundInstance == null && prefab != null)
            {
                backgroundInstance = Object.Instantiate(prefab, sceneReferences.targetCanvas.transform);
                backgroundInstance.transform.SetAsFirstSibling();
                backgroundImage = backgroundInstance.GetComponent<Image>();
                backgroundImage.color = Color.clear;
            }

            if (backgroundImage != null)
            {
                isAnimating = true;
                isFadingOut = false;
                animationProgress = 0f;
            }
        }

        // Releases background with fade out
        internal void ReleaseBackground(float fadeOutDuration)
        {
            if (activeRequests > 0)
            {
                activeRequests--;

                if (activeRequests == 0 && backgroundInstance != null)
                {
                    FADE_TIME = fadeOutDuration;
                    isAnimating = true;
                    isFadingOut = true;
                    animationProgress = 0f;
                    destroyAfterAnimation = true;
                }
            }
        }

        // Cleans up background instance
        internal void CleanupBackground()
        {
            if (backgroundInstance != null)
            {
                Object.Destroy(backgroundInstance);
                backgroundInstance = null;
                backgroundImage = null;
            }
        }

        // Updates background animation state
        private void UpdateAnimation()
        {
            if (backgroundImage == null) return;

            float t = Mathf.Clamp01(animationProgress / FADE_TIME);

            Color targetValue = new Color(
                targetColor.r,
                targetColor.g,
                targetColor.b,
                isFadingOut ? 0f : targetColor.a
            );

            Color currentColor = backgroundImage.color;

            backgroundImage.color = Color.Lerp(
                isFadingOut ? currentColor : new Color(targetColor.r, targetColor.g, targetColor.b, 0f),
                targetValue,
                t
            );

            bool targetReached = Mathf.Abs(backgroundImage.color.a - targetValue.a) < 0.01f;

            if (targetReached || t >= 1f)
            {
                isAnimating = false;
                if (destroyAfterAnimation && isFadingOut && backgroundInstance != null)
                {
                    Object.Destroy(backgroundInstance);
                    backgroundInstance = null;
                    backgroundImage = null;
                    destroyAfterAnimation = false;
                }
            }

            animationProgress += Time.deltaTime;
        }
    }
}
