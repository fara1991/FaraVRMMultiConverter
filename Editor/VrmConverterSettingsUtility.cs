using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    public static class VrmConverterSettingsUtility
    {
        public static DeleteSpecificObjectsSettings CreateSettingsAsset(string path)
        {
            EnsureDirectoryExists(Path.GetDirectoryName(path));

            var settings = ScriptableObject.CreateInstance<DeleteSpecificObjectsSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory)) return;

            var folders = directory.Replace('\\', '/').Split('/');
            var currentPath = folders[0];
            for (var i = 1; i < folders.Length; i++)
            {
                var newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }
}