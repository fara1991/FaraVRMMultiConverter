using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    public static class VrmConverterListUtility
    {
        /// <summary>
        /// 重複を除いてGameObjectをリストに追加するロジック
        /// </summary>
        public static void AddUniqueGameObjects(List<GameObject> targetList, IEnumerable<GameObject> additions)
        {
            if (additions == null) return;
            foreach (var obj in additions.Where(obj => obj != null))
            {
                if (!targetList.Contains(obj))
                {
                    targetList.Add(obj);
                }
            }
        }

        /// <summary>
        /// 削除すべきインデックスを決定するロジック
        /// 1. 指定インデックスが有効ならそれを返す
        /// 2. そうでなければ末尾のnullのインデックスを返す
        /// 3. nullがなければ末尾のインデックスを返す
        /// </summary>
        public static int GetIndexToRemove(List<GameObject> targetList, int lastFocusedIndex)
        {
            if (targetList == null || targetList.Count == 0) return -1;

            if (lastFocusedIndex >= 0 && lastFocusedIndex < targetList.Count)
            {
                return lastFocusedIndex;
            }

            for (var i = targetList.Count - 1; i >= 0; i--)
            {
                if (targetList[i] == null) return i;
            }

            return targetList.Count - 1;
        }
    }
}