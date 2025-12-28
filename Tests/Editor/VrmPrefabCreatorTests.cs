using System;
using System.IO;
using System.Linq;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmPrefabCreatorTests
    {
        private string _testDir;
        private string _outputPath;
        private VrmPrefabCreator _creator;

        private const string TestModelPath =
            "Assets/Fara/FaraVRMMultiConverter/Sample/mio3_avatar_tools-main/assets/blend/mio3_humanoid_base_chest_b4.blend";

        [SetUp]
        public void SetUp()
        {
            _testDir = $"Assets/Temp_PrefabTest_{Guid.NewGuid().ToString()[..8]}";
            if (!AssetDatabase.IsValidFolder(_testDir))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(_testDir));
            }

            _outputPath = $"{_testDir}/Output";
            AssetDatabase.CreateFolder(_testDir, "Output");

            _creator = new VrmPrefabCreator(_outputPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(_testDir))
            {
                AssetDatabase.DeleteAsset(_testDir);
            }
        }

        /// <summary>
        /// UnityのHumanoid判定を確実にパスする最小構成のモデルを作成
        /// </summary>
        private GameObject CreateValidHumanoidMock(string name)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TestModelPath);
            if (modelAsset == null) Assert.Inconclusive("Test model not found at " + TestModelPath);

            var root = Object.Instantiate(modelAsset);
            root.name = name;
            var animator = root.GetComponent<Animator>();
            if (!animator) animator = root.AddComponent<Animator>();

            var assets = AssetDatabase.LoadAllAssetsAtPath(TestModelPath);
            var originalAvatar = assets.OfType<Avatar>().FirstOrDefault(a => a.isHuman);

            animator.avatar = originalAvatar;
            animator.Rebind();

            return root;
        }

        [Test]
        public void CreateVrmPrefab_WithActualHumanoidAsset_Success()
        {
            // Arrange
            var avatar = CreateValidHumanoidMock("ActualHumanoidAvatar");

            // Act
            var prefabPath = _creator.CreateVrmPrefab(avatar);

            // Assert
            Assert.IsNotEmpty(prefabPath, "Prefab path should not be empty.");

            // プロジェクトルートからの相対パスをフルパスに変換して存在確認
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", prefabPath));
            Assert.IsTrue(File.Exists(fullPath), $"Prefab file should exist at: {fullPath}");

            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(savedPrefab, "Saved prefab should be loadable via AssetDatabase.");
            Assert.IsTrue(savedPrefab.GetComponent<Animator>().isHuman, "Saved prefab should still be Humanoid.");

            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void CreateVrmPrefab_WhenNotHumanoid_ShouldReturnEmptyPath()
        {
            // Arrange
            var nonHumanoid = new GameObject("NonHumanoid");
            nonHumanoid.AddComponent<Animator>(); // Avatarを設定しないので isHuman は false

            // Act
            var path = _creator.CreateVrmPrefab(nonHumanoid);

            // Assert
            Assert.IsEmpty(path, "Should return empty string for non-humanoid avatars.");

            Object.DestroyImmediate(nonHumanoid);
        }

        [Test]
        public void CreateVrmPrefab_WhenExistingPrefab_ShouldReturnExistingPath()
        {
            // Arrange
            // 既存のプレハブ（Humanoid）を準備
            var avatarGo = CreateValidHumanoidMock("ExistingPrefabTest");
            var initialPath = $"{_testDir}/Existing.prefab";
            PrefabUtility.SaveAsPrefabAsset(avatarGo, initialPath);
            Object.DestroyImmediate(avatarGo);

            // プレハブアセットを読み込んで渡す
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(initialPath);

            // Act
            var resultPath = _creator.CreateVrmPrefab(existingPrefab);

            // Assert
            Assert.AreEqual(initialPath, resultPath, "Should return the existing prefab path.");
        }

        [Test]
        public void CreateVrmPrefab_WhenDirectoryCreationFailed_ShouldLogError()
        {
            // EnsureDirectoryExists の catch ブロックを通すテスト
            // 出力ディレクトリと同名の「ファイル」を作成して、ディレクトリ作成を失敗させる
            var invalidDir = $"{_testDir}/BlockedByFile";
            var filePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", invalidDir));
            File.WriteAllText(filePath, "I am a file, not a directory");

            var creatorWithInvalidPath = new VrmPrefabCreator(invalidDir);
            var avatar = CreateValidHumanoidMock("ErrorTestAvatar");

            // catch句の Debug.LogError を期待する
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ディレクトリの作成に失敗しました.*"));

            // 実行（内部で例外が発生し、ログを吐いて throw される）
            Assert.Throws<IOException>(() => creatorWithInvalidPath.CreateVrmPrefab(avatar));

            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void CreateVrmPrefab_ShouldCleanupUniVrmCache()
        {
            // ResetUniVrmCache の DestroyImmediate を通すテスト
            // VRM.PreviewSceneManager 型のオブジェクトを擬似的に作成する
            var previewType = Type.GetType("VRM.PreviewSceneManager, VRM");
            var cacheObj = new GameObject("UniVrmCache");
            cacheObj.AddComponent(previewType);

            var avatar = CreateValidHumanoidMock("CacheCleanupAvatar");

            // Act
            _creator.CreateVrmPrefab(avatar);

            // Assert
            Assert.IsTrue(cacheObj == null || cacheObj.Equals(null), "PreviewSceneManager object should be destroyed.");
            Object.DestroyImmediate(avatar);
        }
    }
}