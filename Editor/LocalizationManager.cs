using UnityEditor;

namespace Fara.FaraVRMMultiConverter.Editor
{
    /// <summary>
    /// 多言語対応を管理するクラス
    /// </summary>
    public static class LocalizationManager
    {
        private const string LanguagePrefKey = "Fara_Language";

        private static bool? _isJapanese;

        /// <summary>
        /// 現在の言語が日本語かどうか
        /// </summary>
        public static bool IsJapanese
        {
            get
            {
                _isJapanese ??= EditorPrefs.GetBool(LanguagePrefKey, true);
                return _isJapanese.Value;
            }
            set
            {
                _isJapanese = value;
                EditorPrefs.SetBool(LanguagePrefKey, value);
            }
        }

        /// <summary>
        /// 日本語と英語のテキストから適切な方を返す
        /// </summary>
        public static string Get(string japanese, string english)
        {
            return IsJapanese ? japanese : english;
        }
    }

    /// <summary>
    /// UI関連の多言語テキスト
    /// </summary>
    public static class L10N
    {
        // 共通
        public static string Error => LocalizationManager.Get("エラー", "Error");
        public static string Complete => LocalizationManager.Get("完了", "Complete");
        public static string OK => "OK";

        // VRChatToVrmConverter 用
        public static class Converter
        {
            public static string WindowTitle => "VRMMultiConverter";
            public static string Language => LocalizationManager.Get("言語 / Language", "Language / 言語");
            public static string HowToUseHeader => LocalizationManager.Get("使い方手順", "Instructions for use");

            public static string Instructions => LocalizationManager.Get(
                "1. VRM変換したいVRCアバターのPrefabをProjectから「VRC → VRM変換アバター」にドラッグ＆ドロップ\n" +
                "2. BlendShapeやMetaObjectをコピーしたいVRMアバターをProjectからベースVRMプレハブにドラッグ＆ドロップ\n" +
                "3. VRM Meta情報(Title、Version、Author)を入力\n" +
                "4. VRM Metaに設定するサムネイル出力先のファイルパスを入力(サムネイル未作成なら指定解像度でサムネイルを作成)\n" +
                "5. VRC用オブジェクトなどVRMで不要なオブジェクトがあれば、設定ファイルを開いて削除するオブジェクト名を追加\n" +
                "6. Convertボタンをクリック\n" +
                "7. VRM変換を実行",
                "1. Drag and drop the VRC avatar Prefab you want to convert from the Project to 「VRC → VRM conversion avatar」\n" +
                "2. Drag and drop the VRM avatar you want to copy the BlendShape or MetaObject from the Project to the base VRM prefab\n" +
                "3. Enter VRM Meta Information (Title, Version, Author)\n" +
                "4. Enter the file path for the thumbnail output destination in VRM Meta (If no thumbnail exists, create one at the specified resolution)\n" +
                "5. If there are any objects in VRM that are unnecessary for VRC, open the settings file and add the names of the objects to be deleted\n" +
                "6. Click the Convert button\n" +
                "7. Perform VRM conversion"
            );

            // VRMアバター設定
            public static string PathSettings => LocalizationManager.Get(
                "VRMアバター設定",
                "VRM Avatar Settings"
            );

            public static string SourceVrcPrefab => LocalizationManager.Get(
                "VRC → VRMに変換するアバター",
                "VRC → VRM Conversion avatar"
            );

            public static string IsVrmComponentCopy => LocalizationManager.Get(
                "他のアバターからVRM Componentをコピー",
                "Copy VRM Components from another avatar"
            );

            public static string TargetVrmBasePrefab => LocalizationManager.Get("コピー元VRMアバター", "Source VRM Avatar");

            public static string VrmOutputPath => LocalizationManager.Get("VRM出力先フォルダ", "VRM Output Path");

            // VRM Meta情報
            public static string VrmMetaInformation => LocalizationManager.Get("VRM Meta情報", "VRM Meta Information");
            public static string Version => LocalizationManager.Get("Version", "Version");

            public static string Author => LocalizationManager.Get("Author", "Author");

            // サムネイル設定
            public static string ThumbnailSettings => LocalizationManager.Get("サムネイル設定", "Thumbnail Settings");
            public static string ThumbnailPath => LocalizationManager.Get("サムネイル保存パス", "VRM Thumbnail Path");
            public static string ThumbnailResolution => LocalizationManager.Get("サムネイル解像度", "Thumbnail Resolution");

