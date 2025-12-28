using System.IO;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRM;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmMetaUpdaterTests
    {
        private string _testDir;
        private string _prefabPath;
        private const string TestPrefabName = "TestAvatar";

        [SetUp]
        public void SetUp()
        {
            _testDir = $"Assets/Temp_Test_{System.Guid.NewGuid()}";
            if (!AssetDatabase.IsValidFolder(_testDir))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(_testDir));
            }
            _prefabPath = $"{_testDir}/{TestPrefabName}.prefab";

            var go = new GameObject(TestPrefabName);
            PrefabUtility.SaveAsPrefabAsset(go, _prefabPath);
            Object.DestroyImmediate(go);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(_testDir))
            {
                AssetDatabase.DeleteAsset(_testDir);
            }
        }

        [Test]
        public void UpdateMeta_ShouldCreateNewMetaComponentsAndObject()
        {
            // Act
            // thumbnailPath には必ず存在するディレクトリ (_testDir) を渡す
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0.0", "TestAuthor", _testDir, 256);

            // Assert
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist.");
            
            var vrmMeta = prefab.GetComponent<VRMMeta>();
            Assert.IsNotNull(vrmMeta, "VRMMeta component should be added.");
            Assert.IsNotNull(vrmMeta.Meta, "VRMMetaObject should be created.");
            Assert.AreEqual("1.0.0", vrmMeta.Meta.Version);
        }

        [Test]
        public void UpdateMeta_ShouldReuseExistingMetaAndObject()
        {
            // Arrange
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0", "Author1", _testDir, 256);
            
            var prefabBefore = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            var initialMetaObject = prefabBefore.GetComponent<VRMMeta>().Meta;

            // Act
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "2.0", "Author2", _testDir, 256);

            // Assert
            var prefabAfter = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            var updatedMeta = prefabAfter.GetComponent<VRMMeta>();
            
            Assert.AreSame(initialMetaObject, updatedMeta.Meta, "Should reuse the existing MetaObject asset.");
            Assert.AreEqual("2.0", updatedMeta.Meta.Version);
        }

        [Test]
        public void UpdateMeta_ThumbnailPath_SuccessBranch()
        {
            // Arrange
            string thumbDir = $"{_testDir}/Thumbnails";
            AssetDatabase.CreateFolder(_testDir, "Thumbnails");
            
            // フルパスを正しく計算
            string absoluteThumbPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", thumbDir));
            if (!Directory.Exists(absoluteThumbPath)) Directory.CreateDirectory(absoluteThumbPath);

            // Act
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0", "Author", thumbDir, 256);

            // Assert
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            var meta = prefab.GetComponent<VRMMeta>().Meta;
            Assert.IsNotNull(meta.Thumbnail, "Thumbnail should be assigned.");
        }

        [Test]
        public void UpdateMeta_ShouldHandleEmptyInputsGracefully()
        {
            // Arrange
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0", "Author", _testDir, 256);
            
            // Act
            // バージョンと著者を空にして、既存の値が維持されるか確認
            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "", "", _testDir, 256);

            // Assert
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            var metaObject = prefab.GetComponent<VRMMeta>().Meta;
            
            Assert.AreEqual("1.0", metaObject.Version, "Version should remain unchanged.");
            Assert.AreEqual("Author", metaObject.Author, "Author should remain unchanged.");
        }
        
        [Test]
        public void UpdateMeta_WithInvalidThumbnailPath_ShouldLogWarning()
        {
            // SetThumbnail の else 句を通すテスト
            // VrmThumbnailGenerator が失敗するように、不正な文字を含む「ファイルパス」を渡す
            // (ディレクトリ作成に失敗し、例外が発生して catch 句に飛ぶことを利用)
            string invalidPath = "Assets/??invalid??"; 

            // catch 句に入る Error ログを明示的に期待する（これをしないとテストが失敗する）
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0", "Author", invalidPath, 256);
        }
        
        [Test]
        public void UpdateMeta_WithEmptyPrefabName_ShouldSkipTitleAndLogWarning()
        {
            // SetMetaInformation の Title スキップを通すテスト
            // ただし、空の名前にするとアセット保存パスの生成に失敗して Error が出るため、
            // その Error も含めて期待リストに入れる
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Parent directory must exist.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Error occurred during VRM Meta update.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_prefabPath, "", "1.0", "Author", _testDir, 256);
        }
        
        [Test]
        public void UpdateMeta_WhenExceptionOccurs_ShouldLogError()
        {
            // catch ブロック (Debug.LogError) を通すテスト
            // 既に存在するアセットパスにディレクトリ作成を試みさせるなどして例外を誘発
            string metaDir = $"{_testDir}/{TestPrefabName}.MetaObject";
            // ディレクトリであるべき場所にファイルを置く
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", metaDir));
            File.WriteAllText(absolutePath, "BlockingFile");

            // 期待されるエラーログをすべて登録
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Error occurred during VRM Meta update.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_prefabPath, TestPrefabName, "1.0", "Author", _testDir, 256);
        }
    }
}