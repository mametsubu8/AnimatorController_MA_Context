# 出力テキストフォーマット仕様

## 概要

`AvatarContextSerializer.Serialize()` が生成するテキストは、セクションヘッダー (`--- Section Name ---`) で区切られた構造化テキストです。各セクションは `ContextSerializeOptions` のフラグで個別にON/OFFできます。

## 全体構造

```
=== Avatar Context: {AvatarName} ===

--- Avatar Descriptor ---
  ...

--- Playable Layers ---
  ...

--- Expression Parameters (Cost: N/256) ---
  ...

--- Expression Menu ---
  ...

--- Hierarchy ---
  ...

--- Modular Avatar Components ---
  ...

--- PhysBones ---
  ...

--- Contacts ---
  ...

--- Renderers ---
  ...

--- AnimatorControllers ---
=== AnimatorController: ControllerName ===
  ...
```

## セクション出力順序

| 順序 | セクション | 対応オプション | 出力条件 |
|:--:|:--|:--|:--|
| 1 | ヘッダー | - | 常に出力 |
| 2 | Avatar Descriptor | `IncludeAvatarDescriptor` | VRCAvatarDescriptorが存在する場合 |
| 3 | Playable Layers | `IncludeAvatarDescriptor` | Avatar Descriptorセクションの一部 |
| 4 | Expression Parameters | `IncludeExpressionParameters` | カスタムExpressionsが有効な場合 |
| 5 | Expression Menu | `IncludeExpressionMenu` | カスタムExpressionsが有効な場合 |
| 6 | Hierarchy | `IncludeHierarchy` | 常に出力 |
| 7 | Modular Avatar Components | `IncludeMAComponents` | MAコンポーネントが存在する場合 |
| 8 | PhysBones | `IncludeVRCComponents` | PhysBone/Colliderが存在する場合 |
| 9 | Contacts | `IncludeVRCComponents` | Contact Sender/Receiverが存在する場合 |
| 10 | Renderers | `IncludeRenderers` | SkinnedMeshRendererが存在する場合 |
| 11 | AnimatorControllers | `IncludeAnimatorControllers` | 収集されたControllerが存在する場合 |

## 各セクション詳細

### ヘッダー

```
=== Avatar Context: {AvatarName} ===
```

アバターのルートGameObject名が入ります。常に出力されます。

---

### Avatar Descriptor

```
--- Avatar Descriptor ---
  View Position: (x, y, z)
  Lip Sync: {mode}
  Lip Sync Mesh: {mesh_name}
  Eye Look: {Enabled|Disabled}
  Auto Footsteps: {true|false}
  Auto Locomotion: {true|false}
```

| フィールド | 型 | 説明 |
|:--|:--|:--|
| View Position | Vector3 | ビューポイントの座標 |
| Lip Sync | Enum | リップシンクモード（Viseme Blend Shape等） |
| Lip Sync Mesh | String | リップシンク対象メッシュ名（設定時のみ） |
| Eye Look | Enabled/Disabled | アイトラッキング有効/無効 |
| Auto Footsteps | Boolean | 自動足音の有効/無効 |
| Auto Locomotion | Boolean | 自動ロコモーションの有効/無効 |

**フォーマットルール:**
- 各フィールドは2スペースインデント
- Vector3は `(x, y, z)` 形式
- プロパティが存在しない場合はそのフィールドをスキップ

---

### Playable Layers

```
--- Playable Layers ---
  [{LayerType}] {ControllerName} (Mask: {MaskName})
  [{LayerType}] (default)
  [{LayerType}] (none) [disabled]
```

Avatar Descriptorセクションの直後に出力されます。`customizeAnimationLayers` がfalseの場合は `(using defaults)` と表示されます。

| 表記 | 意味 |
|:--|:--|
| `[Base] FX_Controller (Mask: FX_Mask)` | カスタムController割当あり、マスク指定あり |
| `[Gesture] GestureController` | カスタムController割当あり、マスクなし |
| `[Additive] (default)` | デフォルトController使用 |
| `[Action] (none)` | Controller未割当 |
| `[Sitting] (none) [disabled]` | Controller未割当かつレイヤー無効 |

**LayerType の種類:** Base, Additive, Gesture, Action, FX, Sitting, TPose, IKPose

**フォーマットルール:**
- 各レイヤーは2スペースインデント
- LayerTypeは `[]` で囲む
- マスク指定がある場合は `(Mask: 名前)` を末尾に追加
- 無効レイヤーは `[disabled]` を末尾に追加
- 割り当てられたAnimatorControllerは `SerializeContext` に収集され、後の AnimatorControllers セクションで出力される

---

### Expression Parameters

```
--- Expression Parameters (Cost: {totalCost}/256) ---
  [{ParameterType}] {ParameterName} (Default: {value}, Saved, Synced)
```

ヘッダーには同期コストの合計値が含まれます。

