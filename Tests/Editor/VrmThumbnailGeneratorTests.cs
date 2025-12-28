using System.IO;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmThumbnailGeneratorTests
    {
        private const string TestThumbnailPath = "Assets/Fara/Tests/GeneratedThumbnails";
        private GameObject _avatarInstance;

        [SetUp]
        public void SetUp()
        {
            // テスト用のダミーアバター作成
            _avatarInstance = new GameObject("TestAvatarInstance");
            _avatarInstance.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            // GameObjectの破棄
            if (_avatarInstance != null)
            {
                Object.DestroyImmediate(_avatarInstance);
            }

            // 生成されたテストアセットの削除
            if (Directory.Exists(TestThumbnailPath))
            {
                AssetDatabase.DeleteAsset(TestThumbnailPath);
                AssetDatabase.Refresh();
            }

            // 一時カメラが万が一残っていたら削除
            var tempCam = GameObject.Find("ThumbnailCamera_Temp");
            if (tempCam != null) Object.DestroyImmediate(tempCam);
        }

        [Test]
        public void ThumbnailExists_ShouldReturnCorrectStatus()
        {
            var name = "ExistTest";
            Assert.IsFalse(VrmThumbnailGenerator.ThumbnailExists(name, TestThumbnailPath), "最初は存在しない");

            // フォルダだけ作って空のテクスチャを保存してみる
            if (!Directory.Exists(TestThumbnailPath)) Directory.CreateDirectory(TestThumbnailPath);
            var dummy = new Texture2D(2, 2);
            File.WriteAllBytes(Path.Combine(TestThumbnailPath, name + ".png"), dummy.EncodeToPNG());
            AssetDatabase.ImportAsset(Path.Combine(TestThumbnailPath, name + ".png"), ImportAssetOptions.ForceSynchronousImport);

            Assert.IsTrue(VrmThumbnailGenerator.ThumbnailExists(name, TestThumbnailPath), "保存後は存在する");
        }

        [Test]
        public void GetOrCreateThumbnail_FullFlowTest()
        {
            // 1. 新規生成パス (EnsureThumbnailDirectoryExists -> CreateThumbnailFromCamera)
            var prefabName = "NewAvatarThumbnail";
            var result = VrmThumbnailGenerator.GetOrCreateThumbnail(
                prefabName, 
                _avatarInstance, 
                TestThumbnailPath, 
                (int)VrmThumbnailGenerator.ThumbnailResolution.Resolution512);

            Assert.IsNotNull(result);
            Assert.AreEqual(512, result.width);
            Assert.IsTrue(File.Exists(Path.Combine(TestThumbnailPath, prefabName + ".png")), "ファイルが書き出されていること");

            // 2. 既存取得パス (FindExistingThumbnail)
            // ログに「既存のサムネイルを使用します」が出るルート
            var secondResult = VrmThumbnailGenerator.GetOrCreateThumbnail(
                prefabName, 
                _avatarInstance, 
                TestThumbnailPath);

            Assert.AreEqual(result, secondResult, "二回目は同じアセットを返すべき");
        }

        [Test]
        public void SetupCamera_WhenNoMainCamera_ShouldWork()
        {
            // メインカメラを一時的に隠す
            var mainCam = Camera.main;
            string originalTag = "";
            if (mainCam != null)
            {
                originalTag = mainCam.tag;
                mainCam.tag = "Untagged";
            }

            try
            {
                // カメラがない状態で生成を試みる
                var result = VrmThumbnailGenerator.GetOrCreateThumbnail("NoCamTest", _avatarInstance, TestThumbnailPath);
                Assert.IsNotNull(result, "カメラが無くても自動生成して動くべき");
            }
            finally
            {
                if (mainCam != null) mainCam.tag = originalTag;
            }
        }

        [Test]
        public void SetupCamera_RestoresOriginalCameraState()
        {
            var mainCam = Camera.main;

            // 元の状態を記録
            var originalPos = new Vector3(10, 10, 10);
            mainCam!.transform.position = originalPos;
            const float originalFieldOfView = 60f;
            mainCam.fieldOfView = originalFieldOfView;

            // 実行
            VrmThumbnailGenerator.GetOrCreateThumbnail("RestoreTest", _avatarInstance, TestThumbnailPath);

            // 復元されているか確認 (CleanupCamera)
            Assert.AreEqual(originalPos, mainCam.transform.position, "カメラの位置が復元されていること");
            Assert.AreEqual(originalFieldOfView, mainCam.fieldOfView, "FOVが復元されていること");
        }

        [Test]
        public void RenderThumbnail_WithInvalidResolution_FallsBackTo1024()
        {
            // enumにない適当な数値
            int invalidRes = 999; 
            
            var result = VrmThumbnailGenerator.GetOrCreateThumbnail(
                "FallbackTest", 
                _avatarInstance, 
                TestThumbnailPath, 
                invalidRes);

            Assert.AreEqual(1024, result.width, "不正な解像度は1024にフォールバックされるべき");
        }

        [Test]
        public void WarmupRender_Coverage()
        {
            // warmupRendersとextraDelayMillisecondsを通過させる
            // 内部でThread.Sleepが走る
            Assert.DoesNotThrow(() => {
                VrmThumbnailGenerator.GetOrCreateThumbnail(
                    "WarmupTest", 
                    _avatarInstance, 
                    TestThumbnailPath, 
                    warmupRenders: 1, 
                    extraDelayMilliseconds: 10);
            });
        }
        
        [Test]
        public void CreateThumbnail_FromPrefabStage_ShouldPassThroughInstantiatePath()
        {
            // 1. テスト用のプレハブアセットを作成
            var tempPrefabPath = "Assets/TempTestPrefab.prefab";
            var tempGo = new GameObject("TempPrefabSource");
            PrefabUtility.SaveAsPrefabAsset(tempGo, tempPrefabPath);
            Object.DestroyImmediate(tempGo);

            try
            {
                // 2. プレハブをPrefabモードで開く
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath);
                AssetDatabase.OpenAsset(prefabAsset);
                
                // PrefabStage（現在開いているPrefab編集画面）を取得
                var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                Assert.IsNotNull(stage, "PrefabStageを開くことに失敗しました");

                // PrefabStage内のインスタンスを取得
                var prefabInstanceInStage = stage.prefabContentsRoot;

                // 3. 実行：このパスで VrmThumbnailGenerator.CreateThumbnailFromCamera 内部の 
                // prefabStage != null の分岐（赤い箇所）を通過します
                var result = VrmThumbnailGenerator.GetOrCreateThumbnail(
                    "PrefabStageTest",
                    prefabInstanceInStage,
                    TestThumbnailPath
                );

                // Assert
                Assert.IsNotNull(result, "PrefabStage内のオブジェクトからサムネイルが生成されること");
                
                // 4. Prefabモードを閉じる
                UnityEditor.SceneManagement.StageUtility.GoToMainStage();
            }
            finally
            {
                // クリーンアップ
                AssetDatabase.DeleteAsset(tempPrefabPath);
            }
        }
    }
}