#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AutomaticTutorialMaker
{
    [CustomEditor(typeof(TutorialSceneReferences))]
    public class TutorialSceneReferencesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.4f, 1f);

            GUILayout.Space(5);
            if (GUILayout.Button("How Do I Use It?", GUILayout.Height(30)))
            {
                Application.OpenURL("https://octanta-studio.gitbook.io/automatic-tutorial-maker-for-unity-in-game-tips");
            }
            GUILayout.Space(10);

            GUI.backgroundColor = originalColor;

            DrawDefaultInspector();
        }
    }
}
#endif