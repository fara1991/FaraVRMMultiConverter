using System;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    /// <summary>
    /// ベイク処理のインターフェース
    /// </summary>
    public interface IVrmBakeProcessor
    {
        GameObject ProcessBake(GameObject vrcAvatarInstance, string avatarName);
    }

    /// <summary>
    /// デフォルトのベイク処理実装
    /// </summary>
    public class DefaultVrmBakeProcessor : IVrmBakeProcessor
    {
        public GameObject ProcessBake(GameObject vrcAvatarInstance, string avatarName)
        {
            // 以前 ConvertSingleAvatar 内に直書きされていたロジックをここに集約
            var uniqueTempDir = $"Assets/ZZZ_GeneratedAssets_{avatarName}_{Guid.NewGuid().ToString()[..8]}";
            using (new OverrideTemporaryDirectoryScope(uniqueTempDir))
            {
                AvatarProcessor.ProcessAvatar(vrcAvatarInstance);
            }

            return FindBakedAvatar(vrcAvatarInstance.name);
        }
        
        private static GameObject FindBakedAvatar(string name)
        {
            var rootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjs)
            {
                if (obj.name == name) return obj;
            }
            return rootObjs.FirstOrDefault(obj => obj.name.Contains(name));
        }
    }
}