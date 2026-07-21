#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EasyGradient.UIToolkit
{
    /// <summary>
    /// Custom inspector for GradientStyle: gradient preview, adding/removing
    /// color stops, position sliders.
    /// </summary>
    [CustomEditor(typeof(GradientStyle))]
    public class GradientStyleEditor : Editor
    {
        private const float PreviewHeight = 32f;
        private const int PreviewSamples = 64;

        private SerializedProperty _stopsProp;
        private SerializedProperty _angleProp;
        private SerializedProperty _modeProp;

        private void OnEnable()
        {
            _stopsProp = serializedObject.FindProperty("stops");
            _angleProp = serializedObject.FindProperty("angle");
            _modeProp = serializedObject.FindProperty("mode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPreview((GradientStyle)target);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_modeProp);
            if ((GradientMode)_modeProp.enumValueIndex == GradientMode.Linear)
                EditorGUILayout.PropertyField(_angleProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Stops", EditorStyles.boldLabel);
            DrawStops();

            if (GUILayout.Button("+ Add Stop"))
                AddStop();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStops()
        {
            for (int i = 0; i < _stopsProp.arraySize; i++)
            {
                var stopProp = _stopsProp.GetArrayElementAtIndex(i);
                var colorProp = stopProp.FindPropertyRelative("color");
                var positionProp = stopProp.FindPropertyRelative("position");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(56));
                EditorGUILayout.Slider(positionProp, 0f, 1f, GUIContent.none);

                using (new EditorGUI.DisabledScope(_stopsProp.arraySize <= 1))
                {
                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        _stopsProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break; // the array size changed, don't continue this GUI pass
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddStop()
        {
            int newIndex = _stopsProp.arraySize;
            _stopsProp.InsertArrayElementAtIndex(newIndex);
            var newStop = _stopsProp.GetArrayElementAtIndex(newIndex);
            newStop.FindPropertyRelative("color").colorValue = Color.white;
            newStop.FindPropertyRelative("position").floatValue = 1f;
        }

        private void DrawPreview(GradientStyle style)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(PreviewHeight));
            var sorted = style.GetSortedStops();
            if (sorted.Length == 0) return;

            for (int i = 0; i < PreviewSamples; i++)
            {
                float t0 = i / (float)PreviewSamples;
                float t1 = (i + 1) / (float)PreviewSamples;
                Rect slice = new Rect(rect.x + rect.width * t0, rect.y, rect.width * (t1 - t0) + 1f, rect.height);
                EditorGUI.DrawRect(slice, style.Evaluate((t0 + t1) * 0.5f));
            }
        }
    }
}
#endif
