using UnityEngine;

namespace AutomaticTutorialMaker
{
    [CreateAssetMenu(fileName = "New World Pointer Settings", menuName = "Tutorial System/World Pointer Settings")]
    public class WorldPointerSettings : ScriptableObject
    {
        [Header("Target Settings")]
        [Tooltip("The mode that determines how the pointer behaves and positions itself relative to the target.")]
        public PointerMode pointerMode = PointerMode.Geotag;

        [Tooltip("Whether to use the object's pivot points instead of its bounds for positioning the pointer.")]
        public bool usePivots = false;

        [Header("Movement Settings")]
        [Tooltip("The speed at which the pointer moves towards its target, measured in units per second.")]
        public float moveSpeed = 150f;

        [Tooltip("The time it takes to smooth out transitions in the pointer's movement.")]
        public float smoothTime = 0.3f;

        [Tooltip("The distance threshold at which the pointer considers itself to have reached the target position.")]
        public float positionThreshold = 1f;

        [Tooltip("The time the pointer waits on the first object before moving to the next target.")]
        public float delayOnFirstObject = 0.5f;

        [Tooltip("The default delay time between transitions when moving between multiple targets.")]
        public float defaultDelay = 1.5f;

        [Header("Placement Settings")]
        [Tooltip("The distance to position the pointer behind UI elements when in UI mode.")]
        public float behindUiDistance = 10f;

        [Tooltip("Whether the road sign mode should remain fixed relative to the camera's front view.")]
        public bool roadSignFixedFrontCamera = false;

        [Tooltip("The offset applied to the pointer in road sign mode.")]
        public Vector3 roadSignOffset = new Vector3(0, 2f, 8);

        [Tooltip("The offset applied to the pointer in geotag mode.")]
        public Vector3 geotagOffset = new Vector3(0, 1f, 0);

        [Tooltip("The offset applied to the pointer in facade mode.")]
        public Vector3 facadeOffset = new Vector3(0, 0, 2f);

        [Tooltip("The offset applied to the pointer in side mode.")]
        public Vector3 sideOffset = new Vector3(2f, 0, 0);

        [Tooltip("The offset applied to the pointer in ground mode.")]
        public Vector3 groundOffset = new Vector3(0, 0, 0);

        [Tooltip("The square offset applied to the pointer in frame mode.")]
        public float frameOffset = 10f;

        [Header("Rotation Settings")]
        [Tooltip("The speed at which the pointer rotates, measured in degrees per second.")]
        public float rotationSpeed = 200f;

        [Tooltip("The time it takes to smooth out transitions in the pointer's rotation.")]
        public float rotationSmoothTime = 0.5f;

        [Tooltip("The tilt angles (x, y, z) applied to the pointer in road sign mode.")]
        public Vector3 roadTiltAngles = Vector3.zero;

        [Tooltip("Whether the pointer should always face the camera.")]
        public bool faceToCamera;

        [Header("Appearance Settings")]
        [Tooltip("The type of animation to play when the pointer appears.")]
        public WorldAnimationType appearAnimation;

        [Tooltip("The type of animation to play when the pointer disappears.")]
        public WorldAnimationType disappearAnimation;

        [Tooltip("The duration of the appear animation, measured in seconds.")]
        public float appearDuration = 0.7f;

        [Tooltip("The duration of the disappear animation, measured in seconds.")]
        public float disappearDuration = 0.2f;

        [Tooltip("The material used for the fade animation when the pointer appears or disappears.")]
        public Material transparentMaterial;

        [Tooltip("The multiplier for the overshoot effect in the zoom animation.")]
        public float zoomOvershootMultiplier = 1.2f;

        [Tooltip("The number of full revolutions the pointer makes during the spin animation.")]
        public float spinRevolutions = 1;

        [Header("Idle Settings")]
        [Tooltip("The type of animation to play when the pointer is idle.")]
        public IdleWorldAnimationType pointerIdleAnimation;