| フィールド | 説明 |
|:--|:--|
| ParameterType | パラメータ型（Int / Float / Bool） |
| ParameterName | パラメータ名 |
| Default | デフォルト値 |
| Saved | 保存フラグ（設定時のみ表示） |
| Synced | 同期フラグ（設定時のみ表示） |

**コスト計算:**
- Int: 8 bits
- Float: 8 bits
- Bool: 1 bit
- 上限: 256 bits

**フォーマットルール:**
- 2スペースインデント
- パラメータ型は `[]` で囲む
- フラグ情報は `()` 内にカンマ区切り
- Saved / Synced はそれぞれ有効な場合のみ表示
- 名前が空のパラメータはスキップ
- `customExpressions` がfalseの場合はセクション全体をスキップ

---

### Expression Menu

```
--- Expression Menu ---
  [{ControlType}] {ControlName} (Parameter: {ParamName} = {value})
    SubParameter: {SubParamName}
  [{ControlType}] {MenuName}
    [{ControlType}] {NestedControlName} (Parameter: {ParamName} = {value})
```

メニューツリーを再帰的に展開します。

| フィールド | 説明 |
|:--|:--|
| ControlType | コントロール型（Toggle, Button, Sub Menu, Two Axis Puppet, Four Axis Puppet, Radial Puppet） |
| ControlName | コントロール表示名 |
| Parameter | 主パラメータ名と値 |
| SubParameter | サブパラメータ名（Puppet系コントロールで使用） |

**フォーマットルール:**
- 基本2スペースインデント。サブメニューはさらに2スペース追加
- コントロール型は `[]` で囲む
- パラメータ情報は `(Parameter: 名前 = 値)` 形式
- パラメータが未設定の場合はパラメータ情報を省略
- サブメニューは再帰的にインデントを深くして展開
- **循環参照防止**: 訪問済みメニューアセットは `SerializeContext.VisitedMenus` で追跡し、再訪問しない

---

### Hierarchy

```
--- Hierarchy ---
  {RootName} [{Component1}, {Component2}, SkinnedMeshRenderer ({blendShapeCount})]
    {ChildName} [{Component1}]
      {GrandChildName} [inactive]
        {GreatGrandChildName}
```

GameObjectツリーをインデント形式で表示します。

| 表記 | 意味 |
|:--|:--|
| `[VRCPhysBone, MAMergeAnimator]` | コンポーネント注釈 |
| `[SkinnedMeshRenderer (52)]` | SkinnedMeshRenderer（ブレンドシェイプ数） |
| `[inactive]` | GameObjectが非アクティブ |
| `[(Missing Script)]` | コンポーネントが欠損 |

**フォーマットルール:**
- ルートは2スペースインデント、子は追加で2スペースずつ深くなる
- Transform コンポーネントは表示しない
- Modular Avatarコンポーネントは `MA` プレフィックス付き短縮名（例: `ModularAvatarMergeAnimator` -> `MAMergeAnimator`）
- SkinnedMeshRendererにはブレンドシェイプ数を括弧付きで表示
- コンポーネントがないGameObjectは名前のみ（`[]` なし）
- 非アクティブGameObjectは名前の後に `[inactive]` を表示

---

### Modular Avatar Components

```
--- Modular Avatar Components ---

  [{MAShortName}] @ {RelativePath}
    {property}: {value}
    ...
```

アバター内の全Modular Avatarコンポーネントを一覧表示します。

| フィールド | 説明 |
|:--|:--|
| MAShortName | MA短縮名（`ModularAvatar` プレフィックスを `MA` に置換） |
| RelativePath | アバタールートからの相対パス |

**フォーマットルール:**
- コンポーネントヘッダーは2スペースインデント
- プロパティは4スペースインデント
- コンポーネント間は空行で区切り
- `ModularAvatar` で始まる型名のコンポーネントのみ対象
- 既知コンポーネントは専用フォーマット、不明コンポーネントは `ComponentSerializer` による汎用出力

各コンポーネントの詳細フォーマットは [Components.md](Components.md) を参照してください。

---

### PhysBones

```
--- PhysBones ---

  [PhysBone] @ {RelativePath}
    Root Transform: {name|(self)}
    Integration Type: {type}
    Pull: {value}
    Spring: {value}
    Stiffness: {value}
    Gravity: {value}
    Gravity Falloff: {value}
    Immobile Type: {type}
    Immobile: {value}
    Limit Type: {type}
    Max Angle X: {value}
    Max Angle Z: {value}
    Radius: {value}
    Endpoint: ({x}, {y}, {z})
    Allow Collision: {type}
    Allow Grabbing: {type}
    Allow Posing: {type}
    Parameter: {name}
    Colliders: {name1}, {name2}

  [PhysBone Collider] @ {RelativePath}
    Root Transform: {name}
    Shape Type: {type}
    Radius: {value}
    Height: {value}
    Position: ({x}, {y}, {z})
    Rotation: ({x}, {y}, {z})
    Inside Bounds: true
```

