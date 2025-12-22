using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Fara.FaraVRMMultiConverter.Editor;

namespace Fara.Tests.Editor
{
    [TestFixture]
    public class DeleteUtilityTests
    {
        private GameObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestParent");
            var child1 = new GameObject("TargetObject");
            child1.transform.SetParent(_testObject.transform);
            var child2 = new GameObject("KeepObject");
            child2.transform.SetParent(_testObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void DeleteSpecificObjectsRecursive_WithEmptyList_ReturnsZero()
        {
            // Arrange
            var targetNames = new List<string>();

            // Act
            var result = DeleteUtility.DeleteSpecificObjectsRecursive(_testObject.transform, targetNames);

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void DeleteSpecificObjectsRecursive_WithValidName_DeletesObject()
        {
            // Arrange
            var targetNames = new List<string> { "TargetObject" };
            var initialCount = _testObject.transform.childCount;

            // Act
            var deletedCount = DeleteUtility.DeleteSpecificObjectsRecursive(_testObject.transform, targetNames);

            // Assert
            Assert.AreEqual(1, deletedCount);
            Assert.AreEqual(initialCount - 1, _testObject.transform.childCount);
        }
    }
}