using NUnit.Framework;
using Fara.Fara_VRMMultiConverter.Editor;

namespace Fara.Tests.Editor
{
    [TestFixture]
    public class VrmThumbnailGeneratorTests
    {
        private const string TestThumbnailPath = "Assets/Fara/Tests/TestThumbnails";

        [Test]
        public void ThumbnailExists_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var prefabName = "NonExistentPrefab_12345";

            // Act
            var result = VrmThumbnailGenerator.ThumbnailExists(prefabName, TestThumbnailPath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ThumbnailExists_WithEmptyName_ReturnsFalse()
        {
            // Arrange
            var prefabName = "";

            // Act
            var result = VrmThumbnailGenerator.ThumbnailExists(prefabName, TestThumbnailPath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        [TestCase("TestAvatar(Clone)", "TestAvatar")]
        [TestCase("TestAvatarVariant", "TestAvatar")]
        [TestCase("TestAvatar", "TestAvatar")]
        public void CleanName_RemovesCloneAndVariant(string input, string expected)
        {
            // Act
            var result = input.Replace("(Clone)", "").Replace("Variant", "").Trim();

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}