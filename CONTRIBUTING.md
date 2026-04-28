# コントリビューションガイド

AnimatorController MA Context へのコントリビューションに興味を持っていただきありがとうございます。

## 開発環境のセットアップ

### 必須要件

- **Unity 2022.3** 以上
- **AnimatorController Context** パッケージ (`com.mame8.animator-controller-context`) がインストール済みであること

### オプション依存

以下のパッケージは任意ですが、対応セクションの開発・テストには必要です。

- **VRChat Avatars SDK** (`com.vrchat.avatars`) - Avatar Descriptor、Expression Parameters/Menu、PhysBone、Contact 等の機能に必要
- **Modular Avatar** (`nadena.dev.modular-avatar`) - MA コンポーネントのシリアライズ機能に必要

### プロジェクトの準備

1. このリポジトリをクローンします
2. Unity 2022.3 以上のプロジェクトの `Assets/` 配下にパッケージフォルダを配置します
3. AnimatorController Context パッケージをインストールします
4. 必要に応じて VRChat Avatars SDK と Modular Avatar をインストールします

## 条件コンパイル

本パッケージでは、VRC SDK や Modular Avatar がインストールされていない環境でもコンパイルエラーが発生しないよう、条件コンパイルシンボルを使用しています。

- `AMAC_VRC_SDK` - VRChat Avatars SDK がインストールされている場合に定義される
- `AMAC_MODULAR_AVATAR` - Modular Avatar がインストールされている場合に定義される

これらのシンボルは `asmdef` の `versionDefines` で自動的に設定されます。VRC/MA 固有のコードを追加・変更する場合は、必ず適切な `#if` ディレクティブで囲んでください。

## コーディング規約

### SerializedProperty ベースのアクセス

本パッケージでは、VRC SDK や Modular Avatar の C# 型に直接依存せず、`SerializedProperty` を使ってコンポーネントのプロパティにアクセスしています。

- コンポーネントの型判定は `component.GetType().Name` の文字列比較で行います
- プロパティへのアクセスは `SerializedObject` / `SerializedProperty` 経由で行います
- `FindProperty()` が `null` を返す場合はそのフィールドをスキップしてください（SDK バージョン差異への対応）

### リソース管理

- `SerializedObject` は必ず `try-finally` ブロック内で使用し、`Dispose()` が確実に呼ばれるようにしてください

### ファイル構成

- `Editor/Core/` - シリアライズロジック
- `Editor/UI/` - EditorWindow 関連
- 新しいコンポーネントシリアライザを追加する場合は、既存のシリアライザ（`VRCComponentSerializer.cs`、`MAComponentSerializer.cs`）のパターンに従ってください

## テスト方法

本パッケージは Unity Editor 専用ツールのため、手動テストが主なテスト手段です。

### テスト手順

1. VRChat SDK と Modular Avatar がインストールされた Unity プロジェクトでテストします
2. `Tools > Avatar Context` からエディタウィンドウを開きます
3. 以下のパターンでシリアライズを実行し、出力を確認します:
   - MA コンポーネントを含むアバター
   - MA コンポーネントを含まないアバター
   - PhysBone / Contact を含むアバター
   - 各オプションの ON/OFF 切り替え
4. 確認項目:
   - 全セクションが正しく出力されること
   - オプション OFF のセクションが出力されないこと
   - クリップボードコピーとファイル保存が動作すること
5. VRC SDK / MA 未インストール環境でコンパイルエラーが発生しないことも確認してください

## Issue の報告

バグ報告や機能リクエストは GitHub Issues からお願いします。

### バグ報告時に含めてほしい情報

- Unity バージョン
- 本パッケージのバージョン
- VRChat Avatars SDK のバージョン（使用している場合）
- Modular Avatar のバージョン（使用している場合）
- 再現手順
- 期待される動作と実際の動作
- Console のエラーログ（あれば）

## Pull Request

1. Issue で変更内容について事前に議論することを推奨します
2. フォークしてフィーチャーブランチを作成してください
3. 上記のコーディング規約に従ってください
4. 手動テストで動作確認を行ってください
5. 変更内容を明確に説明する PR を作成してください

### PR のチェックリスト

- [ ] 条件コンパイル（`#if AMAC_VRC_SDK` 等）が適切に使用されていること
- [ ] `SerializedProperty` ベースのアクセスパターンに従っていること
- [ ] VRC SDK / MA 未インストール環境でコンパイルエラーが発生しないこと
- [ ] 手動テストで動作確認済みであること
