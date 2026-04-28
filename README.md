# AnimatorController MA Context

## 概要

VRChatアバターの**AnimatorController・Modular Avatar・VRCコンポーネント**の構成と設定内容を、AI（Claude/GPT等）が理解しやすいテキスト形式にシリアライズするUnity Editorツールです。

AnimatorControllerのシリアライズには [AnimatorController Context](https://github.com/mame8/AnimatorController_Context) パッケージをそのまま利用しており、元パッケージの変更は自動的に反映されます。

## 機能

- **アバター全体コンテキスト生成** - アバターのルートGameObjectから、構成に関わる全情報をテキスト化
- **VRC Avatar Descriptor** - View Position、Lip Sync、Eye Look、Playable Layers
- **Expression Parameters** - パラメータ一覧（型・デフォルト値・同期コスト計算）
- **Expression Menu** - メニューツリーの再帰展開（サブメニュー対応）
- **Hierarchy** - GameObjectツリー（コンポーネント注釈付き、非アクティブ表示）
- **Modular Avatar** - MA Merge Animator、Parameters、MenuItem、BlendshapeSync、ObjectToggle等の詳細設定
- **PhysBone / Contact** - VRC PhysBone・Collider・Contact Sender/Receiverの設定値
- **Renderers** - SkinnedMeshRendererのブレンドシェイプ一覧（デフォルト値付き）
- **AnimatorController** - AnimatorController Contextパッケージによる完全シリアライズ（AnimationClip含む）
- **セクション選択** - 各セクションのON/OFFが可能（出力サイズ調整）
- **エクスポート** - クリップボード・ファイル保存対応

## 動作環境

- Unity 2022.3 以上
- [AnimatorController Context](https://github.com/mame8/AnimatorController_Context) `com.mame8.animator-controller-context` v3.1.0 以上（必須）
- VRChat Avatars SDK（任意・Avatar Descriptor等のシリアライズに必要）
- Modular Avatar（任意・MAコンポーネントのシリアライズに必要）

## インストール方法

### 前提

AnimatorController Context パッケージがインストール済みであること。

### VCC (VRChat Creator Companion) 経由

1. VCCにリポジトリを追加
2. プロジェクトの「Manage Project」から `AnimatorController MA Context` を追加

### Unity Package Manager 経由

1. `Window > Package Manager` を開く
2. `+` → `Add package from disk...`
3. パッケージフォルダ内の `package.json` を選択

### 手動インストール

`Assets/AnimatorControllerMAContext` フォルダをプロジェクトの `Assets` 配下にコピー

## 使い方

### Editor Window

1. Unity メニューから `Tools > Avatar Context` を開く
2. Avatar Root にアバターのルートGameObjectをドラッグ（またはシーン選択で自動検出）
3. Options でシリアライズするセクションを選択
4. `Serialize` をクリック
5. `Copy to Clipboard` でAIに貼り付け、または `Save to File` でファイル保存

### スクリプトからの使用

```csharp
using AnimatorControllerMAContext.Editor;

// 全セクションを含むデフォルトシリアライズ
string context = AvatarContextSerializer.Serialize(avatarRoot);

// オプション指定
var options = new ContextSerializeOptions
{
    IncludeHierarchy = true,
    IncludeAvatarDescriptor = true,
    IncludeExpressionParameters = true,
    IncludeExpressionMenu = true,
    IncludeMAComponents = true,
    IncludeVRCComponents = true,
    IncludeRenderers = true,
    IncludeAnimatorControllers = true,
    IncludeAnimationClips = true
};
string context = AvatarContextSerializer.Serialize(avatarRoot, options);

// AnimatorControllerを除外（出力サイズ削減）
var options = new ContextSerializeOptions
{
    IncludeAnimatorControllers = false
};
string context = AvatarContextSerializer.Serialize(avatarRoot, options);
```

## テキストフォーマット

生成されるテキストは以下の構成です。各セクションは `--- Section Name ---` ヘッダーで区切られます。

```
=== Avatar Context: AvatarName ===

--- Avatar Descriptor ---
  View Position: (0, 1.6, 0.3)
  Lip Sync: Viseme Blend Shape
  Lip Sync Mesh: Body
  Eye Look: Enabled

--- Playable Layers ---
  [Base] (default)
  [Additive] (default)
  [Gesture] GestureController
  [Action] (default)
  [FX] FX_Controller (Mask: FX_Mask)

--- Expression Parameters (Cost: 17/256) ---
  [Bool] HatVisible (Default: 0, Saved, Synced)
  [Int] Outfit (Default: 0, Saved, Synced)
  [Float] FaceBlend (Default: 0, Synced)

--- Expression Menu ---
  [Toggle] Hat (Parameter: HatVisible = 1)
  [Sub Menu] Clothes
    [Toggle] Jacket (Parameter: JacketVisible = 1)
  [Radial Puppet] Expression (Parameter: FaceBlend = 1)
    SubParameter: FaceBlend

--- Hierarchy ---
  AvatarRoot [Animator, VRCAvatarDescriptor, MAParameters]
    Armature
      Hips [VRCPhysBone]
        Spine
          Chest
            ...
    Body [SkinnedMeshRenderer (52)]
    HatToggle [MAMergeAnimator, MAMenuItem]

--- Modular Avatar Components ---

  [MAMergeAnimator] @ HatToggle
    Animator: Hat_FX
    Layer Type: FX
    Delete Attached Animator: true
    Path Mode: Relative
    Match Avatar Write Defaults: true

  [MAMenuItem] @ HatToggle
    Type: Toggle
    Parameter: HatVisible = 1

  [MAParameters] @ AvatarRoot
    Parameters:
      HatVisible [Bool] (Default: 1, Saved)

--- PhysBones ---

  [PhysBone] @ Armature/Hips
    Root Transform: (self)
    Integration Type: Simplified
    Pull: 0.2
    ...

--- Contacts ---

  [Contact Receiver] @ Armature/.../Head/HeadPat
    Shape Type: Sphere
    Radius: 0.3
    Parameter: HeadPat
    Receiver Type: Proximity

--- Renderers ---

  [SkinnedMeshRenderer] @ Body
    Mesh: Body_Mesh
    BlendShapes (52):
      0: vrc.blink_left
      1: vrc.blink_right
      ...
    Materials: Body_Mat

--- AnimatorControllers ---

=== AnimatorController: FX_Controller ===
  (AnimatorController Contextパッケージによる完全シリアライズ)
```

## ドキュメント

詳細なドキュメントは [Docs/](Docs/) フォルダを参照してください。

| ドキュメント | 内容 |
|---|---|
| [Overview.md](Docs/Overview.md) | プロジェクト概要・アーキテクチャ・依存関係 |
| [OutputFormat.md](Docs/OutputFormat.md) | 出力テキストフォーマット完全仕様 |
| [API-Reference.md](Docs/API-Reference.md) | 全クラス・メソッドのAPIリファレンス |
| [Components.md](Docs/Components.md) | 対応コンポーネント詳細（VRC・MA全種） |
| [Development-Guide.md](Docs/Development-Guide.md) | 開発ガイド・条件コンパイル・コーディング規約 |
| [EditorWindow-Guide.md](Docs/EditorWindow-Guide.md) | エディタウィンドウ使用ガイド・AI活用のヒント |

## 注意事項

- VRC SDKやModular Avatarがインストールされていない場合、対応するセクションは生成されません（エラーにはなりません）
- AnimatorControllerセクションはコントローラーの数や複雑さに応じて非常に長くなることがあります。AIのコンテキスト制限を考慮し、必要に応じてオプションでOFFにしてください
- コンポーネントのプロパティアクセスにはUnityの `SerializedProperty` を使用しているため、VRC SDK / Modular Avatarのバージョンアップで内部プロパティ名が変更された場合、一部のフィールドが出力されなくなる可能性があります
- Modular Avatarの既知のコンポーネント（MergeAnimator, Parameters, MenuItem等）には専用フォーマットが用意されていますが、未対応のMAコンポーネントは汎用フォーマットで出力されます

## ライセンス

MIT License
