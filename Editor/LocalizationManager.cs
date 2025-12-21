using UnityEditor;

namespace Fara.Fara_VRMMultiConverter.Editor
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
        public static string Cancel => LocalizationManager.Get("キャンセル", "Cancel");

        // VRChatToVrmConverter 用
        public static class Converter
        {
            public static string WindowTitle => "VRM Multi-Converter";
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
            public static string PathSettings => LocalizationManager.Get("VRMアバター設定", "VRM Avatar Settings");
            public static string SourceVrcPrefab => LocalizationManager.Get("VRC → VRMに変換するアバター", "VRC → VRM Conversion avatar");

            public static string IsVrmComponentCopy => LocalizationManager.Get("他のアバターからVRM Componentをコピー", "Copy VRM Components from another avatar");
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
            public static string ThumbnailAlreadyExists => LocalizationManager.Get("サムネイルは作成済みなので既存のサムネイルを適用します", "The thumbnail has already been created, so apply the existing thumbnail");
            // VRM変換
            public static string ConvertHeader => LocalizationManager.Get("変換", "Convert");
            public static string ConvertButton => LocalizationManager.Get("VRC → VRMに変換", "Convert VRC → VRM");
            // 複数アバター対応
            public static string AvatarCount => LocalizationManager.Get("変換アバター数", "Avatar Count");
            public static string AvatarElement(int index) => LocalizationManager.Get($"変換アバター{index + 1}体目", $"Avatar {index + 1}");
            public static string ThumbnailsAlreadyExist(string avatarNames) => LocalizationManager.Get(
                $"以下のアバターのサムネイルが既に存在します:\n{avatarNames}",
                $"Thumbnails already exist for the following avatars:\n{avatarNames}"
            );
            public static string AllThumbnailsExist => LocalizationManager.Get(
                "すべてのアバターのサムネイルが既に存在するため、サムネイル作成はスキップされます",
                "All avatar thumbnails already exist, thumbnail creation will be skipped"
            );
            public static string NoAvatarsSelected => LocalizationManager.Get(
                "変換するアバターが選択されていません",
                "No avatars selected for conversion"
            );
            public static string Converting => LocalizationManager.Get("VRM変換中", "Converting to VRM");
            public static string ConvertingProgress(string avatarName, int current, int total) => LocalizationManager.Get(
                $"{avatarName} を変換中... ({current}/{total})",
                $"Converting {avatarName}... ({current}/{total})"
            );
            public static string SettingsFileHeader => LocalizationManager.Get("設定ファイル", "Settings file");
            public static string VrcUnnecessaryObjectList => LocalizationManager.Get("削除オブジェクト一覧", "Delete object list");
            public static string SettingsFileNotFound => LocalizationManager.Get(
                "設定ファイルが見つかりません。新規作成してください。",
                "Settings file not found. Please create a new one."
            );
            public static string CreateNewSettingsFile => LocalizationManager.Get("設定ファイルを新規作成", "Create New Settings File");
            public static string OpenSettingsFile => LocalizationManager.Get("設定ファイルを開く", "Open Settings File");
            
            public static string TargetObjectsInfo(int count) => LocalizationManager.Get(
                $"削除対象: {count}個のオブジェクト名が登録されています\n「VRC → VRMに変換するアバター」を再帰的に検索して削除します",
                $"Targets: {count} object names registered\nRecursively search for and delete 「VRC → VRM Conversion Avatar」"
            );
            
            public static string SettingsFileCreated(string path) => LocalizationManager.Get(
                $"設定ファイルを作成しました。\n\n{path}",
                $"Settings file created.\n\n{path}"
            );

            // エラーメッセージ
            public static class Errors
            {
                public static string FailedToInstantiatePrefab => LocalizationManager.Get(
                    "Prefabのインスタンス化に失敗しました",
                    "Failed to instantiate Prefab"
                );
                public static string FailedToExecuteBake => LocalizationManager.Get(
                    "Manual bake avatarの実行に失敗しました",
                    "Failed to execute Manual bake avatar"
                );
                public static string BakedAvatarNotFound => LocalizationManager.Get(
                    "Bakeされたアバターが見つかりません",
                    "Baked avatar not found"
                );
                public static string FailedToCreateVrmPrefab => LocalizationManager.Get(
                    "VRMプレハブの作成に失敗しました",
                    "Failed to create VRM prefab"
                );
                public static string SourceVrcPrefabNotSelected => LocalizationManager.Get(
                    "変換元VRCプレハブが選択されていません",
                    "Source VRC Prefab is not selected"
                );
                public static string TargetVrmBasePrefabNotSelected => LocalizationManager.Get(
                    "ベースVRMプレハブが選択されていません",
                    "Target VRM Base Prefab is not selected"
                );
                public static string VrmOutputPathNotSet => LocalizationManager.Get(
                    "VRM出力パスが設定されていません",
                    "VRM Output Path is not set"
                );
                public static string NotHumanoid => LocalizationManager.Get(
                    "選択したアバターはHumanoidではありません。",
                    "The selected avatar is not Humanoid."
                );
                public static string ConversionError(string message) => LocalizationManager.Get(
                    $"変換中にエラーが発生しました:\n{message}",
                    $"An error occurred during conversion:\n{message}"
                );
            }
            
            public static string ConversionComplete => LocalizationManager.Get("変換完了", "Conversion Complete");

            public static string ConversionSummary(int success, int failed, int total) => LocalizationManager.Get(
                $"変換完了\n\n成功: {success}\n失敗: {failed}\n合計: {total}",
                $"Conversion Complete\n\nSucceeded: {success}\nFailed: {failed}\nTotal: {total}"
            );
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
            
            public static string TargetPrefab(string path) => LocalizationManager.Get(
                $"対象Prefab: {path}",
                $"Target Prefab: {path}"
            );
            public static string PrefabName(string name) => LocalizationManager.Get(
                $"Prefab名: {name}",
                $"Prefab Name: {name}"
            );
            
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
            public static string MetaObjectCreated(string path) => LocalizationManager.Get(
                $"VRMMetaObjectを作成しました: {path}",
                $"VRMMetaObject created: {path}"
            );
            
            public static string ThumbnailSettings => LocalizationManager.Get(
                "--- サムネイル画像の設定 ---",
                "--- Thumbnail Settings ---"
            );
            public static string ThumbnailSet(string name, string path, int width, int height) => LocalizationManager.Get(
                $"✓ サムネイルを設定しました: {name}\n  パス: {path}\n  サイズ: {width}x{height}",
                $"✓ Thumbnail set: {name}\n  Path: {path}\n  Size: {width}x{height}"
            );
            public static string ThumbnailSetFailed => LocalizationManager.Get(
                "✗ サムネイルの設定に失敗しました",
                "✗ Failed to set thumbnail"
            );
            
            public static string MetaInfoSettings => LocalizationManager.Get(
                "--- Meta情報の設定 ---",
                "--- Meta Information Settings ---"
            );
            public static string TitleUpdated(string oldValue, string newValue) => LocalizationManager.Get(
                $"✓ Title: '{oldValue}' → '{newValue}'",
                $"✓ Title: '{oldValue}' → '{newValue}'"
            );
            public static string TitleSkipped => LocalizationManager.Get(
                "✗ Titleが空のため設定をスキップしました",
                "✗ Title is empty, skipped"
            );
            
            public static string VersionUpdated(string oldValue, string newValue) => LocalizationManager.Get(
                $"✓ Version: '{oldValue}' → '{newValue}'",
                $"✓ Version: '{oldValue}' → '{newValue}'"
            );
            public static string VersionSkipped => LocalizationManager.Get(
                "✗ Versionが空のため設定をスキップしました",
                "✗ Version is empty, skipped"
            );
            
            public static string AuthorUpdated(string oldValue, string newValue) => LocalizationManager.Get(
                $"✓ Author: '{oldValue}' → '{newValue}'",
                $"✓ Author: '{oldValue}' → '{newValue}'"
            );
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
            
            public static string UpdateError(string message) => LocalizationManager.Get(
                $"VRM Meta更新中にエラーが発生しました: {message}",
                $"Error occurred during VRM Meta update: {message}"
            );
        }
        
        public static class DeleteObjectsEditor
        {
            public static string WindowTitle => LocalizationManager.Get("特定オブジェクト削除", "Delete Specific Objects");
            public static string TargetObject => LocalizationManager.Get("対象オブジェクト", "Target Object");
            public static string SelectFromHierarchy => LocalizationManager.Get("Hierarchyから選択", "Select from Hierarchy");
            public static string DeleteButton => LocalizationManager.Get("削除実行", "Execute Delete");
            public static string Instructions => LocalizationManager.Get(
                "使い方:\n" +
                "1. 対象のGameObjectを選択\n" +
                "2. 削除実行ボタンをクリック\n" +
                "3. 設定ファイルに登録されたオブジェクトが削除されます",
                "Instructions:\n" +
                "1. Select target GameObject\n" +
                "2. Click Execute Delete button\n" +
                "3. Objects registered in settings file will be deleted"
            );
            public static string SettingsNotFound => LocalizationManager.Get(
                "設定ファイルが見つかりません",
                "Settings file not found"
            );
            public static string NoTargetObject => LocalizationManager.Get(
                "対象オブジェクトが選択されていません",
                "No target object selected"
            );
            public static string DeleteComplete(int count) => LocalizationManager.Get(
                $"{count}個のオブジェクトを削除しました",
                $"Deleted {count} objects"
            );
            public static string NoObjectsDeleted => LocalizationManager.Get(
                "削除対象のオブジェクトが見つかりませんでした",
                "No objects found to delete"
            );
        }

        // DeleteVrcComponentsEditor 用
        public static class DeleteVrcEditor
        {
            public static string WindowTitle => LocalizationManager.Get("VRCコンポーネント削除", "Delete VRC Components");
            public static string TargetObject => LocalizationManager.Get("対象オブジェクト", "Target Object");
            public static string SelectFromHierarchy => LocalizationManager.Get("Hierarchyから選択", "Select from Hierarchy");
            public static string DeleteButton => LocalizationManager.Get("VRCコンポーネント削除", "Delete VRC Components");
            public static string Instructions => LocalizationManager.Get(
                "使い方:\n" +
                "1. VRChat用GameObjectを選択\n" +
                "2. VRCコンポーネント削除ボタンをクリック\n" +
                "3. VRC関連のコンポーネントが再帰的に削除されます",
                "Instructions:\n" +
                "1. Select VRChat GameObject\n" +
                "2. Click Delete VRC Components button\n" +
                "3. VRC-related components will be recursively deleted"
            );
            public static string NoTargetObject => LocalizationManager.Get(
                "対象オブジェクトが選択されていません",
                "No target object selected"
            );
            public static string DeleteComplete => LocalizationManager.Get(
                "VRCコンポーネントの削除が完了しました",
                "VRC components deletion completed"
            );
            public static string ConfirmDelete(string name) => LocalizationManager.Get(
                $"'{name}' のVRCコンポーネントを削除しますか?\n\nこの操作は元に戻せません。",
                $"Delete VRC components from '{name}'?\n\nThis operation cannot be undone."
            );
            public static string DeleteCancelled => LocalizationManager.Get(
                "削除がキャンセルされました",
                "Deletion cancelled"
            );
        }

        // HierarchyRegexRenames 用
        public static class RegexRename
        {
            public static string WindowTitle => LocalizationManager.Get("階層内一括リネーム", "Hierarchy Regex Rename");
            public static string TargetObject => LocalizationManager.Get("対象オブジェクト", "Target Object");
            public static string SearchPattern => LocalizationManager.Get("検索パターン (Regex)", "Search Pattern (Regex)");
            public static string ReplacePattern => LocalizationManager.Get("置換パターン", "Replace Pattern");
            public static string PreviewButton => LocalizationManager.Get("プレビュー", "Preview");
            public static string RenameButton => LocalizationManager.Get("リネーム実行", "Execute Rename");
            public static string PreviewResults => LocalizationManager.Get("プレビュー結果", "Preview Results");
            public static string Instructions => LocalizationManager.Get(
                "使い方:\n" +
                "1. 対象のGameObjectを選択\n" +
                "2. 検索パターンと置換パターンを入力\n" +
                "3. プレビューで確認後、リネーム実行",
                "Instructions:\n" +
                "1. Select target GameObject\n" +
                "2. Enter search and replace patterns\n" +
                "3. Preview, then execute rename"
            );
            public static string NoTargetObject => LocalizationManager.Get(
                "対象オブジェクトが選択されていません",
                "No target object selected"
            );
            public static string EmptyPattern => LocalizationManager.Get(
                "検索パターンを入力してください",
                "Please enter a search pattern"
            );
            public static string InvalidRegex(string error) => LocalizationManager.Get(
                $"正規表現エラー: {error}",
                $"Regex error: {error}"
            );
            public static string RenameComplete(int count) => LocalizationManager.Get(
                $"{count}個のオブジェクトをリネームしました",
                $"Renamed {count} objects"
            );
            public static string NoMatches => LocalizationManager.Get(
                "一致するオブジェクトが見つかりませんでした",
                "No matching objects found"
            );
            public static string ConfirmRename(int count) => LocalizationManager.Get(
                $"{count}個のオブジェクトをリネームします。\n\nよろしいですか?",
                $"Rename {count} objects.\n\nAre you sure?"
            );
        }
    }
}