        [Tooltip("The range of up and down movement in the levitation idle animation.")]
        public float levitationRange = 0.1f;

        [Tooltip("The speed of the levitation idle animation.")]
        public float levitationSpeed = 4f;

        [Tooltip("The direction of movement in the levitation idle animation.")]
        public Vector3 levitationDirection = Vector3.up;

        [Tooltip("The size variation in the pulse idle animation.")]
        public float pulseDelta = 0.1f;

        [Tooltip("The speed of the pulse idle animation.")]
        public float pulseSpeed = 4f;

        [Tooltip("The time interval between blinks in the blink idle animation.")]
        public float blinkInterval = 0.5f;

        [Tooltip("The radius of the orbit movement in the orbit idle animation.")]
        public float orbitRadius = 0.005f;

        [Tooltip("The speed of the orbit rotation in the orbit idle animation.")]
        public float orbitSpeed = 120f;

        [Tooltip("The axis around which the orbit rotation occurs in the orbit idle animation.")]
        public Vector3 orbitAxis = Vector3.up;

        [Tooltip("The height of the bounce in the bounce idle animation.")]
        public float bounceHeight = 0.01f;

        [Tooltip("The speed of the bounce in the bounce idle animation.")]
        public float bounceSpeed = 2f;

        [Tooltip("The speed of the spin rotation in the spin idle animation.")]
        public float spinSpeed = 120f;

        [Tooltip("The axis around which the spin rotation occurs in the spin idle animation.")]
        public Vector3 spinAxis = Vector3.up;

        [Header("Screen Edge Settings")]
        [Tooltip("The padding distance from the screen edges to consider the pointer off-screen.")]
        public float screenEdgePadding = 50f;

        [Tooltip("The behavior of the pointer when it reaches the edge of the screen.")]
        public EdgeBehaviour defaultEdgeBehaviour;

        public enum PointerMode
        {
            [Tooltip("Only updates the position of the pointer.")]
            PositionOnly,

            [Tooltip("Only updates the rotation of the pointer.")]
            RotateOnly,

            [Tooltip("The pointer behaves like a road sign, with specific positioning and rotation.")]
            RoadSign,

            [Tooltip("The pointer floats above the target, similar to a geotag.")]
            Geotag,

            [Tooltip("The pointer appears on building facades, adjusting its position accordingly.")]
            Facade,

            [Tooltip("The pointer positions itself to the side of the target.")]
            Side,

            [Tooltip("The pointer stays on the ground, adjusting its position to match the ground level.")]
            Ground,

            [Tooltip("The pointer outlines the target object with a frame.")]
            Frame
        }

        public enum IdleWorldAnimationType
        {
            [Tooltip("No idle animation is played.")]
            None,

            [Tooltip("The pointer floats up and down while idle.")]
            Levitate,

            [Tooltip("The pointer scales up and down while idle.")]
            Pulse,

            [Tooltip("The pointer fades in and out while idle.")]
            Blink,

            [Tooltip("The pointer rotates around the target while idle.")]
            Orbit,

            [Tooltip("The pointer bounces up and down while idle.")]
            Bounce,

            [Tooltip("The pointer rotates around its own axis while idle.")]
            RotateSpin
        }

        public enum WorldAnimationType
        {
            [Tooltip("No animation is played.")]
            None,

            [Tooltip("The pointer scales in or out when appearing or disappearing.")]
            Zoom,

            [Tooltip("The pointer scales and rotates when appearing or disappearing.")]
            ZoomAndRotate,

            [Tooltip("The pointer fades in or out when appearing or disappearing.")]
            Fade
        }

        public enum EdgeBehaviour
        {
            [Tooltip("The pointer is allowed to go off-screen without any restrictions.")]
            None,

            [Tooltip("The pointer stays on the screen edge when it reaches the edge.")]
            OnEdge,

            [Tooltip("The pointer is disabled when it leaves the screen.")]
            DisableOnExit
        }
    }
}