**フォーマットルール:**
- コンポーネントヘッダーは2スペースインデント
- プロパティは4スペースインデント
- PhysBoneとPhysBoneColliderは同じセクション内
- `Root Transform` が自身の場合は `(self)` と表示
- `Endpoint` / `Position` はゼロベクトルの場合省略
- `Rotation` はゼロの場合省略
- `Limit Type` が None（enumValueIndex 0）の場合、Max Angle X/Z は省略
- `Inside Bounds` はtrueの場合のみ表示
- Collidersは参照先オブジェクト名のカンマ区切り

---

### Contacts

```
--- Contacts ---

  [Contact Sender] @ {RelativePath}
    Root Transform: {name}
    Shape Type: {type}
    Radius: {value}
    Height: {value}
    Position: ({x}, {y}, {z})
    Allow Self: {true|false}
    Allow Others: {true|false}
    Local Only: true
    Collision Tags: {tag1}, {tag2}

  [Contact Receiver] @ {RelativePath}
    Root Transform: {name}
    Shape Type: {type}
    Radius: {value}
    Height: {value}
    Parameter: {name}
    Receiver Type: {type}
    Allow Self: {true|false}
    Allow Others: {true|false}
    Local Only: true
    Collision Tags: {tag1}, {tag2}
```

**フォーマットルール:**
- Contact SenderとContact Receiverは同じセクション内
- Senderが先、Receiverが後
- `Local Only` はtrueの場合のみ表示
- `Collision Tags` は空でない場合のみ表示
- その他のフォーマットルールはPhysBoneと同様

---

### Renderers

```
--- Renderers ---

  [SkinnedMeshRenderer] @ {RelativePath} [inactive] [disabled]
    Mesh: {MeshName}
    BlendShapes ({count}):
      {index}: {shapeName} = {weight}
      {index}: {shapeName}
    Materials: {Mat1}, {Mat2}, {Mat3}
```

| フィールド | 説明 |
|:--|:--|
| RelativePath | アバタールートからの相対パス |
| [inactive] | GameObjectが非アクティブ（該当時のみ） |
| [disabled] | Rendererコンポーネントが無効（該当時のみ） |
| Mesh | メッシュアセット名 |
| BlendShapes | ブレンドシェイプ一覧（カウント付き） |
| Materials | マテリアル名のカンマ区切り |

**フォーマットルール:**
- コンポーネントヘッダーは2スペースインデント
- プロパティは4スペースインデント
- ブレンドシェイプは6スペースインデント
- ブレンドシェイプのウェイトが0以外の場合は `= {weight}` を末尾に追加
- ウェイトが0の場合はシェイプ名のみ
- メッシュがnullの場合はMesh/BlendShapes行をスキップ
- マテリアルがnullの場合は `(none)` と表示

---

### AnimatorControllers

```
--- AnimatorControllers ---

=== AnimatorController: {ControllerName} ===

--- Parameters ---
  [{Type}] {ParameterName} (Default: {value})
  ...

--- Layers ---

  == Layer {index}: {LayerName} ==
  ...

--- AnimationClips ---

  [AnimationClip] {ClipName}
    ...
```

このセクションの内容は `AnimatorControllerContext.Editor.AnimatorControllerSerializer.Serialize()` の出力がそのまま使用されます。フォーマットの詳細はAnimatorController Contextパッケージのドキュメントを参照してください。

**コントローラー収集元:**
- アバタールートの `Animator` コンポーネント
- VRC Avatar Descriptorの Playable Layers
- MA Merge Animator の参照Controller

**`IncludeAnimationClips` オプション:**
- true: AnimatorController内のAnimationClipデータを含む（デフォルト）
- false: AnimationClipsセクションを省略（出力サイズ大幅削減）
- `IncludeAnimatorControllers` がfalseの場合、このオプションは無効

## パス表記

コンポーネントのパスは、アバタールートからの相対パスで表記されます。

```
Armature/Hips/Spine/Chest/Head
```

ルート自身の場合はルートのGameObject名が使用されます。

## テキストサイズの目安

出力テキストのサイズはアバターの構成によって大きく異なります。

| 構成要素 | サイズ影響 |
|:--|:--|
| AnimatorController | 大（特にFXレイヤーが複雑な場合） |
| AnimationClip | 大（Controller内のClip数とカーブ数に依存） |
| Hierarchy | 中（ボーン数に比例） |
| BlendShapes | 中（シェイプ数に比例） |
| PhysBone | 小〜中 |
| Expression Parameters / Menu | 小 |
| MA Components | 小〜中 |

AIのコンテキスト制限を超える場合は、`ContextSerializeOptions` で不要なセクションをOFFにしてください。特に `IncludeAnimatorControllers` と `IncludeAnimationClips` の無効化が効果的です。
