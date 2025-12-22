using System;
using System.Collections.Generic;
using System.Linq;
using Esperecyan.UniVRMExtensions;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    /// <summary>
    /// VRChatアバターをVRMに変換するロジックを担当するクラス
    /// </summary>
    public class VrmAvatarConverter
    {
        private readonly string _vrmOutputPath;
        private readonly string _vrmThumbnailPath;
        private readonly string _vrmVersion;
        private readonly string _vrmAuthor;
        private readonly int _thumbnailResolution;
        private readonly bool _isVrmComponentCopy;
        private readonly GameObject _baseVrmPrefab;
        private readonly DeleteSpecificObjectsSettings _deleteSettings;

        public VrmAvatarConverter(
            string vrmOutputPath,
            string vrmThumbnailPath,
            string vrmVersion,
            string vrmAuthor,
            int thumbnailResolution,
            bool isVrmComponentCopy,
            GameObject baseVrmPrefab,
            DeleteSpecificObjectsSettings settings)
        {
            _vrmOutputPath = vrmOutputPath;
            _vrmThumbnailPath = vrmThumbnailPath;
            _vrmVersion = vrmVersion;
            _vrmAuthor = vrmAuthor;
            _thumbnailResolution = thumbnailResolution;
            _isVrmComponentCopy = isVrmComponentCopy;
            _baseVrmPrefab = baseVrmPrefab;
            _deleteSettings = settings;
        }

        public bool ConvertSingleAvatar(GameObject selectedVrcPrefab)
        {
            GameObject vrcAvatarInstance = null;
            GameObject bakedAvatar = null;
            try
            {
                if (!selectedVrcPrefab) return false;

                EditorUtility.UnloadUnusedAssetsImmediate();

                // 1. Hierarchyにインスタンスを生成
                vrcAvatarInstance = UnityEngine.Object.Instantiate(selectedVrcPrefab);
                if (!vrcAvatarInstance) return false;

                var avatarName = selectedVrcPrefab.name.Replace("(Clone)", "").Replace("Variant", "").Trim();
                vrcAvatarInstance.name = avatarName;

                // 不要なオブジェクトの削除
                DeleteSpecificObjects(vrcAvatarInstance);
                DeleteHideObjects(vrcAvatarInstance);

                // 2. ベイク処理（NDMFなどの実行）
                try
                {
                    Debug.Log($"[Bake] {avatarName} のベイクを開始");
                    var uniqueTempDir = $"Assets/ZZZ_GeneratedAssets_{avatarName}_{Guid.NewGuid().ToString()[..8]}";

                    using (new OverrideTemporaryDirectoryScope(uniqueTempDir))
                    {
                        AvatarProcessor.ProcessAvatar(vrcAvatarInstance);
                    }

                    bakedAvatar = FindBakedAvatar(vrcAvatarInstance.name);
                    if (!bakedAvatar) return false;
                }
                catch (Exception bakeException)
                {
                    Debug.LogError($"ベイク中にエラーが発生しました: {bakeException.Message}");
                    return false;
                }

                // 3. VRM変換の準備
                // 物理オブジェクトの整理とアセットの永続化
                DeleteUtility.DeleteVrcComponentsRecursive(bakedAvatar);
                bakedAvatar.name = avatarName;
                bakedAvatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                // NDMF が生成した一時キャッシュを解放してプレハブ保存に備える
                EditorUtility.UnloadUnusedAssetsImmediate();

                // 4. VRM Prefabの保存
                // 既存アセットがある場合はPrefabUtilityが安全に上書きします
                var prefabCreator = new VrmPrefabCreator(_vrmOutputPath);
                var vrmPrefabPath = prefabCreator.CreateVrmPrefab(bakedAvatar);

                if (string.IsNullOrEmpty(vrmPrefabPath)) return false;

                // 5. VRMセットアップ
                // 生成されたプレハブを一度Unityに再認識させる
                AssetDatabase.ImportAsset(vrmPrefabPath, ImportAssetOptions.ForceUpdate);
                VRMInitializer.Initialize(vrmPrefabPath);

                // 6. コピー元のVRMアバターがあれば設定をコピー
                if (_isVrmComponentCopy && _baseVrmPrefab) CopySettingsFromBase(vrmPrefabPath);

                UpdateAndFinalizeVrm(vrmPrefabPath, avatarName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"VRM変換失敗: {e}");
                return false;
            }
            finally
            {
                // 異常終了時も確実にHierarchyを掃除する
                if (vrcAvatarInstance) UnityEngine.Object.DestroyImmediate(vrcAvatarInstance);
                if (bakedAvatar) UnityEngine.Object.DestroyImmediate(bakedAvatar);
            }
        }

        private static GameObject FindBakedAvatar(string name)
        {
            // 名前が完全一致するルートオブジェクトを優先的に探す
            var rootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjs)
            {
                if (obj.name == name) return obj;
            }

            // 見つからなければ部分一致
            return rootObjs.FirstOrDefault(obj => obj.name.Contains(name));
        }

        private void UpdateAndFinalizeVrm(string vrmPrefabPath, string avatarName)
        {
            // まずメタ情報を更新（サムネイル等の割り当て）
            VrmMetaUpdater.UpdateMeta(
                vrmPrefabPath, avatarName, _vrmVersion, _vrmAuthor, _vrmThumbnailPath,　_thumbnailResolution);

            // 次に内部アセット（BlendShape等）を物理ファイル化し、
            // 最後に1回だけPrefabの参照を書き換えて保存する
            FinalizeVrmInternalAssets(vrmPrefabPath, avatarName);

            if (_isVrmComponentCopy && _baseVrmPrefab)
            {
                ApplyIsBinarySettings(vrmPrefabPath);
            }
        }

        private void DeleteSpecificObjects(GameObject target)
        {
            if (!_deleteSettings) return;
            var names = _deleteSettings.targetObjectNames.Where(n => !string.IsNullOrEmpty(n)).ToList();
            if (names.Count == 0) return;
            DeleteUtility.DeleteSpecificObjectsRecursive(target.transform, names);
        }

        private static void DeleteHideObjects(GameObject target)
        {
            var transforms = target.GetComponentsInChildren<Transform>(true);
            for (var i = transforms.Length - 1; i >= 0; i--)
            {
                var transformInstance = transforms[i];
                if (transformInstance && transformInstance.gameObject != target &&
                    !transformInstance.gameObject.activeSelf)
                    UnityEngine.Object.DestroyImmediate(transformInstance.gameObject);
            }
        }

        private void CopySettingsFromBase(string vrmPrefabPath)
        {
            var destinationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vrmPrefabPath);
            if (destinationPrefab)
            {
                CopyVRMSettings.Copy(_baseVrmPrefab, destinationPrefab, CopyVRMSettings.SupportedComponents.ToList());
            }
        }

        private void FinalizeVrmInternalAssets(string vrmPrefabPath, string avatarName)
        {
            var vrmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vrmPrefabPath);
            if (!vrmPrefab) return;

            VRM.BlendShapeAvatar finalizedBlendShapeAvatar = null;
            var blendProxy = vrmPrefab.GetComponent<VRM.VRMBlendShapeProxy>();
            if (blendProxy && blendProxy.BlendShapeAvatar)
            {
                var folderPath = $"{_vrmOutputPath}/{avatarName}.BlendShapes";
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder(_vrmOutputPath, $"{avatarName}.BlendShapes");
                var clips = new List<VRM.BlendShapeClip>();
                var baseProxy = _baseVrmPrefab?.GetComponent<VRM.VRMBlendShapeProxy>();
                foreach (var sourceClip in blendProxy.BlendShapeAvatar.Clips)
                {
                    if (!sourceClip) continue;
                    var clipSavePath = $"{folderPath}/{sourceClip.name}.asset";
                    var clipCopy = UnityEngine.Object.Instantiate(sourceClip);
                    if (baseProxy && baseProxy.BlendShapeAvatar)
                    {
                        var baseClip = sourceClip.Preset != VRM.BlendShapePreset.Unknown
                            ? baseProxy.BlendShapeAvatar.GetClip(sourceClip.Preset)
                            : baseProxy.BlendShapeAvatar.GetClip(sourceClip.BlendShapeName);
                        if (baseClip) clipCopy.IsBinary = baseClip.IsBinary;
                    }

                    AssetDatabase.CreateAsset(clipCopy, clipSavePath);
                    clips.Add(AssetDatabase.LoadAssetAtPath<VRM.BlendShapeClip>(clipSavePath));
                }

                var newAvatar = ScriptableObject.CreateInstance<VRM.BlendShapeAvatar>();
                newAvatar.Clips = clips;

                AssetDatabase.CreateAsset(newAvatar, $"{folderPath}/BlendShape.asset");
                AssetDatabase.SaveAssets();
                finalizedBlendShapeAvatar =
                    AssetDatabase.LoadAssetAtPath<VRM.BlendShapeAvatar>($"{folderPath}/BlendShape.asset");
            }

            var metaAssetPath = $"{_vrmOutputPath}/{avatarName}.MetaObject/Meta.asset";
            var finalizedMeta = AssetDatabase.LoadAssetAtPath<VRM.VRMMetaObject>(metaAssetPath);

            if (!finalizedBlendShapeAvatar && !finalizedMeta) return;

            using var scope = new PrefabUtility.EditPrefabContentsScope(vrmPrefabPath);
            if (finalizedBlendShapeAvatar)
            {
                scope.prefabContentsRoot.GetComponent<VRM.VRMBlendShapeProxy>().BlendShapeAvatar =
                    finalizedBlendShapeAvatar;
            }

            if (!finalizedMeta) return;

            var metaComp = scope.prefabContentsRoot.GetComponent<VRM.VRMMeta>();
            if (metaComp) metaComp.Meta = finalizedMeta;
        }

        private void ApplyIsBinarySettings(string vrmPrefabPath)
        {
            var vrmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vrmPrefabPath);
            if (!vrmPrefab) return;
            var baseProxy = _baseVrmPrefab.GetComponent<VRM.VRMBlendShapeProxy>();
            var destProxy = vrmPrefab.GetComponent<VRM.VRMBlendShapeProxy>();
            if (!baseProxy || !baseProxy.BlendShapeAvatar || !destProxy || !destProxy.BlendShapeAvatar) return;
            var changed = false;
            foreach (var baseClip in baseProxy.BlendShapeAvatar.Clips)
            {
                if (!baseClip) continue;

                var destClip = baseClip.Preset != VRM.BlendShapePreset.Unknown
                    ? destProxy.BlendShapeAvatar.GetClip(baseClip.Preset)
                    : destProxy.BlendShapeAvatar.GetClip(baseClip.BlendShapeName);
                if (!destClip || destClip.IsBinary == baseClip.IsBinary) continue;

                destClip.IsBinary = baseClip.IsBinary;
                EditorUtility.SetDirty(destClip);
                changed = true;
            }

            if (changed) AssetDatabase.SaveAssets();
        }
    }
}