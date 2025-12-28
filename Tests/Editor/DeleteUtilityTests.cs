using System.Collections.Generic;
using System.Linq;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class DeleteUtilityTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void DeleteSpecificObjectsRecursive_WithExactMatch_DeletesObjects()
        {
            // Arrange
            var child1 = new GameObject("DeleteMe");
            child1.transform.SetParent(_root.transform);
            var child2 = new GameObject("KeepMe");
            child2.transform.SetParent(_root.transform);
            
            var targetNames = new List<string> { "DeleteMe" };

            // Act
            var count = DeleteUtility.DeleteSpecificObjectsRecursive(_root.transform, targetNames, false);

            // Assert
            Assert.AreEqual(1, count);
            Assert.IsNull(GameObject.Find("DeleteMe"));
            Assert.IsNotNull(GameObject.Find("KeepMe"));
        }

        [Test]
        public void DeleteSpecificObjectsRecursive_WithRegexWildcard_DeletesObjects()
        {
            // Arrange
            // '*' が '.*' として扱われるかのテスト
            new GameObject("VRC_Contact_Receiver").transform.SetParent(_root.transform);
            new GameObject("VRC_PhysBone").transform.SetParent(_root.transform);
            new GameObject("NormalObject").transform.SetParent(_root.transform);
            
            var targetNames = new List<string> { "VRC_*" };

            // Act
            var count = DeleteUtility.DeleteSpecificObjectsRecursive(_root.transform, targetNames, true);

            // Assert
            Assert.AreEqual(2, count);
            Assert.IsNull(GameObject.Find("VRC_Contact_Receiver"));
            Assert.IsNull(GameObject.Find("VRC_PhysBone"));
            Assert.IsNotNull(GameObject.Find("NormalObject"));
        }

        [Test]
        public void DeleteSpecificObjectsRecursive_NestedObjects_DeletesRecursively()
        {
            // Arrange
            var parent = new GameObject("Parent");
            parent.transform.SetParent(_root.transform);
            
            var target = new GameObject("DeleteMe");
            target.transform.SetParent(parent.transform); // 階層の下にある
            
            var targetNames = new List<string> { "DeleteMe" };

            // Act
            var count = DeleteUtility.DeleteSpecificObjectsRecursive(_root.transform, targetNames, false);

            // Assert
            Assert.AreEqual(1, count);
            Assert.IsNull(GameObject.Find("DeleteMe"));
            Assert.IsNotNull(GameObject.Find("Parent"));
        }

        [Test]
        public void DeleteVrcComponentsRecursive_RemovesOnlyVrcComponents()
        {
            // Arrange
            var target = new GameObject("ComponentContainer");
            target.transform.SetParent(_root.transform);
            
            // モックとして、Namespaceに "VRC" が含まれるコンポーネントをシミュレートするのは難しいため、
            // 実際には VRChat SDK が入っている環境であればそのコンポーネントを使用しますが、
            // テストを安定させるために、ここでは「NamespaceがVRCを含むか」のロジックが通ることを期待します。
            
            // 注意: ユニットテスト環境に VRChat SDK がない場合、実際の VRC コンポーネントは追加できません。
            // もし独自の Namespace モックを作りたい場合は、継承したクラスを作成します。
            
            target.AddComponent<BoxCollider>(); // VRCではない
            
            // Act
            DeleteUtility.DeleteVrcComponentsRecursive(target);

            // Assert
            Assert.IsNotNull(target.GetComponent<BoxCollider>(), "VRC以外のコンポーネントは残るべき");
        }

        [Test]
        public void IsMatch_EmptyPattern_ReturnsFalse()
        {
            // DeleteUtility の private メソッド IsMatch は reflection で呼ぶか、
            // DeleteSpecificObjectsRecursive を通じて空文字パターンをテストします。
            
            // Arrange
            new GameObject("AnyName").transform.SetParent(_root.transform);
            var targetNames = new List<string> { "", null }; // 空のパターン

            // Act
            var count = DeleteUtility.DeleteSpecificObjectsRecursive(_root.transform, targetNames, true);

            // Assert
            Assert.AreEqual(0, count, "空のパターンでは何も削除されないべき");
        }
        
        [Test]
        public void DeleteVrcComponentsRecursive_WhenVrcComponentExists_ShouldDestroyThem()
        {
            // 1. セットアップ: オブジェクトを作成
            var root = new GameObject("Root");
            
            // 削除対象のVRCコンポーネントを追加
            root.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            // 削除されないはずのコンポーネントを追加（検証ループ内のアサーションを実行させるため）
            root.AddComponent<MeshFilter>();

            // 削除前の確認
            var beforeComponents = root.GetComponentsInChildren<Component>(true);
            Assert.IsTrue(beforeComponents.Any(c => c.GetType().Namespace?.Contains("VRC") ?? false));

            // 2. 実行: 内部で DestroyImmediate が走る
            DeleteUtility.DeleteVrcComponentsRecursive(root);

            // 3. 検証: rootに残っているコンポーネントをチェック
            // GetComponentsInChildren(true) は生きているコンポーネントのみを返す
            var afterComponents = root.GetComponentsInChildren<Component>(true);
            
            foreach (var c in afterComponents)
            {
                // Transform以外かつ、生きているコンポーネント（MeshFilter等）に対して
                // アサーションを行うことで、画像で赤かった行を実行させる
                if (c == null || c is Transform) continue;

                // 残っているもの全ての名前空間に "VRC" が含まれないことを検証（ここが実行される）
                Assert.IsFalse(c.GetType().Namespace?.Contains("VRC") ?? false);
            }

            Object.DestroyImmediate(root);
        }
    }
}