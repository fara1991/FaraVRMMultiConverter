using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        internal static int DeleteSpecificObjectsRecursive(Transform parent, List<string> targetNames, bool useRegex)
        {
            var count = 0;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);

                if (IsMatch(child.name, targetNames, useRegex))
                {
                    Debug.Log($"削除: {child.name} (親: {parent.name})");
                    DestroyImmediate(child.gameObject);
                    count++;
                }
                else
                {
                    count += DeleteSpecificObjectsRecursive(child, targetNames, useRegex);
                }
            }

            return count;
        }
        
        private static bool IsMatch(string name, List<string> patterns, bool useRegex)
        {
            if (useRegex)
            {
                return patterns.Any(p => 
                {
                    if (string.IsNullOrEmpty(p)) return false;
                    // ワイルドカード '*' を正規表現 '.*' に変換して判定
                    var regexPattern = "^" + Regex.Escape(p).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    return Regex.IsMatch(name, regexPattern, RegexOptions.IgnoreCase);
                });
            }
            return patterns.Contains(name);
        }
    }
}