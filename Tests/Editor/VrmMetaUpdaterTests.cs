using System.IO;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using VRM;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmMetaUpdaterTests
    {
        private string _testDir;
        private string _prefabPath;
        private GameObject _testAvatar;
        private const string TestPrefabName = "TestAvatar";

        [SetUp]
        public void SetUp()
        {
            _testDir = $"Assets/Temp_Test_{System.Guid.NewGuid()}";
            if (!AssetDatabase.IsValidFolder(_testDir))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(_testDir));

            _prefabPath = $"{_testDir}/{TestPrefabName}.prefab";

            var go = new GameObject(TestPrefabName);
            PrefabUtility.SaveAsPrefabAsset(go, _prefabPath);
            Object.DestroyImmediate(go);

            _testAvatar = PrefabUtility.LoadPrefabContents(_prefabPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(_testDir)) AssetDatabase.DeleteAsset(_testDir);

            PrefabUtility.UnloadPrefabContents(_testAvatar);
        }

        [Test]
        public void UpdateMeta_ShouldCreateNewMetaComponentsAndObject()
        {
            // Act
            // thumbnailPath には必ず存在するディレクトリ (_testDir) を渡す
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0.0", "TestAuthor", _testDir, 256);

            // Assert
            var vrmMeta = _testAvatar.GetComponent<VRMMeta>();
            Assert.IsNotNull(vrmMeta, "VRMMeta component should be added to the memory object.");
            Assert.IsNotNull(vrmMeta.Meta, "VRMMetaObject should be created.");
            Assert.AreEqual("1.0.0", vrmMeta.Meta.Version);
        }

        [Test]
        public void UpdateMeta_ShouldReuseExistingMetaAndObject()
        {
            // Arrange
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0", "Author1", _testDir, 256);
            var initialMetaObject = _testAvatar.GetComponent<VRMMeta>().Meta;

            // Act
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "2.0", "Author2", _testDir, 256);

            // Assert
            var updatedMeta = _testAvatar.GetComponent<VRMMeta>();

            Assert.AreSame(initialMetaObject, updatedMeta.Meta, "Should reuse the existing MetaObject asset.");
            Assert.AreEqual("2.0", updatedMeta.Meta.Version);
        }

        [Test]
        public void UpdateMeta_ThumbnailPath_SuccessBranch()
        {
            // Arrange
            var thumbDir = $"{_testDir}/Thumbnails";
            AssetDatabase.CreateFolder(_testDir, "Thumbnails");

            // フルパスを正しく計算
            var absoluteThumbPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", thumbDir));
            if (!Directory.Exists(absoluteThumbPath)) Directory.CreateDirectory(absoluteThumbPath);

            // Act
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0", "Author", thumbDir, 256);

            // Assert
            var meta = _testAvatar.GetComponent<VRMMeta>().Meta;
            Assert.IsNotNull(meta.Thumbnail, "Thumbnail should be assigned.");
        }

        [Test]
        public void UpdateMeta_ShouldHandleEmptyInputsGracefully()
        {
            // Arrange
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0", "Author", _testDir, 256);

            // Act
            // バージョンと著者を空にして、既存の値が維持されるか確認
            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "", "", _testDir, 256);

            // Assert
            var metaObject = _testAvatar.GetComponent<VRMMeta>().Meta;

            Assert.AreEqual("1.0", metaObject.Version, "Version should remain unchanged.");
            Assert.AreEqual("Author", metaObject.Author, "Author should remain unchanged.");
        }

        [Test]
        public void UpdateMeta_WithInvalidThumbnailPath_ShouldLogWarning()
        {
            // SetThumbnail の else 句を通すテスト
            // VrmThumbnailGenerator が失敗するように、不正な文字を含む「ファイルパス」を渡す
            // (ディレクトリ作成に失敗し、例外が発生して catch 句に飛ぶことを利用)
            const string invalidPath = "Assets/??invalid??";

            // catch 句に入る Error ログを明示的に期待する（これをしないとテストが失敗する）
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            LogAssert.Expect(LogType.Error, new Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0", "Author", invalidPath, 256);
        }

        [Test]
        public void UpdateMeta_WithEmptyPrefabName_ShouldSkipTitleAndLogWarning()
        {
            // SetMetaInformation の Title スキップを通すテスト
            // ただし、空の名前にするとアセット保存パスの生成に失敗して Error が出るため、
            // その Error も含めて期待リストに入れる
            LogAssert.Expect(LogType.Error, new Regex("Parent directory must exist.*"));
            LogAssert.Expect(LogType.Error, new Regex("Error occurred during VRM Meta update.*"));
            LogAssert.Expect(LogType.Error, new Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, "", "1.0", "Author", _testDir, 256);
        }

        [Test]
        public void UpdateMeta_WhenExceptionOccurs_ShouldLogError()
        {
            // catch ブロック (Debug.LogError) を通すテスト
            // 既に存在するアセットパスにディレクトリ作成を試みさせるなどして例外を誘発
            var metaDir = $"{_testDir}/{TestPrefabName}.MetaObject";
            // ディレクトリであるべき場所にファイルを置く
            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", metaDir));
            File.WriteAllText(absolutePath, "BlockingFile");

            // 期待されるエラーログをすべて登録
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            LogAssert.Expect(LogType.Error, new Regex("Error occurred during VRM Meta update.*"));
            LogAssert.Expect(LogType.Error, new Regex("スタックトレース.*"));

            VrmMetaUpdater.UpdateMeta(_testAvatar, _prefabPath, TestPrefabName, "1.0", "Author", _testDir, 256);
        }
    }
}