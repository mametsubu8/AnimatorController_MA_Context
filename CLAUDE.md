# AnimatorController MA Context

## プロジェクト概要

VRChatアバターの構成（AnimatorController、Modular Avatar、VRCコンポーネント）をAI向けテキスト形式に一括シリアライズするUnity Editorパッケージ。AnimatorControllerのシリアライズは `com.mame8.animator-controller-context` パッケージに委譲し、コードの重複を避けている。

## 技術スタック

- Unity 2022.3+
- C# (Editor-only assembly)
- 依存パッケージ: `com.mame8.animator-controller-context` v0.1.0
- オプション依存: VRC Avatars SDK (`com.vrchat.avatars`)、Modular Avatar (`nadena.dev.modular-avatar`)
- VRC/MAコンポーネントへのアクセスは `SerializedProperty` ベース（型名文字列判定 + プロパティ名アクセス）
- VRC SDK / MA の assembly reference は asmdef で宣言（versionDefines: `AMAC_VRC_SDK`, `AMAC_MODULAR_AVATAR`）

## ドキュメント
詳細なドキュメントは `Docs/` フォルダを参照:
- `Docs/Overview.md` - プロジェクト概要・アーキテクチャ・依存関係
- `Docs/OutputFormat.md` - 出力テキストフォーマット完全仕様
- `Docs/API-Reference.md` - 全クラス・メソッドのAPIリファレンス
- `Docs/Components.md` - 対応コンポーネント詳細（VRC・MA全種）
- `Docs/Development-Guide.md` - 開発ガイド・条件コンパイル・コーディング規約
- `Docs/EditorWindow-Guide.md` - エディタウィンドウ使用ガイド・AI活用のヒント

## ディレクトリ構成

```
Assets/AnimatorControllerMAContext/
├── package.json                                    # UPMパッケージ定義
├── Docs/
│   ├── Overview.md                                 # プロジェクト概要
│   ├── OutputFormat.md                             # 出力テキストフォーマット仕様
│   ├── API-Reference.md                            # APIリファレンス
│   ├── Components.md                               # 対応コンポーネント詳細
│   ├── Development-Guide.md                        # 開発ガイド
│   └── EditorWindow-Guide.md                       # エディタウィンドウ使用ガイド
└── Editor/
    ├── AnimatorControllerMAContext.Editor.asmdef    # Assembly定義
    ├── Core/
    │   ├── AvatarContextSerializer.cs              # メインオーケストレーター
    │   ├── ContextSerializeOptions.cs              # シリアライズオプション
    │   ├── SerializeContext.cs                     # 内部状態（Controller収集、Menu訪問追跡）
    │   ├── ComponentSerializer.cs                  # 汎用コンポーネントシリアライザ（SerializedProperty）
    │   ├── HierarchySerializer.cs                  # GameObjectツリーシリアライザ
    │   ├── VRCComponentSerializer.cs               # VRC固有（AvatarDescriptor, PhysBone, Contact）
    │   ├── MAComponentSerializer.cs                # Modular Avatar固有
    │   └── RendererSerializer.cs                   # SkinnedMeshRenderer（BlendShape一覧）
    └── UI/
        └── AvatarContextWindow.cs                  # EditorWindow（Tools > Avatar Context）
```

## 主要クラス

### AvatarContextSerializer
メインエントリーポイント。アバターのルートGameObjectを受け取り、各セクションのシリアライザを順番に呼び出す。シリアライズ中に発見されたAnimatorControllerを `SerializeContext` に収集し、最後にまとめて `AnimatorControllerSerializer.Serialize()` で出力する。

### SerializeContext
シリアライズ中の内部状態を保持する。収集されたAnimatorController、訪問済みメニュー（循環参照防止）、オプション参照。ユーティリティメソッド（相対パス計算、コンポーネント表示名）も提供。

### ComponentSerializer
`SerializedProperty` を使った汎用コンポーネントシリアライザ。任意のComponentのプロパティをkey-value形式で出力する。MA/VRCの専用シリアライザがカバーしないコンポーネントのフォールバックとして使用。

### VRCComponentSerializer
VRC固有コンポーネントの専用シリアライズ。`FindComponentByTypeName` で型名文字列判定し、`SerializedProperty` でプロパティにアクセス。VRC SDKへの直接的な型依存を回避している。

主要メソッド:
- `SerializeAvatarDescriptor` - View Position、Lip Sync、Eye Look、Playable Layers
- `SerializeExpressionParameters` - パラメータ一覧と同期コスト計算
- `SerializeExpressionMenu` - メニューツリー再帰展開（循環参照対策あり）
- `SerializePhysComponents` - PhysBone、PhysBoneCollider、Contact Sender/Receiver

