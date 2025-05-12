using UnityEngine;

namespace AutomaticTutorialMaker
{
    // Enum representing different canvas anchor positions
    public enum CanvasAnchor
    {
        [Tooltip("Top-left corner of the canvas")]
        TopLeft,

        [Tooltip("Top-center edge of the canvas")]
        TopCenter,

        [Tooltip("Top-right corner of the canvas")]
        TopRight,

        [Tooltip("Middle-left edge of the canvas")]
        MiddleLeft,

        [Tooltip("Center of the canvas")]
        Center,

        [Tooltip("Middle-right edge of the canvas")]
        MiddleRight,

        [Tooltip("Bottom-left corner of the canvas")]
        BottomLeft,

        [Tooltip("Bottom-center edge of the canvas")]
        BottomCenter,

        [Tooltip("Bottom-right corner of the canvas")]
        BottomRight
    }

    // ScriptableObject for managing tutorial graphic settings
    [CreateAssetMenu(fileName = "New Graphic Settings", menuName = "Tutorial System/Graphic Settings")]
    public class GraphicSettings : ScriptableObject
    {
        [Header("Appearance Settings")]
        [Tooltip("Type of animation to play when the graphic appears")]
        public AnimationType appearAnimation = AnimationType.Zoom;

        [Tooltip("Type of animation to play when the graphic disappears")]
        public AnimationType disappearAnimation = AnimationType.Zoom;

        [Tooltip("Duration (in seconds) of the appear animation")]
        public float appearDuration = 0.3f;

        [Tooltip("Duration (in seconds) of the disappear animation")]
        public float disappearDuration = 0.2f;

        [Header("Slide Animation")]
        [Tooltip("Starting corner of the screen for slide animation")]
        public ScreenCorner slideStartCorner = ScreenCorner.TopRight;

        [Tooltip("Ending corner of the screen for slide animation")]
        public ScreenCorner slideEndCorner = ScreenCorner.TopRight;

        [Tooltip("Offset (in pixels) from the screen edge during slide animation")]
        public float screenEdgeOffset = 100f;

        [Header("Idle Animation Settings")]
        [Tooltip("Type of animation to play while the graphic is idle")]
        public IdleAnimationType graphicIdleAnimation = IdleAnimationType.None;

        [Tooltip("Maximum distance for levitation movement from the start position")]
        public float levitationRange = 3f;

        [Tooltip("Speed of vertical floating motion for Levitate animation")]
        public float levitationSpeed = 5f;

        [Tooltip("Direction vector for levitation movement")]
        public Vector3 levitationDirection = Vector3.up;

        [Tooltip("Size variation amount for Pulse animation")]
        public float pulseDelta = 0.1f;

        [Tooltip("Speed of scaling animation for Pulse effect")]
        public float pulseSpeed = 4f;

        [Tooltip("Duration (in seconds) of the fade animation cycle")]
        public float fadeDuration = 1f;

        [Tooltip("Time between visibility toggles for Blink animation")]
        public float blinkInterval = 0.5f;

        [Tooltip("Duration of each blink state (visible/hidden)")]
        public float blinkDuration = 0.2f;
    }
}