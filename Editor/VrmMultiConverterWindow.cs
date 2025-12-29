using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fara.FaraVRMMultiConverter.Editor
{
    [ExcludeFromCodeCoverage]
    public class VrmMultiConverterWindow : EditorWindow
    {
        private const string LanguagePrefKey = "VrmMultiConverterWindow_SettingsPath";

        private readonly List<GameObject> _selectedVrcPrefabs = new();
        private bool _isVrmComponentCopy;
        private GameObject _baseVrmPrefab;
        private string _vrmOutputPath = "Assets/Fara/Prefab/VRM";
        private string _vrmThumbnailPath = "Assets/Fara/Thumbnails";
        private string _vrmVersion = "v4.0.0";
        private string _vrmAuthor = "Fara";

        private VrmThumbnailGenerator.ThumbnailResolution _thumbnailResolution =
            VrmThumbnailGenerator.ThumbnailResolution.Resolution1024;

        private static string _settingsPath =
            "Assets/Fara/FaraVRMMultiConverter/Editor/DeleteSpecificObjectsSettings.asset";

        private static DeleteSpecificObjectsSettings _settings;
        private Vector2 _scrollPosition;
        private int _lastFocusedIndex = -1;

        private enum Language
        {
            Japanese,
            English
        }

        [MenuItem("FaraScripts/VRM Multi-Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<VrmMultiConverterWindow>(L10N.Converter.WindowTitle);
            window.minSize = new Vector2(500, 300);

            if (EditorPrefs.HasKey(LanguagePrefKey))
            {
                _settingsPath = EditorPrefs.GetString(LanguagePrefKey);
            }

            _settings = AssetDatabase.LoadAssetAtPath<DeleteSpecificObjectsSettings>(_settingsPath);
        }

        private void OnEnable()
        {
            if (!EditorPrefs.HasKey(LanguagePrefKey)) return;

            _settingsPath = EditorPrefs.GetString(LanguagePrefKey);
            _settings = AssetDatabase.LoadAssetAtPath<DeleteSpecificObjectsSettings>(_settingsPath);
        }

        private void OnGUI()
        {
            // スクロールビューを追加
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space();

            // 言語切り替え
            GUILayout.Label(L10N.Converter.Language, EditorStyles.boldLabel);
            var newLanguage = (Language) EditorGUILayout.EnumPopup(
                LocalizationManager.IsJapanese ? Language.Japanese : Language.English,
                GUILayout.ExpandWidth(true)
            );
            var newIsJapanese = newLanguage == Language.Japanese;
            if (newIsJapanese != LocalizationManager.IsJapanese)
            {
                LocalizationManager.IsJapanese = newIsJapanese;
                Repaint();
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label(L10N.Converter.HowToUseHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10N.Converter.Instructions, MessageType.Info);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // VRNアバター設定
            GUILayout.Label(L10N.Converter.PathSettings, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(L10N.Converter.SourceVrcPrefab);

            // 変更を検知
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(L10N.Converter.AvatarCount);

            var newSize = EditorGUILayout.IntField(_selectedVrcPrefabs.Count, GUILayout.ExpandWidth(true));
            if (newSize != _selectedVrcPrefabs.Count)
            {
                if (newSize < 0) newSize = 0;

                while (newSize > _selectedVrcPrefabs.Count)
                {
                    _selectedVrcPrefabs.Add(null);
                }

                while (newSize < _selectedVrcPrefabs.Count)
                {
                    _selectedVrcPrefabs.RemoveAt(_selectedVrcPrefabs.Count - 1);
                }
            }

            // プラス・マイナスボタン
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                _selectedVrcPrefabs.Add(null);
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                // 最後のnullを削除、なければ選択中(フォーカス中)のアバターを削除
                RemoveLastOrSelected();
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < _selectedVrcPrefabs.Count; i++)
            {
                var rect = EditorGUILayout.GetControlRect();

                // マウスクリックを検知してインデックスを記録
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _lastFocusedIndex = i;
                }

                _selectedVrcPrefabs[i] = (GameObject) EditorGUI.ObjectField(
                    rect,
                    L10N.Converter.AvatarElement(i),
                    _selectedVrcPrefabs[i],
                    typeof(GameObject),
                    false
                );
            }

            // 追加用の空のElement（複数ドロップ対応）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(L10N.Converter.AvatarElement(_selectedVrcPrefabs.Count));

            var lastFieldRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            var handled = HandleMultipleObjectDrop(lastFieldRect);

            // 複数ドロップが処理されなかった場合のみ通常のObjectFieldを表示
            if (!handled)
            {
                var newObject = (GameObject) EditorGUI.ObjectField(
                    lastFieldRect,
                    null,
                    typeof(GameObject),
                    false
                );

                // 単一オブジェクトが追加された場合
                if (newObject is not null)
                {
                    VrmConverterListUtility.AddUniqueGameObjects(_selectedVrcPrefabs, new[] { newObject });
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            _isVrmComponentCopy = EditorGUILayout.ToggleLeft(
                new GUIContent(L10N.Converter.IsVrmComponentCopy),
                _isVrmComponentCopy,
                GUILayout.ExpandWidth(true)
            );

            // チェックがついている場合のみ表示
            if (_isVrmComponentCopy)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField(L10N.Converter.TargetVrmBasePrefab);
                _baseVrmPrefab = (GameObject) EditorGUILayout.ObjectField(
                    _baseVrmPrefab,
                    typeof(GameObject),
                    false,
                    GUILayout.ExpandWidth(true)
                );

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(L10N.Converter.VrmOutputPath);
            _vrmOutputPath = EditorGUILayout.TextField(_vrmOutputPath, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label(L10N.Converter.VrmMetaInformation, EditorStyles.boldLabel);

            // VRN Meta情報
            EditorGUILayout.LabelField(L10N.Converter.Version);
            _vrmVersion = EditorGUILayout.TextField(_vrmVersion, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(L10N.Converter.Author);
            _vrmAuthor = EditorGUILayout.TextField(_vrmAuthor, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label(L10N.Converter.ThumbnailSettings, EditorStyles.boldLabel);

            // サムネイル設定
            EditorGUILayout.LabelField(L10N.Converter.ThumbnailPath);
            _vrmThumbnailPath = EditorGUILayout.TextField(_vrmThumbnailPath, GUILayout.ExpandWidth(true));

            var thumbnailStatusList = _selectedVrcPrefabs
                .Where(prefab => prefab is not null)
                .Select(prefab => new
                {
                    Name = prefab.name,
                    Exists = VrmThumbnailGenerator.ThumbnailExists(
                        prefab.name.Replace("(Clone)", "").Replace("Variant", "").Trim(),
                        _vrmThumbnailPath)
                })
                .ToList();

            if (thumbnailStatusList.Any(s => s.Exists))
            {
                var existingNames = string.Join(", ", thumbnailStatusList.Where(s => s.Exists).Select(s => s.Name));
                EditorGUILayout.HelpBox(L10N.Converter.ThumbnailsAlreadyExist(existingNames), MessageType.Warning);
            }

            var allThumbnailsExist =
                thumbnailStatusList.Count > 0 && thumbnailStatusList.All(thumbnail => thumbnail.Exists);
            GUI.enabled = !allThumbnailsExist;

            EditorGUILayout.LabelField(L10N.Converter.ThumbnailResolution);
            _thumbnailResolution = (VrmThumbnailGenerator.ThumbnailResolution) EditorGUILayout.EnumPopup(
                _thumbnailResolution,
                GUILayout.ExpandWidth(true)
            );
            GUI.enabled = true;
            if (allThumbnailsExist && thumbnailStatusList.Count > 0)
            {
                EditorGUILayout.HelpBox(L10N.Converter.AllThumbnailsExist, MessageType.Info);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 設定ファイル
            GUILayout.Label(L10N.Converter.SettingsFileHeader, EditorStyles.boldLabel);

            EditorGUILayout.LabelField(L10N.Converter.VrcUnnecessaryObjectList);

            EditorGUI.BeginChangeCheck();
            _settings = (DeleteSpecificObjectsSettings) EditorGUILayout.ObjectField(
                _settings,
                typeof(DeleteSpecificObjectsSettings),
                false
            );
            if (EditorGUI.EndChangeCheck() && _settings)
            {
                _settingsPath = AssetDatabase.GetAssetPath(_settings);
                EditorPrefs.SetString(LanguagePrefKey, _settingsPath);
            }

            EditorGUILayout.Space();
            if (!_settings)
            {
                EditorGUILayout.HelpBox(L10N.Converter.SettingsFileNotFound, MessageType.Warning);
                if (GUILayout.Button(L10N.Converter.CreateNewSettingsFile, GUILayout.Height(30)))
                {
                    CreateNewSettings();
                }
            }
            else
            {
                var validNames = _settings.targetObjectNames.Where(n => !string.IsNullOrEmpty(n)).ToList();
                EditorGUILayout.HelpBox(L10N.Converter.TargetObjectsInfo(validNames.Count), MessageType.Info);
                EditorGUILayout.Space();

                if (GUILayout.Button(L10N.Converter.OpenSettingsFile, GUILayout.Height(25)))
                {
                    Selection.activeObject = _settings;
                    EditorGUIUtility.PingObject(_settings);
                }
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 変換
            GUILayout.Label(L10N.Converter.ConvertHeader, EditorStyles.boldLabel);
            GUI.enabled = _selectedVrcPrefabs.Any(prefab => prefab is not null);
            if (GUILayout.Button(L10N.Converter.ConvertButton, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
            {
                ConvertMultipleToVrm();
            }

            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void RemoveLastOrSelected()
        {
            var index = VrmConverterListUtility.GetIndexToRemove(_selectedVrcPrefabs, _lastFocusedIndex);
            if (index < 0) return;
            
            _selectedVrcPrefabs.RemoveAt(index);
            _lastFocusedIndex = -1;
        }

        private bool HandleMultipleObjectDrop(Rect dropRect)
        {
            var currentEvent = Event.current;
            var eventType = currentEvent.type;

            if (eventType != EventType.DragUpdated && eventType != EventType.DragPerform)
                return false;

            if (!dropRect.Contains(currentEvent.mousePosition))
                return false;

            // ドラッグ中のGameObjectを取得
            var draggedObjects = DragAndDrop.objectReferences
                .OfType<GameObject>()
                .Where(obj => obj)
                .ToList();

            switch (draggedObjects.Count)
            {
                case 0:
                    return false;
                // 複数オブジェクトの場合のみカスタム処理
                case > 1:
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (eventType == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        // ユーティリティを使用して重複なく追加
                        VrmConverterListUtility.AddUniqueGameObjects(_selectedVrcPrefabs, draggedObjects);

                        GUI.changed = true;
                        Repaint();
                    }

                    currentEvent.Use();
                    return true;
                }
                default:
                    // 単一オブジェクトの場合はObjectFieldに任せる
                    return false;
            }
        }

        private void ConvertMultipleToVrm()
        {
            // 実行時にAssetDatabaseを最新にする
            AssetDatabase.Refresh();

            var validPrefabs = _selectedVrcPrefabs.Where(p => p is not null).ToList();
            if (validPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog(L10N.Error, L10N.Converter.NoAvatarsSelected, L10N.OK);
                return;
            }

            // 全体のバリデーション（共通設定のチェック）
            if (_isVrmComponentCopy && !_baseVrmPrefab)
            {
                EditorUtility.DisplayDialog(L10N.Error, L10N.Converter.Errors.TargetVrmBasePrefabNotSelected, L10N.OK);
                return;
            }

            if (string.IsNullOrEmpty(_vrmOutputPath))
            {
                EditorUtility.DisplayDialog(L10N.Error, L10N.Converter.Errors.VrmOutputPathNotSet, L10N.OK);
                return;
            }

            // 変換処理を実行
            var converter = new VrmAvatarConverter(
                _vrmOutputPath,
                _vrmThumbnailPath,
                _vrmVersion,
                _vrmAuthor,
                (int) _thumbnailResolution,
                _isVrmComponentCopy,
                _baseVrmPrefab,
                _settings
            );
            
            var successCount = 0;
            var failedCount = 0;
            var totalCount = validPrefabs.Count;
            var failedAvatarNames = new List<string>();

            try
            {
                Selection.activeObject = null;
                for (var i = 0; i < validPrefabs.Count; i++)
                {
                    var prefab = validPrefabs[i];

                    // キャンセル可能なプログレスバーを表示
                    if (EditorUtility.DisplayCancelableProgressBar(
                            L10N.Converter.Converting,
                            L10N.Converter.ConvertingProgress(prefab.name, i + 1, totalCount),
                            (float) i / totalCount))
                    {
                        // キャンセルされた場合
                        Debug.LogWarning($"変換がキャンセルされました。{successCount}体の変換が完了しました。");
                        break;
                    }

                    try
                    {
                        if (converter.ConvertSingleAvatar(prefab))
                        {
                            successCount++;
                            Debug.Log($"✓ {prefab.name} の変換が完了しました");
                        }
                        else
                        {
                            failedCount++;
                            failedAvatarNames.Add(prefab.name);
                            Debug.LogWarning($"✗ {prefab.name} の変換に失敗しました");
                        }
                    }
                    catch (Exception e)
                    {
                        failedCount++;
                        failedAvatarNames.Add(prefab.name);
                        Debug.LogError($"✗ {prefab.name} の変換中にエラーが発生しました: {e.Message}");
                    }
                    
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                var tempDirs = Directory.GetDirectories("Assets", "ZZZ_GeneratedAssets_*");
                foreach (var dir in tempDirs)
                {
                    AssetDatabase.DeleteAsset(dir);
                }

                // 最後に一度だけRefresh
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // 結果メッセージを構築
            var resultMessage = L10N.Converter.ConversionSummary(successCount, failedCount, totalCount);

            // 失敗したアバターがある場合は詳細を追加
            if (failedAvatarNames.Count > 0)
            {
                resultMessage += "\n\n失敗したアバター:";
                var displayCount = Mathf.Min(failedAvatarNames.Count, 10);
                for (var i = 0; i < displayCount; i++)
                {
                    resultMessage += $"\n• {failedAvatarNames[i]}";
                }

                if (failedAvatarNames.Count > 10)
                {
                    resultMessage += $"\n... 他{failedAvatarNames.Count - 10}体";
                }
            }

            // 完了後に1回だけダイアログを表示
            EditorUtility.DisplayDialog(
                L10N.Converter.ConversionComplete,
                resultMessage,
                L10N.OK
            );
        }

        private static void CreateNewSettings()
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                var folders = directory.Split('/');
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

            _settings = CreateInstance<DeleteSpecificObjectsSettings>();
            AssetDatabase.CreateAsset(_settings, _settingsPath);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(LanguagePrefKey, _settingsPath);

            Debug.Log(LocalizationManager.Get(
                $"設定ファイルを作成しました: {_settingsPath}",
                $"Settings file created: {_settingsPath}"
            ));

            EditorUtility.DisplayDialog(
                L10N.Complete,
                L10N.Converter.SettingsFileCreated(_settingsPath),
                L10N.OK
            );

            Selection.activeObject = _settings;
            EditorGUIUtility.PingObject(_settings);
        }
    }
}