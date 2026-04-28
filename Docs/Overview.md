# AnimatorController MA Context - プロジェクト概要

## 目的と用途

AnimatorController MA Context は、VRChatアバターの全構成情報をAI（Claude / GPT等）が理解しやすいテキスト形式に一括変換するUnity Editorツールです。

VRChatアバター開発では、AnimatorController、Modular Avatarコンポーネント、VRC固有コンポーネント（PhysBone、Contact、Expression Parameters / Menu）、SkinnedMeshRendererのブレンドシェイプなど、多数の設定が複雑に絡み合っています。これらの情報はUnity Editor上のInspectorに分散しており、AIに一括で読み込ませることが困難です。

本パッケージは、アバターのルートGameObjectを指定するだけで、これらすべての構成情報を構造化されたテキストとして出力します。出力テキストをAIに読み込ませることで、以下のような活用が可能になります。

- アバター構成の全体像をAIに把握させる
- AnimatorControllerの設定内容をAIに説明・分析させる
- Modular Avatarの設定ミスや最適化ポイントをAIに指摘させる
- PhysBone / Contactの設定値をAIにレビューさせる
- 複数アバター間の構成差異をAIに比較分析させる

## アーキテクチャ概要

### 処理フロー

```
AvatarContextSerializer.Serialize(avatarRoot, options)
    |
    +-- VRCComponentSerializer.SerializeAvatarDescriptor()   ... Avatar Descriptor
    +-- VRCComponentSerializer.SerializeExpressionParameters() ... Expression Parameters
    +-- VRCComponentSerializer.SerializeExpressionMenu()     ... Expression Menu
    +-- HierarchySerializer.Serialize()                      ... Hierarchy
    +-- MAComponentSerializer.Serialize()                    ... Modular Avatar
    +-- VRCComponentSerializer.SerializePhysComponents()     ... PhysBone / Contact
    +-- RendererSerializer.Serialize()                       ... SkinnedMeshRenderer
    +-- AnimatorControllerSerializer.Serialize()             ... AnimatorControllers
        (AnimatorController Context パッケージに委譲)
```

`AvatarContextSerializer` がメインオーケストレーターとして各専門Serializerを順番に呼び出します。シリアライズ中に発見されたAnimatorControllerは `SerializeContext` に収集され、最後にまとめて `AnimatorControllerSerializer.Serialize()`（依存パッケージ提供）で出力されます。

### SerializedPropertyベースの型非依存設計

VRC SDK や Modular Avatar のコンポーネントに対して、C# の型を直接参照せず `SerializedProperty` 経由でプロパティにアクセスします。

```
コンポーネント型判定: component.GetType().Name == "VRCAvatarDescriptor"
プロパティアクセス: new SerializedObject(component).FindProperty("ViewPosition")
```

この設計により、以下の利点があります。

- VRC SDK / MA の C# API に直接依存しないため、SDKバージョン変更に強い
- SDKがインストールされていない環境でもコンパイルエラーにならない
- asmdefの assembly reference はUnityのソフトリンク（未インストール時は無視）

### 条件コンパイル

| フラグ | 定義条件 | 用途 |
|:--|:--|:--|
| `AMAC_VRC_SDK` | `com.vrchat.avatars` がインストール済み | VRCコンポーネントのシリアライズ有効化 |
| `AMAC_MODULAR_AVATAR` | `nadena.dev.modular-avatar` がインストール済み | MAコンポーネントのシリアライズ有効化 |

これらのフラグは `AnimatorControllerMAContext.Editor.asmdef` の `versionDefines` で自動定義されます。手動での設定は不要です。

## パッケージ情報

| 項目 | 値 |
|:--|:--|
| パッケージ名 | `com.mame8.animator-controller-ma-context` |
| バージョン | 1.0.0 |
| ライセンス | MIT |
| 名前空間 | `AnimatorControllerMAContext.Editor` |
| Unity要件 | 2022.3 以上 |
| プラットフォーム | Editor のみ |

## ディレクトリ構成

```
Assets/AnimatorControllerMAContext/
├── package.json                                    # UPMパッケージ定義
├── README.md                                       # パッケージREADME
├── CLAUDE.md                                       # Claude Code用プロジェクト情報
├── Docs/                                           # ドキュメント
│   ├── Overview.md                                 # プロジェクト概要（本ファイル）
│   ├── OutputFormat.md                             # 出力テキストフォーマット仕様
│   ├── API-Reference.md                            # APIリファレンス
│   ├── Components.md                               # 対応コンポーネント詳細
│   ├── Development-Guide.md                        # 開発ガイド
│   └── EditorWindow-Guide.md                       # エディタウィンドウ使用ガイド
└── Editor/
    ├── AnimatorControllerMAContext.Editor.asmdef    # Assembly定義
    ├── Core/
    │   ├── AvatarContextSerializer.cs              # メインオーケストレーター
    │   ├── ContextSerializeOptions.cs              # シリアライズオプション（9つのブール値）
    │   ├── SerializeContext.cs                     # 内部状態（Controller収集、Menu訪問追跡）
    │   ├── ComponentSerializer.cs                  # 汎用コンポーネントシリアライザ（SerializedProperty反射）
    │   ├── HierarchySerializer.cs                  # GameObjectツリーシリアライザ
    │   ├── VRCComponentSerializer.cs               # VRC固有シリアライザ
    │   ├── MAComponentSerializer.cs                # Modular Avatar固有シリアライザ
    │   └── RendererSerializer.cs                   # SkinnedMeshRendererシリアライザ
    └── UI/
        └── AvatarContextWindow.cs                  # EditorWindow（Tools > Avatar Context）
```

## 依存関係

### 必須依存

| パッケージ | バージョン | 用途 |
|:--|:--|:--|
| `com.mame8.animator-controller-context` | v0.1.0 以上 | AnimatorControllerのシリアライズ |

### オプション依存

| パッケージ | 条件コンパイルフラグ | 用途 |
|:--|:--|:--|
| `com.vrchat.avatars` (VRC Avatars SDK) | `AMAC_VRC_SDK` | Avatar Descriptor, Expression Parameters/Menu, PhysBone, Contact |
| `nadena.dev.modular-avatar` (Modular Avatar) | `AMAC_MODULAR_AVATAR` | MA Merge Animator, Parameters, MenuItem, BlendshapeSync 等 |

オプション依存パッケージがインストールされていない場合、対応するセクションはスキップされます。コンパイルエラーにはなりません。

## AnimatorController Context パッケージとの関係

本パッケージは [AnimatorController Context](https://github.com/mame8/AnimatorController_Context)（`com.mame8.animator-controller-context`）の上位ラッパーです。

- **AnimatorController Context** - AnimatorController単体をテキストに双方向変換
- **AnimatorController MA Context** - アバター全体の構成をテキストに変換（AnimatorControllerの部分は上記パッケージに委譲）

具体的な連携方法は以下の通りです。

1. `AvatarContextSerializer` がシリアライズ中にAnimatorControllerを `SerializeContext` に収集
2. 全セクションの出力後、収集したControllerを `AnimatorControllerSerializer.Serialize()` で出力
3. `ContextSerializeOptions.IncludeAnimationClips` は `SerializeOptions.IncludeAnimationClips` にそのまま渡される

コードの重複は一切なく、AnimatorController Context パッケージのアップデートは自動的に反映されます。
