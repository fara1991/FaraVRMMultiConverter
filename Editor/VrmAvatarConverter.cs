using System;
using System.Collections.Generic;
using System.Linq;
using Esperecyan.UniVRMExtensions;
using UnityEditor;
using UnityEngine;
using VRM;

namespace Fara.FaraMultiVrmConverter.Editor
{
    /// <summary>
    /// VRChatアバターをVRMに変換し、アセットをプレハブ内に埋め込むロジックを担当するクラス
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
        private readonly IVrmBakeProcessor _bakeProcessor;

        public VrmAvatarConverter(
            string vrmOutputPath,
            string vrmThumbnailPath,
            string vrmVersion,
            string vrmAuthor,
            int thumbnailResolution,
            bool isVrmComponentCopy,
            GameObject baseVrmPrefab,
            DeleteSpecificObjectsSettings settings,
            IVrmBakeProcessor bakeProcessor = null)
        {
            _vrmOutputPath = vrmOutputPath;
            _vrmThumbnailPath = vrmThumbnailPath;
            _vrmVersion = vrmVersion;
            _vrmAuthor = vrmAuthor;
            _thumbnailResolution = thumbnailResolution;
            _isVrmComponentCopy = isVrmComponentCopy;
            _baseVrmPrefab = baseVrmPrefab;
            _deleteSettings = settings;
            _bakeProcessor = bakeProcessor ?? new DefaultVrmBakeProcessor();
        }

        public bool ConvertSingleAvatar(GameObject selectedVrcPrefab)
        {
            GameObject vrcAvatarInstance = null;
            GameObject bakedAvatar = null;
            try
            {
                if (!selectedVrcPrefab) return false;

                EditorUtility.UnloadUnusedAssetsImmediate();

                vrcAvatarInstance = UnityEngine.Object.Instantiate(selectedVrcPrefab);
                var avatarName = selectedVrcPrefab.name.Replace("(Clone)", "").Replace("Variant", "").Trim();
                vrcAvatarInstance.name = avatarName;

                DeleteSpecificObjects(vrcAvatarInstance);
                DeleteHideObjects(vrcAvatarInstance);

                // 1. ベイク処理
                bakedAvatar = _bakeProcessor.ProcessBake(vrcAvatarInstance, avatarName);

                // 2. VRM変換ロジックの実行
                return ConvertBakedAvatar(bakedAvatar, avatarName);
            }
            catch (Exception e)
            {
                Debug.LogError(L10N.Converter.Errors.ConversionError(e.Message));
                return false;
            }
            finally
            {
                if (vrcAvatarInstance) UnityEngine.Object.DestroyImmediate(vrcAvatarInstance);
                if (bakedAvatar) UnityEngine.Object.DestroyImmediate(bakedAvatar);
            }
        }

        /// <summary>
        /// ベイク済みのオブジェクトを受け取り、VRMへの最終セットアップを行う
        /// </summary>
        public bool ConvertBakedAvatar(GameObject bakedAvatar, string avatarName)
        {
            try
            {
                DeleteUtility.DeleteVrcComponentsRecursive(bakedAvatar);
                bakedAvatar.name = avatarName;
                bakedAvatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                // 出力フォルダの準備
                var generatedFolderName = $"{avatarName}.Generated";
                var generatedFolderPath = $"{_vrmOutputPath}/{generatedFolderName}";

                if (AssetDatabase.IsValidFolder(generatedFolderPath)) AssetDatabase.DeleteAsset(generatedFolderPath);

                AssetDatabase.CreateFolder(_vrmOutputPath, generatedFolderName);

                // アセットの永続化
                PersistAssetsToFolder(bakedAvatar, generatedFolderPath);

                // VRM Prefabの保存
                var prefabCreator = new VrmPrefabCreator(_vrmOutputPath);
                var vrmPrefabPath = prefabCreator.CreateVrmPrefab(bakedAvatar);

                // VRMセットアップ
                AssetDatabase.ImportAsset(vrmPrefabPath, ImportAssetOptions.ForceUpdate);
                VRMInitializer.Initialize(vrmPrefabPath);
                AssetDatabase.Refresh();

                var vrmPrefab = PrefabUtility.LoadPrefabContents(vrmPrefabPath);

                if (_isVrmComponentCopy && _baseVrmPrefab) CopySettingsFromBase(vrmPrefab);
                VrmMetaUpdater.UpdateMeta(
                    vrmPrefab,
                    vrmPrefabPath,
                    avatarName,
                    _vrmVersion,
                    _vrmAuthor,
                    _vrmThumbnailPath,
                    _thumbnailResolution
                );
                FinalizeVrmInternalAssets(vrmPrefab, avatarName);

                PrefabUtility.SaveAsPrefabAsset(vrmPrefab, vrmPrefabPath);
                PrefabUtility.UnloadPrefabContents(vrmPrefab);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(L10N.Converter.Errors.ConversionError(e.Message));
                return false;
            }
        }

