using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Fara.FaraVRMMultiConverter.Editor;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    public class VrmConverterListUtilityTests
    {
        [Test]
        public void AddUniqueGameObjects_WhenCalledWithDuplicates_ShouldAddOnlyUniqueObjects()
        {
            var list = new List<GameObject>();
            var obj = new GameObject("Test");

            VrmConverterListUtility.AddUniqueGameObjects(list, new[] {obj, obj, null});

            Assert.AreEqual(1, list.Count);
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void GetIndexToRemove_WhenNullExistsAtEnd_ShouldReturnNullIndex()
        {
            var obj = new GameObject("Test");
            var list = new List<GameObject> {obj, null};

            var index = VrmConverterListUtility.GetIndexToRemove(list, -1);

            Assert.AreEqual(1, index);
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void GetIndexToRemove_WhenFocusedIndexIsValid_ShouldReturnFocusedIndex()
        {
            var obj = new GameObject("Test");
            var list = new List<GameObject> {obj, obj};

            var index = VrmConverterListUtility.GetIndexToRemove(list, 0);

            Assert.AreEqual(0, index);
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void GetIndexToRemove_WhenNoNullExistsAndNoFocus_ShouldReturnLastIndex()
        {
            // 全ての要素が有効（nullがない）かつ、フォーカス指定がない(-1)状態を作る
            var obj1 = new GameObject("Test1");
            var obj2 = new GameObject("Test2");
            var list = new List<GameObject> {obj1, obj2};

            // lastFocusedIndex に -1 を渡すと、ループで null が見つからず、
            // 最終的に return targetList.Count - 1; に到達する
            var index = VrmConverterListUtility.GetIndexToRemove(list, -1);

            Assert.AreEqual(1, index); // 末尾のインデックス(1)が返ることを期待

            Object.DestroyImmediate(obj1);
            Object.DestroyImmediate(obj2);
        }
    }
}