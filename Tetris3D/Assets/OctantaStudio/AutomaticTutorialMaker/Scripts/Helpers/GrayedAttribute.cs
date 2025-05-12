using UnityEngine;
using UnityEditor;

namespace AutomaticTutorialMaker
{
    // Custom attribute for grayed out properties
    public class GrayedAttribute : PropertyAttribute
    {
        public GrayedAttribute() { }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(GrayedAttribute))]

    // Custom property drawer for grayed attribute
    public class GrayedPropertyDrawer : PropertyDrawer
    {
        // Draws grayed out property in inspector
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.color = originalColor;
        }
    }
#endif
}