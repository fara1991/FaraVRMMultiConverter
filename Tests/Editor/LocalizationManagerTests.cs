using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fara.FaraVRMMultiConverter.Editor;
using NUnit.Framework;

namespace Fara.FaraVRMMultiConverter.Tests.Editor
{
    [TestFixture]
    public class LocalizationManagerTests
    {
        [Test]
        public void L10N_AllParameters_ReturnJapanese()
        {
            // Act & Assert
            AssertAllL10NProperties(true);
        }

        [Test]
        public void L10N_AllParameters_ReturnEnglish()
        {
            // Act & Assert
            AssertAllL10NProperties(false);
        }

        [Test]
        public void L10N_Converter_PropertiesAndMethods_ShouldReturnStrings()
        {
            // 日本語・英語両方のモードで呼び出しを確認（カバレッジのため）
            LocalizationManager.IsJapanese = true;
            InvokeAllL10NProperties();

            LocalizationManager.IsJapanese = false;
            InvokeAllL10NProperties();
        }
        
        /// <summary>
        /// L10N内のすべての静的文字列プロパティが、指定した言語に対応しているか再帰的にチェックします
        /// </summary>
        private static void AssertAllL10NProperties(bool expectJapanese)
        {
            LocalizationManager.IsJapanese = expectJapanese;

            var l10NType = typeof(L10N);
            CheckPropertiesRecursive(l10NType, expectJapanese);
            
            if (expectJapanese)
            {
                AssertConsistencyRecursive(l10NType);
            }
        }

        private static void CheckPropertiesRecursive(System.Type type, bool expectJapanese)
        {
            // 文字列プロパティのチェック
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string)) continue;

                var value = (string) prop.GetValue(null);

                // "OK" のように言語共通のものはスキップ
                if (prop.Name == "OK") continue;

                // 日本語/英語が含まれているかどうかの簡易判定
                // 実際の実装に合わせて、日本語が含まれているか、または英語のみかを確認
                if (expectJapanese)
                {
                    // 日本語が含まれていることを期待（WindowTitleなどは共通の場合もあるが、多くは日本語）
                    // ここでは、空でないことの確認を最低限行います
                    Assert.IsFalse(string.IsNullOrEmpty(value),
                        $"Property {type.Name}.{prop.Name} is empty in Japanese");
                }
                else
                {
                    // 英語モードの時は、日本語特有の文字（ひらがな、カタカナ、漢字）が含まれていないことを確認
                    // 全角の「」などは英語テキスト内でもUI名称の引用として使われる可能性があるため許容する
                    var hasJapaneseMap = Regex.IsMatch(value, @"[\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF]");
                    
                    // 例外: "Language / 言語" のような項目は英語モードでも日本語が含まれる可能性があるため除外
                    if (prop.Name == "Language") continue;

                    Assert.IsFalse(hasJapaneseMap,
                        $"Property {type.Name}.{prop.Name} contains Japanese characters: {value}");
                }
            }

            // ネストされたクラス（Converter, MetaUpdaterなど）を再帰的にチェック
            var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (var nestedType in nestedTypes)
            {
                CheckPropertiesRecursive(nestedType, expectJapanese);
            }
        }
        
        private static void AssertConsistencyRecursive(System.Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string)) continue;
                if (prop.Name is "OK" or "Language") continue;

                // 日本語と英語それぞれの値を強制的に取得
                LocalizationManager.IsJapanese = true;
                var jpVal = (string)prop.GetValue(null);
                LocalizationManager.IsJapanese = false;
                var enVal = (string)prop.GetValue(null);

                var context = $"{type.Name}.{prop.Name}";

                // 1. 変数置換（フォーマット引数 {0} など）の不一致チェック
                var jpMatches = Regex.Matches(jpVal, @"\{(\d+)\}");
                var enMatches = Regex.Matches(enVal, @"\{(\d+)\}");
                var jpIndices = jpMatches.Select(m => m.Groups[1].Value).Distinct().OrderBy(v => v).ToList();
                var enIndices = enMatches.Select(m => m.Groups[1].Value).Distinct().OrderBy(v => v).ToList();

                Assert.AreEqual(jpIndices, enIndices, $"Placeholders mismatch in {context}\nJP: {jpVal}\nEN: {enVal}");

                // 2. 特殊記号や改行コードの有無をチェック
                // 改行コードの数
                Assert.AreEqual(jpVal.Count(c => c == '\n'), enVal.Count(c => c == '\n'), 
                    $"Line break count mismatch in {context}");
            }

            foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                AssertConsistencyRecursive(nestedType);
            }
        }
        
        private void InvokeAllL10NProperties()
        {
            // Converter 関連
            Assert.IsNotEmpty(L10N.Converter.AvatarElement(0));
            Assert.IsNotEmpty(L10N.Converter.ThumbnailsAlreadyExist("AvatarA, AvatarB"));
            Assert.IsNotEmpty(L10N.Converter.ConvertingProgress("Avatar", 1, 10));
            Assert.IsNotEmpty(L10N.Converter.TargetObjectsInfo(5));
            Assert.IsNotEmpty(L10N.Converter.SettingsFileCreated("Assets/Test.asset"));
            Assert.IsNotEmpty(L10N.Converter.ConversionSummary(1, 1, 2));
            Assert.IsNotEmpty(L10N.Converter.SettingsEditor.RegisteredCount(10));
            Assert.IsNotEmpty(L10N.Converter.Errors.ConversionError("Internal Error"));

            // MetaUpdater 関連
            Assert.IsNotEmpty(L10N.MetaUpdater.TargetPrefab("Assets/Prefab.prefab"));
            Assert.IsNotEmpty(L10N.MetaUpdater.PrefabName("Avatar"));
            Assert.IsNotEmpty(L10N.MetaUpdater.MetaObjectCreated("Assets/Meta.asset"));
            Assert.IsNotEmpty(L10N.MetaUpdater.ThumbnailSet("Thumb", "Assets/T.png", 1024, 1024));
            Assert.IsNotEmpty(L10N.MetaUpdater.TitleUpdated("Old", "New"));
            Assert.IsNotEmpty(L10N.MetaUpdater.VersionUpdated("v1", "v2"));
            Assert.IsNotEmpty(L10N.MetaUpdater.AuthorUpdated("AuthorA", "AuthorB"));
            Assert.IsNotEmpty(L10N.MetaUpdater.UpdateError("Error Message"));

            // 共通
            Assert.IsNotEmpty(L10N.Error);
            Assert.IsNotEmpty(L10N.Complete);
            Assert.IsNotEmpty(L10N.Converter.WindowTitle);
        }
    }
}