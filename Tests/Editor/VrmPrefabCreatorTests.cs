using NUnit.Framework;
using UnityEngine;
using Fara.Fara_VRMMultiConverter.Editor;

namespace Fara.Tests.Editor
{
    [TestFixture]
    public class VrmPrefabCreatorTests
    {
        private GameObject _testAvatar;
        private const string TestOutputPath = "Assets/Fara/Tests/TestOutput";

        [SetUp]
        public void SetUp()
        {
            // テスト用のGameObjectを作成
            _testAvatar = new GameObject("TestAvatar");
            var animator = _testAvatar.AddComponent<Animator>();
            
            // Humanoidアニメーターの設定（簡易版）
            // 実際のテストではモックやテストダブルを使用することを推奨
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後のクリーンアップ
            if (_testAvatar != null)
            {
                Object.DestroyImmediate(_testAvatar);
            }
        }

        [Test]
        public void Constructor_WithValidPath_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new VrmPrefabCreator(TestOutputPath));
        }

        [Test]
        public void CreateVrmPrefab_WithNullAvatar_ReturnsEmptyString()
        {
            // Arrange
            var creator = new VrmPrefabCreator(TestOutputPath);

            // Act
            var result = creator.CreateVrmPrefab(null);

            // Assert
            Assert.IsEmpty(result);
        }
    }
}