            // VRM変換
            public static string ConvertHeader => LocalizationManager.Get("変換", "Convert");

            public static string ConvertButton => LocalizationManager.Get("VRC → VRMに変換", "Convert VRC → VRM");

            // 複数アバター対応
            public static string AvatarCount => LocalizationManager.Get("変換アバター数", "Avatar Count");

            public static string AvatarElement(int index)
            {
                return LocalizationManager.Get(
                    $"変換アバター{index + 1}体目",
                    $"Avatar {index + 1}"
                );
            }

            public static string ThumbnailsAlreadyExist(string avatarNames)
            {
                return LocalizationManager.Get(
                    $"以下のアバターのサムネイルが既に存在します:\n{avatarNames}",
                    $"Thumbnails already exist for the following avatars:\n{avatarNames}"
                );
            }

            public static string AllThumbnailsExist => LocalizationManager.Get(
                "すべてのアバターのサムネイルが既に存在するため、サムネイル作成はスキップされます",
                "All avatar thumbnails already exist, thumbnail creation will be skipped"
            );

            public static string NoAvatarsSelected => LocalizationManager.Get(
                "変換するアバターが選択されていません",
                "No avatars selected for conversion"
            );

            public static string Converting => LocalizationManager.Get("VRM変換中", "Converting to VRM");

            public static string ConvertingProgress(string avatarName, int current, int total)
            {
                return LocalizationManager.Get(
                    $"{avatarName} を変換中... ({current}/{total})",
                    $"Converting {avatarName}... ({current}/{total})"
                );
            }

            public static string SettingsFileHeader => LocalizationManager.Get("設定ファイル", "Settings file");

            public static string VrcUnnecessaryObjectList =>
                LocalizationManager.Get("削除オブジェクト一覧", "Delete object list");

            public static string SettingsFileNotFound => LocalizationManager.Get(
                "設定ファイルが見つかりません。新規作成してください。",
                "Settings file not found. Please create a new one."
            );

            public static string CreateNewSettingsFile =>
                LocalizationManager.Get("設定ファイルを新規作成", "Create New Settings File");

            public static string OpenSettingsFile => LocalizationManager.Get("設定ファイルを開く", "Open Settings File");

            public static string TargetObjectsInfo(int count)
            {
                return LocalizationManager.Get(
                    $"削除対象: {count}個のオブジェクト名が登録されています\n「VRC → VRMに変換するアバター」を再帰的に検索して削除します",
                    $"Targets: {count} object names registered\nRecursively search for and delete 「VRC → VRM Conversion Avatar」"
                );
            }

            public static string SettingsFileCreated(string path)
            {
                return LocalizationManager.Get(
                    $"設定ファイルを作成しました。\n\n{path}",
                    $"Settings file created.\n\n{path}"
                );
            }

            public static class SettingsEditor
            {
                public static string HelpBox => LocalizationManager.Get(
                    "削除対象のオブジェクト名を管理します",
                    "Manage target object names to be deleted"
                );

                public static string UseRegex => LocalizationManager.Get("正規表現モードを使用", "Use Regex Mode");

                public static string TargetObjectNames =>
                    LocalizationManager.Get("削除対象のオブジェクト名", "Target Object Names");

                public static string RegisteredCount(int count)
                {
                    return LocalizationManager.Get($"登録数: {count}個", $"Registered: {count}");
                }
            }

            // エラーメッセージ
            public static class Errors
            {
                public static string TargetVrmBasePrefabNotSelected => LocalizationManager.Get(
                    "ベースVRMプレハブが選択されていません",
                    "Target VRM Base Prefab is not selected"
                );

                public static string VrmOutputPathNotSet => LocalizationManager.Get(
                    "VRM出力パスが設定されていません",
                    "VRM Output Path is not set"
                );

                public static string ConversionError(string message)
                {
                    return LocalizationManager.Get(
                        $"変換中にエラーが発生しました:\n{message}",
                        $"An error occurred during conversion:\n{message}"
                    );
                }
            }

            public static string ConversionComplete => LocalizationManager.Get("変換完了", "Conversion Complete");

            public static string ConversionSummary(int success, int failed, int total)
            {
                return LocalizationManager.Get(
                    $"変換完了\n\n成功: {success}\n失敗: {failed}\n合計: {total}",
                    $"Conversion Complete\n\nSucceeded: {success}\nFailed: {failed}\nTotal: {total}"
                );
            }
        }