        private static void PersistAssetsToFolder(GameObject avatar, string folderPath)
        {
            var processedAssets = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

            foreach (var renderer in avatar.GetComponentsInChildren<Renderer>(true))
            {
                // メッシュの処理
                Mesh srcMesh = null;
                if (renderer is SkinnedMeshRenderer smr)
                {
                    srcMesh = smr.sharedMesh;
                }
                else
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter is not null) srcMesh = meshFilter.sharedMesh;
                }

                if (srcMesh is not null && IsTemporaryAsset(srcMesh))
                {
                    if (!processedAssets.TryGetValue(srcMesh, out var dstMesh))
                    {
                        var cleanMeshName = CleanName(srcMesh.name);
                        var path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{cleanMeshName}_mesh.asset");
                        dstMesh = UnityEngine.Object.Instantiate(srcMesh);
                        AssetDatabase.CreateAsset(dstMesh, path);
                        processedAssets[srcMesh] = dstMesh;
                    }

                    if (renderer is SkinnedMeshRenderer s) s.sharedMesh = (Mesh) dstMesh;
                    else renderer.GetComponent<MeshFilter>().sharedMesh = (Mesh) dstMesh;
                }

                // マテリアルの処理
                var sharedMats = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < sharedMats.Length; i++)
                {
                    var mat = sharedMats[i];
                    if (mat is null) continue;
                    if (!IsTemporaryAsset(mat)) continue;

                    if (!processedAssets.TryGetValue(mat, out var dstMat))
                    {
                        var cleanMatName = CleanName(mat.name);
                        var path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{cleanMatName}.mat");
                        var newMat = new Material(mat);

                        // マテリアルが参照している一時テクスチャも書き出す
                        PersistMaterialTextures(newMat, folderPath, processedAssets);

                        AssetDatabase.CreateAsset(newMat, path);
                        dstMat = newMat;
                        processedAssets[mat] = dstMat;
                    }

                    sharedMats[i] = (Material) dstMat;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = sharedMats;
            }

            AssetDatabase.SaveAssets();
        }

        private static void PersistMaterialTextures(Material mat, string folderPath,
            Dictionary<UnityEngine.Object, UnityEngine.Object> cache)
        {
            var shader = mat.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;

                var propName = ShaderUtil.GetPropertyName(shader, i);
                var tex = mat.GetTexture(propName);
                if (tex is null || !IsTemporaryAsset(tex)) continue;

                if (!cache.TryGetValue(tex, out var dstTex))
                {
                    var path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{CleanName(tex.name)}.asset");
                    dstTex = UnityEngine.Object.Instantiate(tex);
                    AssetDatabase.CreateAsset(dstTex, path);
                    cache[tex] = dstTex;
                }

                mat.SetTexture(propName, (Texture) dstTex);
            }
        }

        private static string CleanName(string name)
        {
            var clean = name.Replace("(Clone)", "").Replace("(LLC Clone)", "").Replace("Variant", "").Trim();
            if (string.IsNullOrEmpty(clean) || (clean.StartsWith("_") && clean.Length > 20)) return "GeneratedAsset";
            return clean;
        }

        private static bool IsTemporaryAsset(UnityEngine.Object obj)
        {
            if (obj is null) return false;
            var path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path) || path.Contains("ZZZ_GeneratedAssets_");
        }

        private void DeleteSpecificObjects(GameObject target)
        {
            if (!_deleteSettings) return;
            var names = _deleteSettings.targetObjectNames.Where(n => !string.IsNullOrEmpty(n)).ToList();
            if (names.Count == 0) return;
            DeleteUtility.DeleteSpecificObjectsRecursive(target.transform, names, _deleteSettings.useRegex);
        }

        private static void DeleteHideObjects(GameObject target)
        {
            var transforms = target.GetComponentsInChildren<Transform>(true);
            for (var i = transforms.Length - 1; i >= 0; i--)
            {
                var t = transforms[i];
                if (t && t.gameObject != target && !t.gameObject.activeSelf)
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        private void CopySettingsFromBase(GameObject vrmPrefab)
        {
            var tempInstance = UnityEngine.Object.Instantiate(vrmPrefab);

            CopyVRMSettings.Copy(_baseVrmPrefab, tempInstance, CopyVRMSettings.SupportedComponents.ToList());
            EditorUtility.CopySerialized(tempInstance.GetComponent<VRMBlendShapeProxy>(),
                vrmPrefab.GetComponent<VRMBlendShapeProxy>());
            ApplyIsBinarySettings(vrmPrefab);

            UnityEngine.Object.DestroyImmediate(tempInstance);
        }

        private void FinalizeVrmInternalAssets(GameObject vrmPrefab, string avatarName)
        {
            BlendShapeAvatar finalizedBlendShapeAvatar = null;
            var blendProxy = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
            if (blendProxy && blendProxy.BlendShapeAvatar)
            {
                var folderPath = $"{_vrmOutputPath}/{avatarName}.BlendShapes";
                var clips = new List<BlendShapeClip>();
                var baseProxy = _baseVrmPrefab?.GetComponent<VRMBlendShapeProxy>();
                foreach (var sourceClip in blendProxy.BlendShapeAvatar.Clips)
                {
                    if (!sourceClip) continue;
                    var clipSavePath = $"{folderPath}/{sourceClip.name}.asset";
                    var clipCopy = UnityEngine.Object.Instantiate(sourceClip);
                    if (baseProxy && baseProxy.BlendShapeAvatar)
                    {
                        var baseClip = sourceClip.Preset != BlendShapePreset.Unknown
                            ? baseProxy.BlendShapeAvatar.GetClip(sourceClip.Preset)
                            : baseProxy.BlendShapeAvatar.GetClip(sourceClip.BlendShapeName);
                        if (baseClip) clipCopy.IsBinary = baseClip.IsBinary;
                    }

                    AssetDatabase.CreateAsset(clipCopy, clipSavePath);
                    clips.Add(AssetDatabase.LoadAssetAtPath<BlendShapeClip>(clipSavePath));
                }

                var newAvatar = ScriptableObject.CreateInstance<BlendShapeAvatar>();
                newAvatar.Clips = clips;
                AssetDatabase.CreateAsset(newAvatar, $"{folderPath}/BlendShape.asset");
                AssetDatabase.SaveAssets();
                finalizedBlendShapeAvatar =
                    AssetDatabase.LoadAssetAtPath<BlendShapeAvatar>($"{folderPath}/BlendShape.asset");
            }

            var metaAssetPath = $"{_vrmOutputPath}/{avatarName}.MetaObject/Meta.asset";
            var finalizedMeta = AssetDatabase.LoadAssetAtPath<VRMMetaObject>(metaAssetPath);

            if (finalizedBlendShapeAvatar)
            {
                var vrmBlendProxy = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
                if (vrmBlendProxy)
                {
                    vrmBlendProxy.BlendShapeAvatar = finalizedBlendShapeAvatar;
                    EditorUtility.SetDirty(vrmBlendProxy);
                }
            }

            if (!finalizedMeta) return;

            var metaComp = vrmPrefab.GetComponent<VRMMeta>();
            if (!metaComp) return;

            metaComp.Meta = finalizedMeta;
            EditorUtility.SetDirty(metaComp);
        }

        private void ApplyIsBinarySettings(GameObject vrmPrefab)
        {
            var baseProxy = _baseVrmPrefab.GetComponent<VRMBlendShapeProxy>();
            var destProxy = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
            if (!baseProxy || !baseProxy.BlendShapeAvatar || !destProxy || !destProxy.BlendShapeAvatar) return;

            var changed = false;
            foreach (var baseClip in baseProxy.BlendShapeAvatar.Clips)
            {
                if (!baseClip) continue;
                var destClip = baseClip.Preset != BlendShapePreset.Unknown
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