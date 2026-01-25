using System.Collections.Generic;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    [CreateAssetMenu(fileName = "DeleteSpecificObjectsSettings", menuName = "Fara/DeleteSpecificObjectsSettings")]
    public class DeleteSpecificObjectsSettings : ScriptableObject
    {
        [Tooltip("削除したいオブジェクトの名前を追加してください")] public List<string> targetObjectNames = new();

        [Tooltip("正規表現を使用してマッチングします（* を任意の文字列として扱えます）")]
        public bool useRegex;
    }
}