        // VrmMetaUpdater 用
        public static class MetaUpdater
        {
            public static string UpdateStarted => LocalizationManager.Get(
                "=== VRM Meta情報の更新開始 ===",
                "=== VRM Meta Update Started ==="
            );

            public static string UpdateCompleted => LocalizationManager.Get(
                "=== VRM Meta情報の更新完了 ===",
                "=== VRM Meta Update Completed ==="
            );

            public static string TargetPrefab(string path)
            {
                return LocalizationManager.Get(
                    $"対象Prefab: {path}",
                    $"Target Prefab: {path}"
                );
            }

            public static string PrefabName(string name)
            {
                return LocalizationManager.Get(
                    $"Prefab名: {name}",
                    $"Prefab Name: {name}"
                );
            }

            public static string VrmMetaComponentNotFound => LocalizationManager.Get(
                "VRMMetaコンポーネントが存在しないため、追加します",
                "VRMMeta component not found, adding it"
            );

            public static string UsingExistingVrmMetaComponent => LocalizationManager.Get(
                "既存のVRMMetaコンポーネントを使用します",
                "Using existing VRMMeta component"
            );

            public static string UsingExistingMetaObject => LocalizationManager.Get(
                "既存のVRMMetaObjectを使用します",
                "Using existing VRMMetaObject"
            );

            public static string CreatingMetaObject => LocalizationManager.Get(
                "VRMMetaObjectが存在しないため、新規作成します",
                "VRMMetaObject not found, creating new one"
            );

            public static string MetaObjectCreated(string path)
            {
                return LocalizationManager.Get(
                    $"VRMMetaObjectを作成しました: {path}",
                    $"VRMMetaObject created: {path}"
                );
            }

            public static string ThumbnailSettings => LocalizationManager.Get(
                "--- サムネイル画像の設定 ---",
                "--- Thumbnail Settings ---"
            );

            public static string ThumbnailSet(string name, string path, int width, int height)
            {
                return LocalizationManager.Get(
                    $"✓ サムネイルを設定しました: {name}\n  パス: {path}\n  サイズ: {width}x{height}",
                    $"✓ Thumbnail set: {name}\n  Path: {path}\n  Size: {width}x{height}"
                );
            }

            public static string MetaInfoSettings => LocalizationManager.Get(
                "--- Meta情報の設定 ---",
                "--- Meta Information Settings ---"
            );

            public static string TitleUpdated(string oldValue, string newValue)
            {
                return LocalizationManager.Get(
                    $"✓ Title: '{oldValue}' → '{newValue}'",
                    $"✓ Title: '{oldValue}' → '{newValue}'"
                );
            }

            public static string VersionUpdated(string oldValue, string newValue)
            {
                return LocalizationManager.Get(
                    $"✓ Version: '{oldValue}' → '{newValue}'",
                    $"✓ Version: '{oldValue}' → '{newValue}'"
                );
            }

            public static string VersionSkipped => LocalizationManager.Get(
                "✗ Versionが空のため設定をスキップしました",
                "✗ Version is empty, skipped"
            );

            public static string AuthorUpdated(string oldValue, string newValue)
            {
                return LocalizationManager.Get(
                    $"✓ Author: '{oldValue}' → '{newValue}'",
                    $"✓ Author: '{oldValue}' → '{newValue}'"
                );
            }

            public static string AuthorSkipped => LocalizationManager.Get(
                "✗ Authorが空のため設定をスキップしました",
                "✗ Author is empty, skipped"
            );

            public static string MetaInfoSummary => LocalizationManager.Get(
                "--- Meta情報のサマリー ---",
                "--- Meta Information Summary ---"
            );

            public static string None => LocalizationManager.Get("なし", "None");

            public static string SavingPrefab => LocalizationManager.Get(
                "Prefabに変更を保存中...",
                "Saving changes to Prefab..."
            );

            public static string PrefabSaved => LocalizationManager.Get(
                "✓ Prefabの保存が完了しました",
                "✓ Prefab saved successfully"
            );

            public static string UpdateError(string message)
            {
                return LocalizationManager.Get(
                    $"VRM Meta更新中にエラーが発生しました: {message}",
                    $"Error occurred during VRM Meta update: {message}"
                );
            }
        }
    }
}