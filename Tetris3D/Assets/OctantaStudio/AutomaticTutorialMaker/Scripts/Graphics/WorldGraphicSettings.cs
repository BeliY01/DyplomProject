using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AutomaticTutorialMaker.WorldPointerSettings;

namespace AutomaticTutorialMaker
{
    [CreateAssetMenu(fileName = "New World Graphic Settings", menuName = "Tutorial System/World Graphic Settings")]
    public class WorldGraphicSettings : ScriptableObject
    {
        [Header("Appearance Settings")]
        [Tooltip("Animation type when the graphic appears in the world space")]
        public WorldAnimationType appearAnimation = WorldAnimationType.Zoom;

        [Tooltip("Animation type when the graphic disappears from the world space")]
        public WorldAnimationType disappearAnimation = WorldAnimationType.Zoom;

        [Tooltip("Duration (in seconds) of the appear animation")]
        public float appearDuration = 0.3f;

        [Tooltip("Duration (in seconds) of the disappear animation")]
        public float disappearDuration = 0.2f;

        [Tooltip("Material used for fade animations (should be a transparent material)")]
        public Material transparentMaterial;

        [Tooltip("Number of full rotations during spin animation")]
        public float spinRevolutions = 1;

        [Header("Placement Settings")]
        [Tooltip("Behavior for graphic placement in relation to the camera")]
        public PlacementBehaviour placementBehaviour;

        [Tooltip("Distance from camera when using front placement")]
        public float frontCameraDistance = 10;

        [Tooltip("Anchor position relative to screen when placed in front of camera")]
        public AnchorPosition frontAnchorPosition = AnchorPosition.MiddleCenter;

        [Tooltip("Margin in pixels from screen edges for front placement")]
        public float marginPixels = 50f;

        [Header("Rotation Settings")]
        [Tooltip("Should the graphic always face the main camera?")]
        public bool faceToCamera = false;

        [Header("Idle Animation Settings")]
        [Tooltip("Type of animation to play while graphic is idle")]
        public IdleWorldGraphicAnimationType graphicIdleAnimation = IdleWorldGraphicAnimationType.None;

        [Tooltip("Speed of vertical floating motion for Levitate animation")]
        public float levitationSpeed = 5f;

        [Tooltip("Direction vector for levitation movement")]
        public Vector3 levitationDirection = Vector3.up;

        [Tooltip("Maximum distance for levitation movement from start position")]
        public float levitationRange = 3f;

        [Tooltip("Size variation amount for Pulse animation")]
        public float pulseDelta = 0.1f;

        [Tooltip("Speed of scaling animation for Pulse effect")]
        public float pulseSpeed = 4f;

        [Tooltip("Time between visibility toggles for Blink animation")]
        public float blinkInterval = 0.5f;

        [Tooltip("Duration of each blink state (visible/hidden)")]
        public float blinkDuration = 0.2f;

        [Tooltip("Rotation speed for Spin animation (degrees per second)")]
        public float spinSpeed = 120f;

        [Tooltip("Axis of rotation for Spin animation")]
        public Vector3 spinAxis = Vector3.up;

        [Header("Screen Edge Settings")]
        [Tooltip("Safe area padding from screen edges in pixels")]
        public float screenEdgePadding = 50f;

        [Tooltip("Behavior when graphic reaches screen boundaries")]
        public EdgeGraphicBehaviour defaultEdgeBehaviour;

        public enum IdleWorldGraphicAnimationType
        {
            [Tooltip("No idle animation")]
            None,

            [Tooltip("Float up and down continuously")]
            Levitate,

            [Tooltip("Pulse scale rhythmically")]
            Pulse,

            [Tooltip("Blink visibility periodically")]
            Blink,

            [Tooltip("Continuous rotation around specified axis")]
            RotateSpin
        }

        public enum RotationMode
        {
            [Tooltip("No special rotation behavior")]
            None,

            [Tooltip("Always face towards camera")]
            FaceToCamera,

            [Tooltip("Maintain parallel orientation to camera")]
            ParallelToCamera
        }

        public enum EdgeGraphicBehaviour
        {
            [Tooltip("Allow graphic to move off-screen")]
            None,

            [Tooltip("Disable graphic when it exits screen boundaries")]
            DisableOnExit
        }

        public enum PlacementBehaviour
        {
            [Tooltip("Use world-space positioning")]
            None,

            [Tooltip("Keep positioned in front of camera")]
            FrontOfCamera
        }

        public enum AnchorPosition
        {
            [Tooltip("Top-left corner of screen")]
            TopLeft,

            [Tooltip("Top-center edge of screen")]
            TopCenter,

            [Tooltip("Top-right corner of screen")]
            TopRight,

            [Tooltip("Middle-left edge of screen")]
            MiddleLeft,

            [Tooltip("Center of screen")]
            MiddleCenter,

            [Tooltip("Middle-right edge of screen")]
            MiddleRight,

            [Tooltip("Bottom-left corner of screen")]
            BottomLeft,

            [Tooltip("Bottom-center edge of screen")]
            BottomCenter,

            [Tooltip("Bottom-right corner of screen")]
            BottomRight
        }
    }
}