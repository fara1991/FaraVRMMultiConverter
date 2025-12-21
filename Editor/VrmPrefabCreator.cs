using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fara.Fara_VRMMultiConverter.Editor
{
    /// <summary>
    /// VRMプレハブの作成を担当するクラス
    /// </summary>
    public class VrmPrefabCreator
    {
        private readonly string _vrmOutputPath;

        public VrmPrefabCreator(string vrmOutputPath)
        {
            _vrmOutputPath = vrmOutputPath;
        }

        public string CreateVrmPrefab(GameObject avatar)
        {
            Debug.Log("=== VRMプレハブの作成開始 ===");

            // ファイルをロックしている可能性のある隠しプレビューマネージャーを破壊
            Selection.activeObject = null;
            ResetUniVrmCache(); 

            var animator = avatar.GetComponent<Animator>();
            if (!animator || !animator.isHuman)
            {
                EditorUtility.DisplayDialog(avatar.name, "HumanoidのAnimatorが必要です", "OK");
                return "";
            }

            // 保存先のパスを取得
            var path = GetPrefabPath(avatar);
            EnsureDirectoryExists(Path.GetDirectoryName(path));

            // 直接プレハブを保存（上書き）。
            // 削除して作り直すよりも、既存のプレハブアセットに対して上書き保存するほうがUnityのGUIDが維持され安定します。
            PrefabUtility.SaveAsPrefabAsset(avatar, path);

            Debug.Log($"✓ VRMプレハブの保存が完了しました: {path}");
            return path;
        }
    
        private void ResetUniVrmCache()
        {
            // VRM 0.x のプレビュー機能をリセット
            var previewType = Type.GetType("VRM.PreviewSceneManager, VRM");
            if (previewType == null) return;
            foreach (var manager in UnityEngine.Object.FindObjectsOfType(previewType)) 
                UnityEngine.Object.DestroyImmediate(((Component)manager).gameObject);
        }

        private string GetPrefabPath(GameObject avatar)
        {
            // すでにプロジェクト内に同じ名前のプレハブがあるか確認
            var path = AssetDatabase.GetAssetPath(avatar);
            if (!string.IsNullOrEmpty(path) && Path.GetExtension(path) == ".prefab")
            {
                return path;
            }

            // なければ出力ディレクトリに作成
            return $"{_vrmOutputPath}/{avatar.name}.prefab";
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return;
            if (Directory.Exists(directory)) return;

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception e)
            {
                Debug.LogError($"ディレクトリの作成に失敗しました: {e.Message}");
                throw;
            }
        }
    }
}