### MAComponentSerializer
Modular Avatarコンポーネントの専用シリアライズ。コンポーネントの `GetType().Name` でディスパッチし、`SerializedProperty` でプロパティにアクセス。

対応コンポーネント:
- MergeAnimator, Parameters, MenuItem, MenuInstaller
- BlendshapeSync, BoneProxy, ObjectToggle
- MeshSettings, ScaleAdjuster, MergeBlendTree
- WorldFixedObject, VisibleHeadAccessory, MenuGroup
- 上記以外は `ComponentSerializer.Serialize()` で汎用出力

### HierarchySerializer
GameObjectツリーをインデント形式でシリアライズ。各GameObjectに付いているコンポーネントを `[ComponentName]` 注釈で表示。SkinnedMeshRendererはブレンドシェイプ数も表示。非アクティブオブジェクトには `[inactive]` マーク。

### RendererSerializer
`SkinnedMeshRenderer` のメッシュ名、ブレンドシェイプ一覧（非ゼロウェイト表示）、マテリアル一覧をシリアライズ。

## テキストフォーマット仕様

### セクション順序
1. `=== Avatar Context: Name ===` - ヘッダー
2. `--- Avatar Descriptor ---` - AvatarDescriptor設定
3. `--- Playable Layers ---` - レイヤー割当
4. `--- Expression Parameters (Cost: N/256) ---` - パラメータ一覧
5. `--- Expression Menu ---` - メニューツリー
6. `--- Hierarchy ---` - GameObjectツリー
7. `--- Modular Avatar Components ---` - MAコンポーネント詳細
8. `--- PhysBones ---` - PhysBone / Collider
9. `--- Contacts ---` - Contact Sender / Receiver
10. `--- Renderers ---` - SkinnedMeshRenderer
11. `--- AnimatorControllers ---` - AnimatorController完全出力

### コンポーネントパス表記
MAコンポーネント、PhysBone等のパスはアバタールートからの相対パス:
```
[MAMergeAnimator] @ Armature/Hips/HatToggle
```

### AnimatorController出力
`AnimatorControllerContext.Editor.AnimatorControllerSerializer.Serialize()` の出力をそのまま使用。フォーマット仕様はAnimatorController Contextパッケージのドキュメントを参照。

## 開発時の注意

### VRC/MAへの型依存回避
- コンポーネントの型判定は `component.GetType().Name` の文字列比較で行う
- プロパティアクセスは `SerializedObject` / `SerializedProperty` 経由
- VRC/MAのC# APIを直接参照しないため、SDKバージョン変更に強い
- asmdef の assembly reference は Unity のソフトリンク（未インストール時はスキップ）

### AnimatorController Context との関係
- `package.json` で `com.mame8.animator-controller-context` を依存宣言
- `asmdef` で `AnimatorControllerContext.Editor` assembly を参照
- `AnimatorControllerSerializer.Serialize()` をそのまま呼び出し
- 元パッケージの変更は自動的に反映される（コードのコピーなし）

### その他の開発規約
- `SerializedObject` は必ず `try-finally` ブロック内で使用し、例外発生時でも `Dispose()` が確実に呼ばれるようにすること
- `ContextSerializeOptions` はフィールドではなく auto-property を使用していること
- `ComponentSerializer` の `SkippedPropertyNames` は `HashSet<string>` を使用して O(1) ルックアップを実現していること

### SerializedProperty のプロパティ名
MAコンポーネントのプロパティ名はMAバージョンで変わる可能性がある。`FindProperty()` が `null` を返した場合はそのフィールドをスキップする設計。一部のコンポーネント（ObjectToggle等）ではフォールバックプロパティ名も試行。

### コスト計算
VRC Expression Parameters の同期コスト: Int=8bit, Float=8bit, Bool=1bit, 上限256bit。

## ビルド・テスト

### 手動テスト手順
1. VRChat SDKとModular Avatarがインストールされたプロジェクトで動作確認
2. `Tools > Avatar Context` でウィンドウを開く
3. 各種構成のアバター（MA使用/未使用、PhysBoneあり/なし）でSerializeを実行
4. 以下を確認:
   - 全セクションが正しく出力されること
   - オプションOFFのセクションが出力されないこと
   - AnimatorControllerが正しくシリアライズされること
   - クリップボードコピー・ファイル保存が動作すること
   - VRC SDK / MA未インストール環境でコンパイルエラーにならないこと
