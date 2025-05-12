using UnityEditor;
using UnityEngine;

namespace AutomaticTutorialMaker
{
    // Custom attribute to mark properties as read-only in the Unity Inspector
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    // Custom property drawer to implement read-only functionality in the Unity Editor
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        // Renders the property field as disabled, preventing user editing
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Temporarily disable GUI interaction
            GUI.enabled = false;
            // Draw the property field
            EditorGUI.PropertyField(position, property, label, true);
            // Restore GUI interaction
            GUI.enabled = true;
        }
    }
#endif
}