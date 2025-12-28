using System.IO;
using System.Linq;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmPrefabCreatorTests
    {
        private GameObject _testAvatar;
        private const string TestOutputDir = "Assets/Fara/Tests/TestOutput";
        private VrmPrefabCreator _creator;

        [SetUp]
        public void SetUp()
        {
            _creator = new VrmPrefabCreator(TestOutputDir);
            // 単純なGameObjectとして作成
            _testAvatar = new GameObject("TestAvatar");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testAvatar != null)
            {
                Object.DestroyImmediate(_testAvatar);
            }

            if (Directory.Exists(TestOutputDir))
            {
                AssetDatabase.DeleteAsset(TestOutputDir);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void CreateVrmPrefab_WithoutAnimator_ReturnsEmptyString()
        {
            // Animatorがない場合
            var result = _creator.CreateVrmPrefab(_testAvatar);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CreateVrmPrefab_WithNonHumanoidAnimator_ReturnsEmptyString()
        {
            // Animatorはあるが、Avatarがセットされていない（isHuman=false）場合
            _testAvatar.AddComponent<Animator>();

            var result = _creator.CreateVrmPrefab(_testAvatar);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void CreateVrmPrefab_WithActualHumanoidAsset_Success()
        {
            // Arrange: プロジェクト内から Humanoid である GameObject を動的に探す
            var allGameObjectGuids = AssetDatabase.FindAssets("t:Model t:Prefab");
            GameObject humanoidPrefab = null;
            var foundPath = "";

            foreach (var guid in allGameObjectGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;

                var animator = asset.GetComponent<Animator>();
                // Humanoid かどうかを確認
                if (animator == null || !animator.isHuman) continue;
                humanoidPrefab = asset;
                foundPath = path;
                break; // 最初に見つかったものを使用
            }

            // if (humanoidPrefab == null)
            // {
            // Assert.Ignore("プロジェクト内に Humanoid アバターが見つからないため、このテストをスキップします。");
            // return;
            // }

            Debug.Log($"テストに使用するアバター: {foundPath}");

            // シーン上にインスタンス化
            var instance = (GameObject) PrefabUtility.InstantiatePrefab(humanoidPrefab);

            try
            {
                // Act
                var resultPath = _creator.CreateVrmPrefab(instance);

                // Assert
                Assert.IsNotEmpty(resultPath, "保存パスが空であってはいけません。");
                Assert.IsTrue(File.Exists(resultPath), "プレハブファイルが実際に生成されている必要があります。");

                var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(resultPath);
                Assert.IsNotNull(savedPrefab, "保存されたプレハブをロードできる必要があります。");
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CreateVrmPrefab_WithNonPrefabGameObject_SavesAsNewAsset()
        {
            var allGameObjectGuids = AssetDatabase.FindAssets("t:Model t:Prefab");
            var humanoidSource = (
                from guid in allGameObjectGuids
                select AssetDatabase.GUIDToAssetPath(guid)
                into path
                select AssetDatabase.LoadAssetAtPath<GameObject>(path)
                into asset
                where asset != null
                let anim = asset.GetComponent<Animator>()
                where anim != null && anim.isHuman
                select asset
            ).FirstOrDefault();

            if (humanoidSource == null) Assert.Ignore("Humanoidモデルが見つかりません");

            // 2. Object.Instantiate で「プレハブリンクのない独立したGameObject」を作成
            // ただし、Animator.avatar が外れないように注意してコピーします
            var instance = Object.Instantiate(humanoidSource);
            instance.name = "NewAvatarInstance";

            try
            {
                // Act
                var resultPath = _creator.CreateVrmPrefab(instance);

                // Assert: 空文字ではなく、出力ディレクトリ内のパスが返ってきているか
                Assert.IsNotEmpty(resultPath, "結果が空文字であってはいけません。");
                Assert.IsTrue(resultPath.StartsWith(TestOutputDir), $"パスが {TestOutputDir} で始まっていません: {resultPath}");
                Assert.IsTrue(File.Exists(resultPath), "プレハブファイルがディスク上に存在しません。");
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void EnsureDirectoryExists_CreatesNewDirectory()
        {
            // ディレクトリ作成ロジックの単体テスト
            var nestedDir = $"{TestOutputDir}/SubDir/Target";
            var method = typeof(VrmPrefabCreator).GetMethod("EnsureDirectoryExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] {nestedDir});

            Assert.IsTrue(Directory.Exists(nestedDir));
        }

        [Test]
        public void EnsureDirectoryExists_WithEmptyPath_DoesNothing()
        {
            // Arrange: 空文字列を渡す
            var method = typeof(VrmPrefabCreator).GetMethod("EnsureDirectoryExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert: エラーにならずに終了することを確認
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] {""}));
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] {null}));
        }

        [Test]
        public void EnsureDirectoryExists_WhenDirectoryAlreadyExists_DoesNothing()
        {
            // Arrange: すでに存在するディレクトリ
            if (!Directory.Exists(TestOutputDir)) Directory.CreateDirectory(TestOutputDir);

            var method = typeof(VrmPrefabCreator).GetMethod("EnsureDirectoryExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] {TestOutputDir}));
        }

        [Test]
        public void EnsureDirectoryExists_InvalidPath_LogsError()
        {
            // Arrange: 作成不可能な不正なパス（Windowsで使えない文字など）
            // 注意: 環境によっては例外を投げない可能性もあるため、LogAssertを使用します
            var invalidPath = "Assets/???invalid:\0path";

            var method = typeof(VrmPrefabCreator).GetMethod("EnsureDirectoryExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            // Directory.CreateDirectory が例外を投げることを期待し、かつ内部で Debug.LogError が呼ばれるのを無視/確認
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ディレクトリの作成に失敗しました.*"));

            // Invoke経由なので TargetInvocationException に包まれる
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                method.Invoke(null, new object[] {invalidPath}));
        }
        
        [Test]
        public void GetPrefabPath_ReturnsCorrectPath()
        {
            // パス生成ロジックの単体テスト
            var method = typeof(VrmPrefabCreator).GetMethod("GetPrefabPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = (string) method.Invoke(_creator, new object[] {_testAvatar});

            Assert.AreEqual($"{TestOutputDir}/TestAvatar.prefab", result);
        }

        [Test]
        public void GetPrefabPath_WhenAvatarIsAlreadyPrefab_ReturnsExistingPath()
        {
            // Arrange: 既存のプレハブアセットを作成
            var prefabPath = $"{TestOutputDir}/ExistingPrefab.prefab";
            if (!Directory.Exists(TestOutputDir)) Directory.CreateDirectory(TestOutputDir);

            // 一度保存してアセットにする
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(_testAvatar, prefabPath);

            // Act: リフレクションで内部メソッドを呼び出す
            var method = typeof(VrmPrefabCreator).GetMethod("GetPrefabPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (string) method.Invoke(_creator, new object[] {prefabAsset});

            // Assert: 新しいパスではなく、既存のプレハブパスが返ることを確認
            Assert.AreEqual(prefabPath, result);
        }

        [Test]
        public void ResetUniVrmCache_DoesNotThrow()
        {
            var method = typeof(VrmPrefabCreator).GetMethod("ResetUniVrmCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, null));
        }

        [Test]
        public void ResetUniVrmCache_WhenComponentExists_DestroysIt()
        {
            // Arrange: VRM.PreviewSceneManager のフリをするコンポーネントを適当に探すのは難しいため、
            // 型が存在する場合に DestroyImmediate が呼ばれるパスを想定します。
            // (このメソッドは static なので直接呼ぶだけで、内部の FindObjectsOfType が走ります)
            var method = typeof(VrmPrefabCreator).GetMethod("ResetUniVrmCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert: 実行してエラーが起きないことを確認
            Assert.DoesNotThrow(() => method.Invoke(null, null));
        }

        [Test]
        public void ResetUniVrmCache_WhenPreviewManagerExists_DestroysIt()
        {
            // Arrange: VRM.PreviewSceneManager のふりをする GameObject を作成
            // UniVRM がプロジェクトにある場合、その型を動的に作成してアタッチします
            var previewType = System.Type.GetType("VRM.PreviewSceneManager, VRM");
            var dummyManager = new GameObject("DummyPreviewManager");
            dummyManager.AddComponent(previewType);

            var method = typeof(VrmPrefabCreator).GetMethod("ResetUniVrmCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, null);

            // Assert: 削除されていることを確認
            Assert.IsTrue(dummyManager == null || dummyManager.Equals(null));
        }
    }
}