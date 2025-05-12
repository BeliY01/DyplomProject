using UnityEngine;

namespace AutomaticTutorialMaker
{
    [CreateAssetMenu(fileName = "New Pointer Graphic Settings", menuName = "Tutorial System/Pointer Graphic Settings")]
    public class PointerSettings : ScriptableObject
    {
        [Header("Target Settings")]
        [Tooltip("Whether to use pivot points for positioning the pointer. If enabled, the pointer will align with the object's pivot point.")]
        public bool usePivots = true;

        [Header("Movement Settings")]
        [Tooltip("The speed at which the pointer moves towards its target.")]
        public float moveSpeed = 150f;

        [Tooltip("The smoothing time for the pointer's movement. Higher values result in smoother but slower movement.")]
        public float smoothTime = 0.3f;

        [Tooltip("The distance threshold at which the pointer considers itself to have reached the target position.")]
        public float positionThreshold = 1f;

        [Tooltip("The delay before the pointer starts moving to the first target object.")]
        public float delayOnFirstObject = 0.5f;

        [Tooltip("The default delay between movements when switching between multiple target objects.")]
        public float defaultDelay = 0f;

        [Header("Appearance Settings")]
        [Tooltip("The type of animation to play when the pointer appears.")]
        public AnimationType appearAnimation;

        [Tooltip("The type of animation to play when the pointer disappears.")]
        public AnimationType disappearAnimation;

        [Tooltip("The duration of the appear animation.")]
        public float appearDuration = 0.7f;

        [Tooltip("The duration of the disappear animation.")]
        public float disappearDuration = 0.2f;

        [Header("Slide Animation")]
        [Tooltip("The starting corner of the screen for the slide animation.")]
        public ScreenCorner slideStartCorner = ScreenCorner.TopRight;

        [Tooltip("The ending corner of the screen for the slide animation.")]
        public ScreenCorner slideEndCorner = ScreenCorner.TopRight;

        [Tooltip("The offset from the screen edge during the slide animation.")]
        public float screenEdgeOffset = 100f;

        [Header("Idle Animation Settings")]
        [Tooltip("The type of idle animation to play when the pointer is not moving.")]
        public IdleAnimationType pointerIdleAnimation;

        [Tooltip("The range of movement for the levitation idle animation.")]
        public float levitationRange = 3f;

        [Tooltip("The speed of the levitation idle animation.")]
        public float levitationSpeed = 5f;

        [Tooltip("The direction of the levitation idle animation.")]
        public Vector3 levitationDirection = Vector3.up;

        [Tooltip("The size variation for the pulse idle animation.")]
        public float pulseDelta = 0.1f;

        [Tooltip("The speed of the pulse idle animation.")]
        public float pulseSpeed = 4f;

        [Tooltip("The interval between blinks for the blink idle animation.")]
        public float blinkInterval = 0.5f;

        [Tooltip("The duration of the fade animation for the fade idle animation.")]
        public float fadeDuration = 1;

        [Header("Screen Edge Settings")]
        [Tooltip("Whether the pointer should rotate when it goes off-screen.")]
        public bool rotateWhenOffscreen = true;

        [Tooltip("The base rotation angle for the pointer when it is off-screen.")]
        public float baseRotation = 0f;

        [Tooltip("Whether the rotation transition should be smooth when the pointer goes off-screen.")]
        public bool smoothRotation = true;

        [Tooltip("The speed of rotation when the pointer goes off-screen.")]
        public float rotationSpeed = 10f;

        [Tooltip("The padding from the screen edge to consider the pointer off-screen.")]
        public float screenEdgePadding = 10f;
    }

    // Enum defining animation types for pointer
    public enum AnimationType
    {
        [Tooltip("No animation will be played.")]
        None,

        [Tooltip("The pointer will zoom in or out when appearing or disappearing.")]
        Zoom,

        [Tooltip("The pointer will fade in or out when appearing or disappearing.")]
        Fade,

        [Tooltip("The pointer will slide in or out from a specified screen corner when appearing or disappearing.")]
        Slide
    }

    // Enum defining idle animation types for pointer
    public enum IdleAnimationType
    {
        [Tooltip("No idle animation will be played.")]
        None,

        [Tooltip("The pointer will levitate up and down while idle.")]
        Levitate,

        [Tooltip("The pointer will pulse in size while idle.")]
        Pulse,

        [Tooltip("The pointer will blink on and off while idle.")]
        Blink,

        [Tooltip("The pointer will fade in and out while idle.")]
        Fade
    }

    // Enum representing different screen corner positions
    public enum ScreenCorner
    {
        [Tooltip("The top-left corner of the screen.")]
        TopLeft,

        [Tooltip("The top-center of the screen.")]
        TopCenter,

        [Tooltip("The top-right corner of the screen.")]
        TopRight,

        [Tooltip("The middle-left of the screen.")]
        MiddleLeft,

        [Tooltip("The middle-right of the screen.")]
        MiddleRight,

        [Tooltip("The bottom-left corner of the screen.")]
        BottomLeft,

        [Tooltip("The bottom-center of the screen.")]
        BottomCenter,

        [Tooltip("The bottom-right corner of the screen.")]
        BottomRight
    }
}