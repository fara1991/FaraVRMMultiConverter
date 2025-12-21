using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Esperecyan.UniVRMExtensions;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Fara.Fara_VRMMultiConverter.Editor
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
        private bool _isBatchMode;

        // 変換セッション中に新しく生成されたアセットを管理するキャッシュ
        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _assetCopyCache = new();

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
            _isBatchMode = false;
        }

        public void SetBatchMode(bool isBatchMode) => _isBatchMode = isBatchMode;

        public bool ConvertSingleAvatar(GameObject selectedVrcPrefab)
        {
            GameObject vrcAvatarInstance = null;
            GameObject bakedAvatar = null;
            _assetCopyCache.Clear();

            try
            {
                if (!selectedVrcPrefab) return false;

                // 開始前にエディタのキャッシュ（VRMプレビュー等）をクリーンアップ
                Selection.activeObject = null;
                ResetUniVrmCache();
                EditorUtility.UnloadUnusedAssetsImmediate();

                // 1. Hierarchyにインスタンスを生成
                vrcAvatarInstance = UnityEngine.Object.Instantiate(selectedVrcPrefab);
                if (!vrcAvatarInstance) return false;

                string avatarName = selectedVrcPrefab.name.Replace("(Clone)", "").Replace("Variant", "").Trim();
                vrcAvatarInstance.name = avatarName;

                // 不要なオブジェクトの削除
                DeleteSpecificObjects(vrcAvatarInstance);
                DeleteHideObjects(vrcAvatarInstance);

                // 2. ベイク処理（NDMFなどの実行）
                try
                {
                    Debug.Log($"[Bake] {avatarName} のベイクを開始");
                    AvatarProcessor.ProcessAvatar(vrcAvatarInstance);

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

                ConsolidateMaterials(bakedAvatar);
                ProtectAssetsInHierarchy(bakedAvatar, avatarName);

                // NDMF が生成した一時キャッシュを解放してプレハブ保存に備える
                EditorUtility.UnloadUnusedAssetsImmediate();

                // 4. VRM Prefabの保存
                // 既存アセットがある場合はPrefabUtilityが安全に上書きします
                var prefabCreator = new VrmPrefabCreator(_vrmOutputPath);
                var vrmPrefabPath = prefabCreator.CreateVrmPrefab(bakedAvatar);

                // Hierarchy上のオブジェクトは役割を終えたので破棄
                if (bakedAvatar) UnityEngine.Object.DestroyImmediate(bakedAvatar);
                if (vrcAvatarInstance) UnityEngine.Object.DestroyImmediate(vrcAvatarInstance);

                if (string.IsNullOrEmpty(vrmPrefabPath)) return false;

                // 5. VRMセットアップ
                // 生成されたプレハブを一度Unityに再認識させる
                AssetDatabase.ImportAsset(vrmPrefabPath, ImportAssetOptions.ForceUpdate);

                // VRMInitializerを実行。内部でアセットの読み込み・コンポーネント付与・保存が完結します
                VRMInitializer.Initialize(vrmPrefabPath);

                // 6. コピー元のVRMアバターがあれば設定をコピー
                if (_isVrmComponentCopy && _baseVrmPrefab)
                {
                    CopySettingsFromBase(vrmPrefabPath);
                }

                // 7. メタ情報とサムネイルの更新
                VrmMetaUpdater.UpdateMeta(vrmPrefabPath, avatarName, _vrmVersion, _vrmAuthor, _vrmThumbnailPath,
                    _thumbnailResolution, skipRefresh: true);

                // VRM内部アセット（BlendShapeClip等）の整理
                FinalizeVrmInternalAssets(vrmPrefabPath, avatarName);

                if (_isVrmComponentCopy && _baseVrmPrefab)
                {
                    ApplyIsBinarySettings(vrmPrefabPath);
                }

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

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ProtectAssetsInHierarchy(GameObject targetAvatar, string avatarName)
        {
            var generatedAssetFolder = $"{_vrmOutputPath}/{avatarName}.Generated";
            if (!AssetDatabase.IsValidFolder(generatedAssetFolder))
            {
                if (!AssetDatabase.IsValidFolder(_vrmOutputPath))
                {
                    System.IO.Directory.CreateDirectory(_vrmOutputPath);
                    AssetDatabase.ImportAsset(_vrmOutputPath);
                }

                AssetDatabase.CreateFolder(_vrmOutputPath, $"{avatarName}.Generated");
            }

            foreach (var renderer in targetAvatar.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh)
                {
                    skinnedRenderer.sharedMesh = SaveAssetToFile(skinnedRenderer.sharedMesh, generatedAssetFolder);
                }
                else if (renderer is MeshRenderer meshRenderer &&
                         meshRenderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
                {
                    filter.sharedMesh = SaveAssetToFile(filter.sharedMesh, generatedAssetFolder);
                }

                var materials = renderer.sharedMaterials;
                bool isChanged = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    var savedMaterial = SaveAssetToFile(materials[i], generatedAssetFolder);
                    if (savedMaterial != materials[i])
                    {
                        materials[i] = savedMaterial;
                        isChanged = true;
                    }
                }

                if (isChanged) renderer.sharedMaterials = materials;
            }

            AssetDatabase.SaveAssets();
        }

        private T SaveAssetToFile<T>(T sourceAsset, string targetDir) where T : UnityEngine.Object
        {
            if (sourceAsset == null) return null;
            if (_assetCopyCache.TryGetValue(sourceAsset, out var cached) && cached is T typedCached) return typedCached;

            var currentAssetPath = AssetDatabase.GetAssetPath(sourceAsset);
            bool isTemporaryAsset = string.IsNullOrEmpty(currentAssetPath) ||
                                    currentAssetPath.Contains("ZZZ_GeneratedAssets") ||
                                    currentAssetPath.StartsWith("Packages/");
            if (!isTemporaryAsset) return sourceAsset;

            string shortId = sourceAsset.GetInstanceID().ToString("X");
            string extension = sourceAsset is Material ? ".mat" : ".asset";
            string fileName = $"{sourceAsset.name}_{shortId}{extension}";
            string savePath = $"{targetDir}/{fileName}";

            var copyInstance = UnityEngine.Object.Instantiate(sourceAsset);
            copyInstance.name = sourceAsset.name;

            if (copyInstance is Material materialInstance)
            {
                SaveReferencedTextures(materialInstance, targetDir);
            }

            AssetDatabase.CreateAsset(copyInstance, savePath);
            _assetCopyCache[sourceAsset] = copyInstance;
            return copyInstance;
        }

        private void SaveReferencedTextures(Material material, string targetDir)
        {
            var shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture texture = material.GetTexture(propertyName);
                if (texture)
                {
                    var savedTexture = SaveTextureToFile(texture, targetDir);
                    if (savedTexture && savedTexture != texture)
                    {
                        material.SetTexture(propertyName, savedTexture);
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        private Texture SaveTextureToFile(Texture sourceTexture, string targetDir)
        {
            if (sourceTexture == null) return null;
            if (_assetCopyCache.TryGetValue(sourceTexture, out var cached) && cached is Texture typedCached)
                return typedCached;

            var originalAssetPath = AssetDatabase.GetAssetPath(sourceTexture);
            string shortId = sourceTexture.GetInstanceID().ToString("X");
            string extension = string.IsNullOrEmpty(originalAssetPath) ? ".png" : Path.GetExtension(originalAssetPath);
            if (string.IsNullOrEmpty(extension)) extension = ".png";

            string fileName = $"{sourceTexture.name}_{shortId}{extension}";
            string savePath = $"{targetDir}/{fileName}";

            Texture resultTexture = null;
            if (!string.IsNullOrEmpty(originalAssetPath) && File.Exists(originalAssetPath))
            {
                if (AssetDatabase.CopyAsset(originalAssetPath, savePath))
                {
                    AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.SaveAssets();
                    resultTexture = AssetDatabase.LoadAssetAtPath<Texture>(savePath);
                }
            }

            if (!resultTexture)
            {
                var readableTexture = CreateReadableTextureCopy(sourceTexture);
                if (readableTexture)
                {
                    var systemPath = Path.Combine(Application.dataPath, savePath.Substring(7));
                    File.WriteAllBytes(systemPath, readableTexture.EncodeToPNG());
                    AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.SaveAssets();
                    resultTexture = AssetDatabase.LoadAssetAtPath<Texture>(savePath);
                    UnityEngine.Object.DestroyImmediate(readableTexture);
                }
            }

            if (resultTexture) _assetCopyCache[sourceTexture] = resultTexture;
            return resultTexture;
        }

        private Texture2D CreateReadableTextureCopy(Texture source)
        {
            if (!source) return null;
            RenderTexture tempRT = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, tempRT);
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = tempRT;

            Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previousRT;
            RenderTexture.ReleaseTemporary(tempRT);
            return readableTexture;
        }

        private static GameObject FindBakedAvatar(string name)
            => UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .FirstOrDefault(obj => obj.name.Contains(name));

        private void ConsolidateMaterials(GameObject targetAvatar)
        {
            var renderers = targetAvatar.GetComponentsInChildren<Renderer>(true);
            var materialNameMap = new Dictionary<string, Material>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                bool isChanged = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i]) continue;
                    if (materialNameMap.TryGetValue(materials[i].name, out var existingMat))
                    {
                        if (materials[i] == existingMat) continue;
                        materials[i] = existingMat;
                        isChanged = true;
                    }
                    else materialNameMap.Add(materials[i].name, materials[i]);
                }

                if (isChanged) renderer.sharedMaterials = materials;
            }
        }

        private void ResetUniVrmCache()
        {
            try
            {
                var previewType = Type.GetType("VRM.PreviewSceneManager, VRM");
                if (previewType == null) return;
                foreach (var manager in GameObject.FindObjectsOfType(previewType))
                    UnityEngine.Object.DestroyImmediate(((Component) manager).gameObject);
            }
            catch
            {
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
            for (int i = transforms.Length - 1; i >= 0; i--)
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
                    if (baseProxy && baseProxy.BlendShapeAvatar != null)
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
                using var scope = new PrefabUtility.EditPrefabContentsScope(vrmPrefabPath);
                scope.prefabContentsRoot.GetComponent<VRM.VRMBlendShapeProxy>().BlendShapeAvatar =
                    AssetDatabase.LoadAssetAtPath<VRM.BlendShapeAvatar>($"{folderPath}/BlendShape.asset");
            }

            var vrmMeta = vrmPrefab.GetComponent<VRM.VRMMeta>();
            if (vrmMeta && vrmMeta.Meta)
            {
                var metaAssetPath = $"{_vrmOutputPath}/{avatarName}.MetaObject/Meta.asset";
                var finalizedMeta = AssetDatabase.LoadAssetAtPath<VRM.VRMMetaObject>(metaAssetPath);
                if (finalizedMeta)
                {
                    using var scope = new PrefabUtility.EditPrefabContentsScope(vrmPrefabPath);
                    scope.prefabContentsRoot.GetComponent<VRM.VRMMeta>().Meta = finalizedMeta;
                }
            }
        }

        private void ApplyIsBinarySettings(string vrmPrefabPath)
        {
            var vrmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vrmPrefabPath);
            if (!vrmPrefab) return;
            var baseProxy = _baseVrmPrefab.GetComponent<VRM.VRMBlendShapeProxy>();
            var destProxy = vrmPrefab.GetComponent<VRM.VRMBlendShapeProxy>();
            if (!baseProxy || !baseProxy.BlendShapeAvatar || !destProxy || !destProxy.BlendShapeAvatar) return;
            bool changed = false;
            foreach (var baseClip in baseProxy.BlendShapeAvatar.Clips)
            {
                if (baseClip == null) continue;
                var destClip = baseClip.Preset != VRM.BlendShapePreset.Unknown
                    ? destProxy.BlendShapeAvatar.GetClip(baseClip.Preset)
                    : destProxy.BlendShapeAvatar.GetClip(baseClip.BlendShapeName);
                if (destClip != null && destClip.IsBinary != baseClip.IsBinary)
                {
                    destClip.IsBinary = baseClip.IsBinary;
                    EditorUtility.SetDirty(destClip);
                    changed = true;
                }
            }

            if (changed) AssetDatabase.SaveAssets();
        }
    }
}