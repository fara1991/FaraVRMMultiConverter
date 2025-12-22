using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    internal class DeleteUtility : MonoBehaviour
    {
        /// <summary>
        /// 対象オブジェクト以下すべてのVRCコンポーネントを削除
        /// </summary>
        internal static void DeleteVrcComponentsRecursive(GameObject target)
        {
            var vrcComponents = target.GetComponentsInChildren<Component>(true)
                .Where(c => c && c.GetType().Namespace != null &&
                            c.GetType().Namespace!.Contains("VRC"))
                .ToArray();

            foreach (var component in vrcComponents)
            {
                DestroyImmediate(component);
            }
        }

        /// <summary>
        /// 再帰的にすべての特定オブジェクトを削除
        /// </summary>
        internal static int DeleteSpecificObjectsRecursive(Transform parent, List<string> targetNames)
        {
            var count = 0;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);

                if (targetNames.Contains(child.name))
                {
                    Debug.Log($"削除: {child.name} (親: {parent.name})");
                    DestroyImmediate(child.gameObject);
                    count++;
                }
                else
                {
                    // 削除されなかった子オブジェクトの中も再帰的に検索
                    count += DeleteSpecificObjectsRecursive(child, targetNames);
                }
            }

            return count;
        }
    }
}