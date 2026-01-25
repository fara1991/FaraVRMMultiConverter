# FaraMultiVrmConverter

VRChatアバターをVRM形式へ一括変換するためのUnityエディタ拡張ツールです。複数のアバターを一度に処理でき、既存のVRMからのコンポーネントコピーやサムネイルの自動生成機能を備えています。

[English version (README_EN.md)](README_EN.md)

## 主な機能

- **一括変換**: 複数のVRChatアバターPrefabを一度にVRMへ変換できます。
- **コンポーネントコピー**: 既存のVRMからBlendShapeProxyやMeta情報などのVRMコンポーネントをコピーできます。
- **サムネイル自動生成**: 指定した解像度でアバターのサムネイルを自動的に生成し、VRM Metaに設定します。
- **特定オブジェクトの自動削除**: VRM化の際に不要なオブジェクト（VRChat専用コンポーネントを持つオブジェクトなど）を自動で削除する設定が可能です。
- **多言語対応**: 日本語と英語の表示を切り替え可能です。

## 必要要件

- Unity 2022.3
- VRM 0.x (UniVRM)
- UniVRM-Extensions
- NDMF (Non-Destructive Modular Framework)
- VRChat SDK - Avatars

## 使い方

1. `FaraScripts/VRMMultiConverter` からウィンドウを開きます。
2. 変換したいVRChatアバターのPrefabを「VRC → VRMへ変換するアバター」リストにドラッグ＆ドロップします。
3. 必要に応じて「他のアバターからVRM Componentをコピー」にチェックを入れ、ベースとなるVRM Prefabを指定します。
4. VRMの出力先、Meta情報（Version, Author）、サムネイルの保存先を設定します。
5. 削除したいオブジェクトがある場合は、設定ファイルを開いてオブジェクト名を追加します。
6. 「VRC → VRMに変換」ボタンをクリックして実行します。

## ライセンス

[MIT License](LICENSE)