using UnityEditor;
using UnityEngine;

namespace ImpostorPlugin.Editor
{
    /// <summary>
    /// Custom Editor for the Impostor component.
    /// </summary>
    [CustomEditor(typeof(Impostor))]
    public class ImpostorEditor : UnityEditor.Editor
    {
        private SerializedProperty _atlasResolutionProp;
        private SerializedProperty _framesCountProp;
        private SerializedProperty _framePaddingProp;
        private SerializedProperty _isHemiSphereProp;

        private static readonly int[] AtlasResolutionOptions = { 512, 1024, 2048, 4096, 8192 };
        private static readonly GUIContent[] AtlasResolutionLabels = 
        {
            new GUIContent("512"),
            new GUIContent("1024"),
            new GUIContent("2048"),
            new GUIContent("4096"),
            new GUIContent("8192")
        };

        private void OnEnable()
        {
            _atlasResolutionProp = serializedObject.FindProperty("_atlasResolution");
            _framesCountProp = serializedObject.FindProperty("_framesCount");
            _framePaddingProp = serializedObject.FindProperty("_framePadding");
            _isHemiSphereProp = serializedObject.FindProperty("_isHemiSphere");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAtlasSettings();
            EditorGUILayout.Space(5);
            DrawBakingSettings();
            EditorGUILayout.Space(10);
            DrawBakeButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAtlasSettings()
        {
            int currentIndex = System.Array.IndexOf(AtlasResolutionOptions, _atlasResolutionProp.intValue);
            if (currentIndex < 0) currentIndex = 2;
            
            int newIndex = EditorGUILayout.Popup(new GUIContent("Atlas Resolution"), currentIndex, AtlasResolutionLabels);
            
            if (newIndex != currentIndex)
            {
                _atlasResolutionProp.intValue = AtlasResolutionOptions[newIndex];
            }

            EditorGUILayout.PropertyField(_framesCountProp, new GUIContent("Frames Count"));
            
            if (_framesCountProp.intValue < 2)
            {
                _framesCountProp.intValue = 2;
            }
            if (_framesCountProp.intValue % 2 != 0)
            {
                _framesCountProp.intValue--;
            }
        }

        private void DrawBakingSettings()
        {
            EditorGUILayout.PropertyField(_framePaddingProp, new GUIContent("Frame Padding"));
            EditorGUILayout.PropertyField(_isHemiSphereProp, new GUIContent("Hemisphere"));
        }

        private void DrawBakeButton()
        {
            var impostor = (Impostor)target;
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };
            
            if (GUILayout.Button("Bake Impostor", buttonStyle))
            {
                Undo.RecordObject(impostor, "Bake Impostor");
                impostor.Bake();
                EditorUtility.SetDirty(impostor);
            }
            
            GUI.backgroundColor = originalColor;
        }
    }
}
