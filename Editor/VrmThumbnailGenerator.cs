using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fara.FaraVRMMultiConverter.Editor
{
    /// <summary>
    /// VRMアバターのサムネイル画像を生成するヘルパークラス
    /// </summary>
    public static class VrmThumbnailGenerator
    {
        /// <summary>
        /// サムネイル解像度の選択肢
        /// </summary>
        public enum ThumbnailResolution
        {
            [InspectorName("512x512")] Resolution512 = 512,
            [InspectorName("1024x1024")] Resolution1024 = 1024,
            [InspectorName("2048x2048")] Resolution2048 = 2048,
            [InspectorName("4096x4096")] Resolution4096 = 4096
        }

        /// <summary>
        /// サムネイルが既に存在するかチェック
        /// </summary>
        public static bool ThumbnailExists(string prefabName, string thumbnailPath)
        {
            string[] extensions = {".png", ".jpg", ".jpeg"};

            return extensions
                .Select(ext => Path.Combine(thumbnailPath, prefabName + ext))
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Any(existingThumbnail => existingThumbnail);
        }

        /// <summary>
        /// サムネイル画像を取得または作成
        /// </summary>
        public static Texture2D GetOrCreateThumbnail(
            string prefabName,
            GameObject avatarInstance,
            string thumbnailPath,
            int resolution = (int) ThumbnailResolution.Resolution1024,
            int warmupRenders = 2,
            int extraDelayMilliseconds = 0)
        {
            // サムネイルディレクトリを作成
            EnsureThumbnailDirectoryExists(thumbnailPath);

            // 既存のサムネイルを探す
            var existingThumbnail = FindExistingThumbnail(prefabName, thumbnailPath);
            if (existingThumbnail) return existingThumbnail;

            // 新規作成
            Debug.Log("既存のサムネイルが見つかりませんでした");
            Debug.Log("新しいサムネイルを作成します...");
            return CreateThumbnailFromCamera(
                prefabName,
                avatarInstance,
                thumbnailPath,
                resolution,
                warmupRenders,
                extraDelayMilliseconds);
        }

        private static void EnsureThumbnailDirectoryExists(string thumbnailPath)
        {
            if (Directory.Exists(thumbnailPath)) return;

            Debug.Log($"サムネイルディレクトリが存在しないため作成します: {thumbnailPath}");
            Directory.CreateDirectory(thumbnailPath);
        }

        private static Texture2D FindExistingThumbnail(string prefabName, string thumbnailPath)
        {
            Debug.Log($"既存のサムネイル画像を検索中: {prefabName}");
            string[] extensions = {".png", ".jpg", ".jpeg"};

            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(thumbnailPath, prefabName + ext);
                Debug.Log($"  検索パス: {fullPath}");

                var existingThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
                if (!existingThumbnail) continue;

                Debug.Log($"✓ 既存のサムネイルを使用します: {fullPath}");
                return existingThumbnail;
            }

            return null;
        }

        private static Texture2D CreateThumbnailFromCamera(
            string prefabName,
            GameObject avatarInstance,
            string thumbnailPath,
            int resolution,
            int warmupRenders,
            int extraDelayMilliseconds)
        {
            Debug.Log("=== サムネイル画像の作成開始 ===");

            // プレハブコンテンツの場合、一時的にシーンにインスタンス化する必要がある
            GameObject tempInstance;
            var needsCleanup = false;

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(avatarInstance);
            if (prefabStage)
            {
                Debug.Log("プレハブコンテンツを検出しました。一時的にシーンにインスタンス化します");
                tempInstance = Object.Instantiate(avatarInstance);
                tempInstance.SetActive(true);
                needsCleanup = true;
            }
            else
            {
                tempInstance = avatarInstance;
            }

            try
            {
                var cameraInfo = SetupCamera();
                WarmupRender(cameraInfo.Camera, warmupRenders, extraDelayMilliseconds);

                var thumbnail = RenderThumbnail(cameraInfo.Camera, resolution);

                CleanupCamera(cameraInfo);

                return SaveThumbnail(thumbnail, prefabName, thumbnailPath);
            }
            finally
            {
                // 一時的にインスタンス化した場合は削除
                if (needsCleanup && tempInstance)
                {
                    Debug.Log("一時的なインスタンスを削除します");
                    Object.DestroyImmediate(tempInstance);
                }
            }
        }

        private static void WarmupRender(Camera camera, int warmupRenders, int extraDelayMilliseconds)
        {
            warmupRenders = Mathf.Clamp(warmupRenders, 0, 10);
            extraDelayMilliseconds = Mathf.Clamp(extraDelayMilliseconds, 0, 2000);

            if (warmupRenders == 0 && extraDelayMilliseconds == 0) return;

            Debug.Log($"ウォームアップ描画: {warmupRenders}回 / 追加待機: {extraDelayMilliseconds}ms");

            for (var i = 0; i < warmupRenders; i++)
            {
                camera.Render();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }

            if (extraDelayMilliseconds > 0) Thread.Sleep(extraDelayMilliseconds);
        }

        private class CameraInfo
        {
            public Camera Camera;
            public bool WasCreated;
            public Vector3 OriginalPosition;
            public Quaternion OriginalRotation;
            public RenderTexture OriginalTargetTexture;
            public CameraClearFlags OriginalClearFlags;
            public Color OriginalBackgroundColor;
            public float OriginalFieldOfView;
            public int OriginalCullingMask;
            public float OriginalNearClip;
        }

        private static CameraInfo SetupCamera()
        {
            var info = new CameraInfo
            {
                Camera = Camera.main
            };

            if (!info.Camera)
            {
                Debug.Log("メインカメラが見つかりません。カメラを検索中...");
                info.Camera = Object.FindObjectOfType<Camera>();
            }

            info.WasCreated = false;

            // 既存カメラの状態を保存
            info.OriginalPosition = info.Camera.transform.position;
            info.OriginalRotation = info.Camera.transform.rotation;
            info.OriginalTargetTexture = info.Camera.targetTexture;
            info.OriginalClearFlags = info.Camera.clearFlags;
            info.OriginalBackgroundColor = info.Camera.backgroundColor;
            info.OriginalFieldOfView = info.Camera.fieldOfView;
            info.OriginalCullingMask = info.Camera.cullingMask;
            info.OriginalNearClip = info.Camera.nearClipPlane;

            Debug.Log("既存カメラの状態を保存しました");

            // 近づいてもアバターが消えないように
            info.Camera.cullingMask = ~0;
            info.Camera.nearClipPlane = 0.01f;

            PositionCamera(info.Camera);

            return info;
        }

        private static void PositionCamera(Camera camera)
        {
            // アバターは原点にある前提で、標準的なバストアップ位置に固定
            camera.transform.position = new Vector3(0, 0.7f, 1.4f);
            camera.transform.LookAt(new Vector3(0, 0.7f, 0));
        }

        private static Texture2D RenderThumbnail(Camera camera, int resolution)
        {
            if (!Enum.IsDefined(typeof(ThumbnailResolution), resolution))
            {
                Debug.Log($"変換不可な解像度のため、1024x1024で出力します");
                resolution = (int) ThumbnailResolution.Resolution1024;
            }

            var rt = RenderTexture.GetTemporary(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            var previousRT = camera.targetTexture;
            var previousClearFlags = camera.clearFlags;
            var previousBackgroundColor = camera.backgroundColor;

            camera.targetTexture = rt;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // グレー背景

            camera.Render();

            RenderTexture.active = rt;
            var thumbnail = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            thumbnail.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            thumbnail.Apply();
            RenderTexture.active = null;

            camera.targetTexture = previousRT;
            camera.clearFlags = previousClearFlags;
            camera.backgroundColor = previousBackgroundColor;
            RenderTexture.ReleaseTemporary(rt);

            Debug.Log("✓ サムネイルのレンダリングが完了しました");
            return thumbnail;
        }

        private static void CleanupCamera(CameraInfo info)
        {
            Debug.Log("既存カメラの状態を復元します");
            info.Camera.transform.position = info.OriginalPosition;
            info.Camera.transform.rotation = info.OriginalRotation;
            info.Camera.targetTexture = info.OriginalTargetTexture;
            info.Camera.clearFlags = info.OriginalClearFlags;
            info.Camera.backgroundColor = info.OriginalBackgroundColor;
            info.Camera.fieldOfView = info.OriginalFieldOfView;
            info.Camera.cullingMask = info.OriginalCullingMask;
            info.Camera.nearClipPlane = info.OriginalNearClip;
        }

        private static Texture2D SaveThumbnail(Texture2D thumbnail, string prefabName, string thumbnailPath)
        {
            var assetPath = $"{thumbnailPath.TrimEnd('/', '\\')}/{prefabName}.png".Replace('\\', '/');
            Debug.Log($"サムネイルを保存中: {assetPath}");

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
            var fullFilePath = $"{projectRoot}/{assetPath}".Replace('\\', '/');

            var bytes = thumbnail.EncodeToPNG();
            File.WriteAllBytes(fullFilePath, bytes);
            Debug.Log($"✓ ファイルに書き込みました: {bytes.Length} bytes");

            // Refreshの代わりに同期インポートして参照可能にする
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var savedThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Debug.Log($"✓ サムネイルを作成しました: {assetPath}");
            Debug.Log($"  サイズ: {savedThumbnail.width}x{savedThumbnail.height}");
            return savedThumbnail;
        }
    }
}