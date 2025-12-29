using System.IO;
using NUnit.Framework;
using UnityEditor;
using Fara.FaraVRMMultiConverter.Editor;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    public class VrmConverterSettingsUtilityTests
    {
        private const string TestSettingsPath = "Assets/Test_DeleteSettings.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestSettingsPath);
        }

        [Test]
        public void CreateSettingsAsset_WhenValidPathProvided_ShouldCreateAssetAtLocation()
        {
            var settings = VrmConverterSettingsUtility.CreateSettingsAsset(TestSettingsPath);

            Assert.IsNotNull(settings);
            Assert.IsTrue(File.Exists(Path.GetFullPath(TestSettingsPath)));

            var loaded = AssetDatabase.LoadAssetAtPath<DeleteSpecificObjectsSettings>(TestSettingsPath);
            Assert.AreEqual(settings, loaded);
        }

        [Test]
        public void CreateSettingsAsset_WhenNestedDirectoryDoesNotExist_ShouldCreateDirectoriesAndAsset()
        {
            // 深い階層の存在しないパスを指定する
            const string nestedPath = "Assets/TestFolder/SubFolder/DeepFolder/Test_DeleteSettings.asset";
            const string rootTestFolder = "Assets/TestFolder";

            try
            {
                // メソッドを呼び出す（内部で EnsureDirectoryExists が走り、赤かったループが実行される）
                var settings = VrmConverterSettingsUtility.CreateSettingsAsset(nestedPath);

                // アセットが作成されているか確認
                Assert.IsNotNull(settings);
                Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/TestFolder/SubFolder/DeepFolder"));
                Assert.IsTrue(File.Exists(Path.GetFullPath(nestedPath)));
            }
            finally
            {
                // テスト後に作成したフォルダごと削除してクリーンアップ
                if (AssetDatabase.IsValidFolder(rootTestFolder)) AssetDatabase.DeleteAsset(rootTestFolder);
            }
        }
    }
}