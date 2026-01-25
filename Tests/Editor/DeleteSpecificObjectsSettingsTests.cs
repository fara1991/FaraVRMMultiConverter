using Fara.FaraMultiVrmConverter.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Fara.FaraMultiVrmConverter.Tests.Editor
{
    [TestFixture]
    public class DeleteSpecificObjectsSettingsTests
    {
        private DeleteSpecificObjectsSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DeleteSpecificObjectsSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Settings_InitialValues_AreDefault()
        {
            Assert.IsNotNull(_settings.targetObjectNames);
            Assert.AreEqual(0, _settings.targetObjectNames.Count);
            Assert.IsFalse(_settings.useRegex);
        }
    }
}