using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    [CreateAssetMenu(fileName = "DeleteSpecificObjectsSettings", menuName = "Fara/DeleteSpecificObjectsSettings")]
    public class DeleteSpecificObjectsSettings : ScriptableObject
    {
        [Tooltip("削除したいオブジェクトの名前を追加してください")] public List<string> targetObjectNames = new();
        [Tooltip("正規表現を使用してマッチングします（* を任意の文字列として扱えます）")] public bool useRegex;
    }

    /// <summary>
    /// DeleteSpecificObjectsSettingsのカスタムエディタ
    /// </summary>
    [CustomEditor(typeof(DeleteSpecificObjectsSettings))]
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

            EditorGUILayout.HelpBox("削除対象のオブジェクト名を管理します", MessageType.Info);
            EditorGUILayout.PropertyField(_useRegexProperty, new GUIContent("正規表現モードを使用"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_targetObjectNamesProperty, new GUIContent("削除対象のオブジェクト名"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"登録数: {_targetObjectNamesProperty.arraySize}個", EditorStyles.boldLabel);

            if (!serializedObject.hasModifiedProperties) return;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}