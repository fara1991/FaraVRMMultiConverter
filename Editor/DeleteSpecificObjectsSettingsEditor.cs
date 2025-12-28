using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    /// <summary>
    /// DeleteSpecificObjectsSettingsのカスタムエディタ
    /// </summary>
    [CustomEditor(typeof(DeleteSpecificObjectsSettings))]
    [ExcludeFromCodeCoverage]
    public class DeleteSpecificObjectsSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetObjectNamesProperty;
        private SerializedProperty _useRegexProperty;

        private void OnEnable()
        {
            _targetObjectNamesProperty = serializedObject.FindProperty("targetObjectNames");
            _useRegexProperty = serializedObject.FindProperty("useRegex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(L10N.Converter.SettingsEditor.HelpBox, MessageType.Info);
            EditorGUILayout.PropertyField(_useRegexProperty, new GUIContent(L10N.Converter.SettingsEditor.UseRegex));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_targetObjectNamesProperty, new GUIContent(L10N.Converter.SettingsEditor.TargetObjectNames), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L10N.Converter.SettingsEditor.RegisteredCount(_targetObjectNamesProperty.arraySize), EditorStyles.boldLabel);

            if (!serializedObject.hasModifiedProperties) return;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}