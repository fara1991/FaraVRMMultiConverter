using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fara.Fara_VRMMultiConverter.Editor
{
    /// <summary>
    /// VRM Metaデータを更新するヘルパークラス
    /// </summary>
    public static class VrmMetaUpdater
    {
        /// <summary>
        /// VRM Metaデータを更新
        /// </summary>
        public static void UpdateMeta(
            string prefabPath, string prefabName, string version, string author, string thumbnailPath, int resolution,
            bool skipRefresh = false
        )
        {
            Debug.Log(L10N.MetaUpdater.UpdateStarted);
            Debug.Log(L10N.MetaUpdater.TargetPrefab(prefabPath));
            Debug.Log(L10N.MetaUpdater.PrefabName(prefabName));

            var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var vrmMeta = GetOrAddVrmMeta(prefabContents);
                var metaObject = GetOrCreateMetaObject(vrmMeta, prefabPath, prefabName);
                SetThumbnail(metaObject, prefabName, prefabContents, thumbnailPath, resolution);
                SetMetaInformation(metaObject, prefabName, version, author);
                SaveChanges(metaObject, prefabContents, prefabPath);
            }
            catch (Exception e)
            {
                Debug.LogError(L10N.MetaUpdater.UpdateError(e.Message));
                Debug.LogError($"スタックトレース: {e.StackTrace}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            // バッチ処理中はRefreshをスキップ
            if (!skipRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(L10N.MetaUpdater.UpdateCompleted);
        }

        private static VRM.VRMMeta GetOrAddVrmMeta(GameObject prefabContents)
        {
            var vrmMeta = prefabContents.GetComponent<VRM.VRMMeta>();
            if (!vrmMeta)
            {
                Debug.Log(L10N.MetaUpdater.VrmMetaComponentNotFound);
                vrmMeta = prefabContents.AddComponent<VRM.VRMMeta>();
            }
            else
            {
                Debug.Log(L10N.MetaUpdater.UsingExistingVrmMetaComponent);
            }

            return vrmMeta;
        }

        private static VRM.VRMMetaObject GetOrCreateMetaObject(
            VRM.VRMMeta vrmMeta, string prefabPath, string prefabName)
        {
            if (vrmMeta.Meta)
            {
                Debug.Log(L10N.MetaUpdater.UsingExistingMetaObject);
                return vrmMeta.Meta;
            }

            Debug.Log(L10N.MetaUpdater.CreatingMetaObject);

            var outputDir = Path.GetDirectoryName(prefabPath);
            var metaDir = $"{outputDir}/{prefabName}.MetaObject";
            if (!AssetDatabase.IsValidFolder(metaDir))
            {
                AssetDatabase.CreateFolder(outputDir, $"{prefabName}.MetaObject");
            }
            
            var metaPath = $"{metaDir}/Meta.asset";
            var metaObject = AssetDatabase.LoadAssetAtPath<VRM.VRMMetaObject>(metaPath);
            if (!metaObject)
            {
                metaObject = ScriptableObject.CreateInstance<VRM.VRMMetaObject>();
                AssetDatabase.CreateAsset(metaObject, metaPath);
                Debug.Log(L10N.MetaUpdater.MetaObjectCreated(metaPath));
            }
            
            vrmMeta.Meta = metaObject;
            Debug.Log(L10N.MetaUpdater.MetaObjectCreated(metaPath));

            return metaObject;
        }

        private static void SetThumbnail(
            VRM.VRMMetaObject metaObject, string prefabName, GameObject prefabContents, string thumbnailPath, int resolution)
        {
            Debug.Log(L10N.MetaUpdater.ThumbnailSettings);
            var thumbnail =
                VrmThumbnailGenerator.GetOrCreateThumbnail(prefabName, prefabContents, thumbnailPath, resolution);

            if (thumbnail)
            {
                metaObject.Thumbnail = thumbnail;
                Debug.Log(L10N.MetaUpdater.ThumbnailSet(
                    thumbnail.name,
                    AssetDatabase.GetAssetPath(thumbnail),
                    thumbnail.width,
                    thumbnail.height
                ));
            }
            else
            {
                Debug.LogWarning(L10N.MetaUpdater.ThumbnailSetFailed);
            }
        }

        private static void SetMetaInformation(
            VRM.VRMMetaObject metaObject, string prefabName, string version, string author)
        {
            Debug.Log(L10N.MetaUpdater.MetaInfoSettings);

            // Title
            if (!string.IsNullOrEmpty(prefabName))
            {
                var oldTitle = metaObject.Title;
                metaObject.Title = prefabName;
                Debug.Log(L10N.MetaUpdater.TitleUpdated(oldTitle, prefabName));
            }
            else
            {
                Debug.LogWarning(L10N.MetaUpdater.TitleSkipped);
            }

            // Version
            if (!string.IsNullOrEmpty(version))
            {
                var oldVersion = metaObject.Version;
                metaObject.Version = version;
                Debug.Log(L10N.MetaUpdater.VersionUpdated(oldVersion, version));
            }
            else
            {
                Debug.LogWarning(L10N.MetaUpdater.VersionSkipped);
            }

            // Author
            if (!string.IsNullOrEmpty(author))
            {
                var oldAuthor = metaObject.Author;
                metaObject.Author = author;
                Debug.Log(L10N.MetaUpdater.AuthorUpdated(oldAuthor, author));
            }
            else
            {
                Debug.LogWarning(L10N.MetaUpdater.AuthorSkipped);
            }

            Debug.Log(L10N.MetaUpdater.MetaInfoSummary);
            Debug.Log($"  Title: {metaObject.Title}");
            Debug.Log($"  Version: {metaObject.Version}");
            Debug.Log($"  Author: {metaObject.Author}");
            Debug.Log($"  Thumbnail: {(metaObject.Thumbnail ? metaObject.Thumbnail.name : L10N.MetaUpdater.None)}");
        }

        private static void SaveChanges(VRM.VRMMetaObject metaObject, GameObject prefabContents, string prefabPath)
        {
            EditorUtility.SetDirty(metaObject);
            AssetDatabase.SaveAssetIfDirty(metaObject);

            Debug.Log(L10N.MetaUpdater.SavingPrefab);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            Debug.Log(L10N.MetaUpdater.PrefabSaved);
        }
    }
}