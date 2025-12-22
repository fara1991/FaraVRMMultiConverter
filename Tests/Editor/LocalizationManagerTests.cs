using NUnit.Framework;
using Fara.FaraVRMMultiConverter.Editor;

namespace Fara.Tests.Editor
{
    [TestFixture]
    public class LocalizationManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            // テスト前の初期化
            LocalizationManager.IsJapanese = true;
        }

        [Test]
        public void Get_WhenJapanese_ReturnsJapaneseText()
        {
            // Arrange
            LocalizationManager.IsJapanese = true;

            // Act
            var result = LocalizationManager.Get("テスト", "Test");

            // Assert
            Assert.AreEqual("テスト", result);
        }

        [Test]
        public void Get_WhenEnglish_ReturnsEnglishText()
        {
            // Arrange
            LocalizationManager.IsJapanese = false;

            // Act
            var result = LocalizationManager.Get("テスト", "Test");

            // Assert
            Assert.AreEqual("Test", result);
        }

        [Test]
        public void AvatarElement_ReturnsCorrectFormat()
        {
            // Arrange
            LocalizationManager.IsJapanese = true;

            // Act
            var result = L10N.Converter.AvatarElement(0);

            // Assert
            Assert.IsTrue(result.Contains("0"));
        }

        [Test]
        public void ConversionSummary_ReturnsCorrectFormat()
        {
            // Arrange
            LocalizationManager.IsJapanese = true;

            // Act
            var result = L10N.Converter.ConversionSummary(5, 2, 7);

            // Assert
            Assert.IsTrue(result.Contains("5"));
            Assert.IsTrue(result.Contains("2"));
            Assert.IsTrue(result.Contains("7"));
        }
    }
}