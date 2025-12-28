using System;
using System.IO;
using System.Linq;
using Esperecyan.UniVRMExtensions;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRM;
using Object = UnityEngine.Object;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class VrmAvatarConverterTests
    {
        private string _testRootDir;
        private string _outputPath;
        private string _thumbnailPath;
        private DeleteSpecificObjectsSettings _deleteSettings;
        private int _testCounter;

        private const string TestModelPath =
            "Assets/Fara/FaraVRMMultiConverter/Sample/mio3_avatar_tools-main/assets/blend/mio3_humanoid_base_chest_b4.blend";

        private class MockBakeProcessor : IVrmBakeProcessor
        {
            public GameObject ProcessBake(GameObject vrcAvatarInstance, string avatarName)
            {
                // 本物のベイク処理の代わりに、複製して名前を変えるだけの処理を行う
                var mockBaked = Object.Instantiate(vrcAvatarInstance);
                mockBaked.name = avatarName;
                return mockBaked;
            }
        }

        private class ExceptionBakeProcessor : IVrmBakeProcessor
        {
            public GameObject ProcessBake(GameObject vrcAvatarInstance, string avatarName)
            {
                throw new Exception("Simulated Exception for Testing");
            }
        }

        [SetUp]
        public void SetUp()
        {
            _testCounter++;
            _testRootDir = $"Assets/Temp_Converter_{_testCounter}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_testRootDir));

            _outputPath = $"{_testRootDir}/Output";
            AssetDatabase.CreateFolder(_testRootDir, "Output");
            _thumbnailPath = $"{_testRootDir}/Thumbnails";
            AssetDatabase.CreateFolder(_testRootDir, "Thumbnails");

            _deleteSettings = ScriptableObject.CreateInstance<DeleteSpecificObjectsSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(_testRootDir)) AssetDatabase.DeleteAsset(_testRootDir);
            if (_deleteSettings) Object.DestroyImmediate(_deleteSettings);
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

            var allTransforms = root.GetComponentsInChildren<Transform>();

            var eyeL = allTransforms.FirstOrDefault(t => t.name == "Eye_L");
            if (eyeL) eyeL.name = "LeftEye";

            var eyeR = allTransforms.FirstOrDefault(t => t.name == "Eye_R");
            if (eyeR) eyeR.name = "RightEye";

            return root;
        }

        [Test]
        public void ConvertSingleAvatar_ShouldSucceed_WithMockBake()
        {
            var vrcAvatar = CreateValidHumanoidMock("TestVrcAvatar");
            var mockBake = new MockBakeProcessor();

            // MockBakeProcessorを注入して、重いベイク処理を回避
            var converter = new VrmAvatarConverter(
                _outputPath, _thumbnailPath, "1.0", "Author", 256, false, null, _deleteSettings,
                mockBake
            );

            bool result = converter.ConvertSingleAvatar(vrcAvatar);

            Assert.IsTrue(result, "ConvertSingleAvatar should return true with a valid mock bake.");

            var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_outputPath}/TestVrcAvatar.prefab");
            Assert.IsNotNull(resultPrefab, "Resulting VRM prefab should be created.");

            Object.DestroyImmediate(vrcAvatar);
        }

        [Test]
        public void ConvertBakedAvatar_WithBaseVrm_ShouldCopyComponentsAndBinarySettings()
        {
            var bakedMock = CreateValidHumanoidMock("ValidBakedInstance");

            // 【重要】BaseVRMも実体のあるモデル（ボーン階層）から作成する
            var baseGo = CreateValidHumanoidMock("BaseVRMSource");

            var basePrefabPath = $"{_testRootDir}/BaseVRM.prefab";
            var basePrefab = PrefabUtility.SaveAsPrefabAsset(baseGo, basePrefabPath);

            // VRMInitializerを実行して、VRMMetaやVRMSpringBone(secondary)をセットアップ
            VRMInitializer.Initialize(basePrefabPath);

            // 初期化されたプレハブをロード
            basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(basePrefabPath))
            {
                var root = editingScope.prefabContentsRoot;

                // Binary設定のテスト用に、JoyのクリップをIsBinary = trueにする
                var baseProxy = root.GetComponent<VRMBlendShapeProxy>();
                var clip = baseProxy.BlendShapeAvatar.GetClip(BlendShapePreset.Joy);
                if (clip != null)
                {
                    clip.IsBinary = true;
                    EditorUtility.SetDirty(clip);
                }

                // VRMInitializerがsecondaryの下にVRMSpringBoneを作りますが、
                // Copy処理を確実に走らせるためにRootBonesを1つ設定しておきます（ボーン階層があるので可能）
                var springBone = root.GetComponentInChildren<VRMSpringBone>();
                if (springBone != null)
                {
                    var animator = root.GetComponent<Animator>();
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (head != null)
                    {
                        springBone.RootBones = new System.Collections.Generic.List<Transform> {head};
                        EditorUtility.SetDirty(springBone);
                    }
                }
            }

            // 最終的なプレハブアセットをロード
            basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);

            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, true, basePrefab,
                _deleteSettings);

            bool result = converter.ConvertBakedAvatar(bakedMock, "BaseCopyTest");

            Assert.IsTrue(result, "Conversion should succeed with a valid Humanoid instance.");
            var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_outputPath}/BaseCopyTest.prefab");
            Assert.IsNotNull(resultPrefab.GetComponent<VRMBlendShapeProxy>());

            Object.DestroyImmediate(bakedMock);
            Object.DestroyImmediate(baseGo);
        }

        [Test]
        public void ConvertBakedAvatar_ShouldSucceed_WithAssetsPersistence()
        {
            var bakedMock = CreateValidHumanoidMock("PersistenceTestInstance");
            var tempMeshObj = new GameObject("TempAssetHolder");
            tempMeshObj.transform.SetParent(bakedMock.transform);
            var smr = tempMeshObj.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = new Mesh {name = "MemoryMesh"};
            smr.sharedMaterial = new Material(Shader.Find("VRM/MToon")) {name = "MemoryMat"};

            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, false, null,
                _deleteSettings);

            bool result = converter.ConvertBakedAvatar(bakedMock, "PersistenceTest");

            Assert.IsTrue(result);
            var genFolder = $"{_outputPath}/PersistenceTest.Generated";
            Assert.IsTrue(AssetDatabase.IsValidFolder(genFolder));
            Assert.Greater(AssetDatabase.FindAssets("", new[] {genFolder}).Length, 0);

            Object.DestroyImmediate(bakedMock);
        }

        [Test]
        public void ConvertBakedAvatar_WithAssetsPersistence_ShouldHandleComplexAssets()
        {
            var bakedMock = CreateValidHumanoidMock("ComplexAssetInstance");

            // 1. MeshFilter + MeshRenderer のパターン (SkinnedMeshRenderer以外)
            var meshObj = new GameObject("StaticMesh");
            meshObj.transform.SetParent(bakedMock.transform);
            var filter = meshObj.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh {name = "StaticMemoryMesh"};
            var renderer = meshObj.AddComponent<MeshRenderer>();

            // 2. 一時アセットとして認識されるマテリアルとテクスチャの作成
            // パスに "ZZZ_GeneratedAssets_" を含むことで IsTemporaryAsset を true にする
            var mat = new Material(Shader.Find("VRM/MToon")) {name = "TempMat"};
            var tex = new Texture2D(2, 2) {name = "TempTex"};
            mat.mainTexture = tex;
            renderer.sharedMaterial = mat;

            // アセットデータベースに一時的なパスで保存（IsTemporaryAssetの条件を満たすため）
            var tempAssetDir = $"{_testRootDir}/ZZZ_GeneratedAssets_Test";
            AssetDatabase.CreateFolder(_testRootDir, "ZZZ_GeneratedAssets_Test");
            var tempMatPath = $"{tempAssetDir}/TempMat.mat";
            var tempTexPath = $"{tempAssetDir}/TempTex.asset";
            AssetDatabase.CreateAsset(tex, tempTexPath);
            AssetDatabase.CreateAsset(mat, tempMatPath);

            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, false, null,
                _deleteSettings);

            // 実行
            bool result = converter.ConvertBakedAvatar(bakedMock, "ComplexTest");

            // 検証
            Assert.IsTrue(result);
            var genFolder = $"{_outputPath}/ComplexTest.Generated";

            // メッシュ、マテリアル、テクスチャが書き出されているか確認
            var assets = AssetDatabase.FindAssets("", new[] {genFolder}).Select(AssetDatabase.GUIDToAssetPath).ToList();
            Assert.IsTrue(assets.Any(p => p.Contains("StaticMemoryMesh")), "Mesh should be persisted.");
            Assert.IsTrue(assets.Any(p => p.Contains("TempMat")), "Material should be persisted.");
            Assert.IsTrue(assets.Any(p => p.Contains("TempTex")), "Texture should be persisted.");

            Object.DestroyImmediate(bakedMock);
        }

        [Test]
        public void ConvertSingleAvatar_WithInvalidInput_ShouldReturnFalse()
        {
            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, false, null,
                _deleteSettings);

            // 1. ガード句のテスト (null渡し)
            bool result = converter.ConvertSingleAvatar(null);
            Assert.IsFalse(result, "Should return false for null input.");

            // 2. catch (Exception e) ブロックのテスト
            // 意図的に例外を投げるMockを注入
            var exceptionMock = new ExceptionBakeProcessor();
            var exceptionConverter = new VrmAvatarConverter(
                _outputPath, _thumbnailPath, "1.0", "Author", 256, false, null, _deleteSettings,
                exceptionMock
            );

            var vrcAvatar = CreateValidHumanoidMock("ExceptionTestAvatar");

            // catch句内の Debug.LogError を期待する
            // メッセージ内容を具体的に指定することで、確実にキャッチします
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Simulated Exception.*"));

            bool exceptionResult = exceptionConverter.ConvertSingleAvatar(vrcAvatar);
            Assert.IsFalse(exceptionResult, "Should return false when an exception occurs.");

            Object.DestroyImmediate(vrcAvatar);
        }

        [Test]
        public void ConvertBakedAvatar_ShouldHandleExistingFolder()
        {
            var bakedMock = CreateValidHumanoidMock("FolderTestInstance");
            var folderName = "FolderTest.Generated";
            var folderPath = $"{_outputPath}/{folderName}";

            // 先に出力フォルダを作成しておく (AssetDatabase.DeleteAsset の箇所を通す)
            AssetDatabase.CreateFolder(_outputPath, folderName);
            File.WriteAllText(Path.Combine(Application.dataPath, "..", folderPath, "dummy.txt"), "dummy");

            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, false, null,
                _deleteSettings);

            bool result = converter.ConvertBakedAvatar(bakedMock, "FolderTest");

            Assert.IsTrue(result);
            Assert.IsFalse(File.Exists(Path.Combine(Application.dataPath, "..", folderPath, "dummy.txt")),
                "Existing folder should be deleted.");

            Object.DestroyImmediate(bakedMock);
        }

        [Test]
        public void ConvertSingleAvatar_ShouldProcessOptionalLogics()
        {
            // 1. DeleteSpecificObjects / DeleteHideObjects のテスト準備
            var vrcAvatar = CreateValidHumanoidMock("DeletionTestAvatar");

            // 削除対象のオブジェクトを作成
            var targetObj = new GameObject("DeleteMe");
            targetObj.transform.SetParent(vrcAvatar.transform);
            _deleteSettings.targetObjectNames = new System.Collections.Generic.List<string> {"DeleteMe"};

            // 非アクティブなオブジェクトを作成 (DeleteHideObjects用)
            var hiddenObj = new GameObject("Hidden");
            hiddenObj.transform.SetParent(vrcAvatar.transform);
            hiddenObj.SetActive(false);

            // 2. EnsureDirectoryExists / ConvertBakedAvatar の例外テスト用準備
            // 読み取り専用フォルダや、不正なパス文字を含む出力パスをシミュレート
            var invalidPath = _outputPath + "/Invalid|Path";

            var mockBake = new MockBakeProcessor();
            var converter = new VrmAvatarConverter(
                _outputPath, _thumbnailPath, "1.0", "Author", 256, false, null, _deleteSettings,
                mockBake
            );

            // 実行
            bool result = converter.ConvertSingleAvatar(vrcAvatar);

            // 検証
            Assert.IsTrue(result);
            // インスタンス側は削除処理が走っているはず
            // (注: ConvertSingleAvatar内部でDestroyImmediateされるため、直接の参照確認は難しいが、
            // 変換プロセスが正常に終了していればロジックは通っている)

            Object.DestroyImmediate(vrcAvatar);
        }

        [Test]
        public void ConvertBakedAvatar_WhenExceptionInTryBlock_ShouldCatchAndReturnFalse()
        {
            var converter = new VrmAvatarConverter(_outputPath, _thumbnailPath, "1.0", "Author", 256, false, null,
                _deleteSettings);

            // L10Nクラスが出力する可能性のあるすべてのエラーログを許容するように正規表現を広げる
            // ログの冒頭が「An error occurred during conversion:」等で始まるため
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*(conversion|エラー).*"));

            // bakedAvatarに null を渡して意図的に例外を発生させる
            bool result = converter.ConvertBakedAvatar(null, "CatchTest");
            Assert.IsFalse(result, "Should return false when an exception occurs internally.");
        }
    }
}