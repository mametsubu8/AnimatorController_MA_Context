# 対応コンポーネント詳細

## 概要

本パッケージは、VRChat SDK コンポーネントと Modular Avatar コンポーネントを個別に認識し、各コンポーネントに最適化されたフォーマットでシリアライズします。コンポーネントの型判定は `component.GetType().Name` の文字列比較で行い、プロパティアクセスは `SerializedProperty` 経由で実施します。

---

## VRC コンポーネント

### VRCAvatarDescriptor

**出力セクション:** `--- Avatar Descriptor ---` + `--- Playable Layers ---`

アバターの基本設定とPlayable Layers構成を出力します。

| プロパティ | SerializedProperty名 | 出力フォーマット | 説明 |
|:--|:--|:--|:--|
| View Position | `ViewPosition` | `(x, y, z)` | ビューポイント座標 |
| Lip Sync | `lipSync` | Enum表示名 | リップシンクモード |
| Lip Sync Mesh | `VisemeSkinnedMesh` | オブジェクト名 | Viseme対象メッシュ（設定時のみ） |
| Eye Look | `enableEyeLook` | `Enabled` / `Disabled` | アイトラッキング |
| Auto Footsteps | `autoFootsteps` | `true` / `false` | 自動足音 |
| Auto Locomotion | `autoLocomotion` | `true` / `false` | 自動ロコモーション |

**Playable Layers:**

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| カスタマイズ有無 | `customizeAnimationLayers` | falseの場合は `(using defaults)` と表示 |
| Baseレイヤー群 | `baseAnimationLayers` | Base, Additive, Gesture, Action, FX |
| Specialレイヤー群 | `specialAnimationLayers` | Sitting, TPose, IKPose |

各レイヤーの構成:

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| レイヤー種別 | `type` | Enum（Base, Additive等） |
| Controller | `animatorController` | 割り当てAnimatorController（収集対象） |
| デフォルト使用 | `isDefault` | trueの場合 `(default)` |
| 有効/無効 | `isEnabled` | falseの場合 `[disabled]` |
| マスク | `mask` | AvatarMask（設定時のみ表示） |

**出力例:**
```
--- Avatar Descriptor ---
  View Position: (0, 1.6, 0.05)
  Lip Sync: Viseme Blend Shape
  Lip Sync Mesh: Body
  Eye Look: Enabled
  Auto Footsteps: true
  Auto Locomotion: true

--- Playable Layers ---
  [Base] (default)
  [Additive] (default)
  [Gesture] GestureController
  [Action] (default)
  [FX] FX_Controller (Mask: FX_Mask)
  [Sitting] (default)
  [TPose] (default)
  [IKPose] (default)
```

---

### VRCExpressionParameters

**出力セクション:** `--- Expression Parameters (Cost: N/256) ---`

アバターの同期パラメータ一覧と同期コストを出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| パラメータ名 | `name` | パラメータの識別名 |
| 型 | `valueType` | Int(0) / Float(1) / Bool(2) |
| デフォルト値 | `defaultValue` | 初期値（float） |
| 保存 | `saved` | true: アバター切替時に値を保持 |
| 同期 | `networkSynced` | true: 他プレイヤーに同期 |

**同期コスト計算:**

| パラメータ型 | コスト | enumValueIndex |
|:--|:--|:--|
| Int | 8 bits | 0 |
| Float | 8 bits | 1 |
| Bool | 1 bit | 2 |
| 上限 | 256 bits | - |

コスト計算はSyncedパラメータのみが対象です。非同期パラメータはコストに含まれません。

**出力条件:**
- `customExpressions` がtrueであること
- `expressionParameters` アセットがnullでないこと
- パラメータ配列が空でないこと
- 名前が空のパラメータはスキップ

**出力例:**
```
--- Expression Parameters (Cost: 25/256) ---
  [Bool] HatVisible (Default: 0, Saved, Synced)
  [Int] Outfit (Default: 0, Saved, Synced)
  [Float] FaceBlend (Default: 0, Synced)
  [Bool] PrivateMode (Default: 0, Saved)
```

---

### VRCExpressionsMenu

**出力セクション:** `--- Expression Menu ---`

Expression Menuのコントロールツリーを再帰的に展開して出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| コントロール名 | `name` | 表示名 |
| コントロール型 | `type` | Toggle, Button, Sub Menu, Two Axis Puppet, Four Axis Puppet, Radial Puppet |
| パラメータ | `parameter.name` | 主パラメータ名 |
| 値 | `value` | パラメータに設定する値 |
| サブパラメータ | `subParameters[].name` | Puppet系のサブパラメータ |
| サブメニュー | `subMenu` | Sub Menu先のメニューアセット（再帰展開） |

**循環参照防止:**

`SerializeContext.VisitedMenus` (HashSet) で訪問済みメニューアセットを追跡します。同じメニューアセットが再度出現した場合は展開をスキップし、無限ループを防止します。

**出力例:**
```
--- Expression Menu ---
  [Toggle] Hat (Parameter: HatVisible = 1)
  [Sub Menu] Clothes
    [Toggle] Jacket (Parameter: JacketVisible = 1)
    [Toggle] Shoes (Parameter: ShoesVisible = 1)
  [Radial Puppet] Expression (Parameter: FaceBlend = 1)
    SubParameter: FaceBlend
  [Two Axis Puppet] EarMovement
    SubParameter: EarX
    SubParameter: EarY
```

---

### VRCPhysBone

**出力セクション:** `--- PhysBones ---`

| プロパティ | SerializedProperty名 | 出力条件 |
|:--|:--|:--|
| Root Transform | `rootTransform` | 常時（nullの場合 `(self)`） |
| Integration Type | `integrationType` | 常時 |
| Pull | `pull` | 常時 |
| Spring | `spring` | 常時 |
| Stiffness | `stiffness` | 常時 |
| Gravity | `gravity` | 常時 |
| Gravity Falloff | `gravityFalloff` | 常時 |
| Immobile Type | `immobileType` | 常時 |
| Immobile | `immobile` | 常時 |
| Limit Type | `limitType` | 常時 |
| Max Angle X | `maxAngleX` | `limitType` > 0 の場合 |
| Max Angle Z | `maxAngleZ` | `limitType` > 0 の場合 |
| Radius | `radius` | 常時 |
| Endpoint | `endpointPosition` | 非ゼロの場合 |
| Allow Collision | `allowCollision` | 常時 |
| Allow Grabbing | `allowGrabbing` | 常時 |
| Allow Posing | `allowPosing` | 常時 |
| Parameter | `parameter` | 空でない場合 |
| Colliders | `colliders` | 要素がある場合 |

**出力例:**
```
  [PhysBone] @ Armature/Hips/Spine/Chest/Hair
    Root Transform: Hair
    Integration Type: Simplified
    Pull: 0.2
    Spring: 0.5
    Stiffness: 0
    Gravity: 0.1
    Gravity Falloff: 0.5
    Immobile Type: All Motion
    Immobile: 0
    Limit Type: Angle
    Max Angle X: 45
    Max Angle Z: 45
    Radius: 0.02
    Endpoint: (0, 0.3, 0)
    Allow Collision: True
    Allow Grabbing: True
    Allow Posing: True
    Parameter: Hair
    Colliders: HeadCollider, ShoulderCollider
```

---

### VRCPhysBoneCollider

**出力セクション:** `--- PhysBones ---`（PhysBoneの後に続く）

| プロパティ | SerializedProperty名 | 出力条件 |
|:--|:--|:--|
| Root Transform | `rootTransform` | 設定時 |
| Shape Type | `shapeType` | 常時 |
| Radius | `radius` | 常時 |
| Height | `height` | 常時 |
| Position | `position` | 非ゼロの場合 |
| Rotation | `rotation` | 非ゼロの場合 |
| Inside Bounds | `insideBounds` | trueの場合のみ |

**出力例:**
```
  [PhysBone Collider] @ Armature/Hips/Spine/Chest/Head/HeadCollider
    Root Transform: Head
    Shape Type: Sphere
    Radius: 0.1
    Height: 0
    Inside Bounds: true
```

---

### VRCContactSender

**出力セクション:** `--- Contacts ---`

| プロパティ | SerializedProperty名 | 出力条件 |
|:--|:--|:--|
| Root Transform | `rootTransform` | 設定時 |
| Shape Type | `shapeType` | 常時 |
| Radius | `radius` | 常時 |
| Height | `height` | 常時 |
| Position | `position` | 非ゼロの場合 |
| Allow Self | `allowSelf` | 常時 |
| Allow Others | `allowOthers` | 常時 |
| Local Only | `localOnly` | trueの場合のみ |
| Collision Tags | `collisionTags` | タグがある場合 |

**出力例:**
```
  [Contact Sender] @ Armature/Hips/Spine/Chest/Head/HeadPatSender
    Root Transform: Head
    Shape Type: Sphere
    Radius: 0.15
    Height: 0
    Allow Self: false
    Allow Others: true
    Collision Tags: Head
```

---

### VRCContactReceiver

**出力セクション:** `--- Contacts ---`（Contact Senderの後に続く）

| プロパティ | SerializedProperty名 | 出力条件 |
|:--|:--|:--|
| Root Transform | `rootTransform` | 設定時 |
| Shape Type | `shapeType` | 常時 |
| Radius | `radius` | 常時 |
| Height | `height` | 常時 |
| Position | `position` | 非ゼロの場合 |
| Parameter | `parameter` | 空でない場合 |
| Receiver Type | `receiverType` | 常時 |
| Allow Self | `allowSelf` | 常時 |
| Allow Others | `allowOthers` | 常時 |
| Local Only | `localOnly` | trueの場合のみ |
| Collision Tags | `collisionTags` | タグがある場合 |

**出力例:**
```
  [Contact Receiver] @ Armature/Hips/Spine/Chest/Head/HeadPatReceiver
    Shape Type: Sphere
    Radius: 0.3
    Height: 0
    Parameter: HeadPat
    Receiver Type: Proximity
    Allow Self: false
    Allow Others: true
    Collision Tags: Head
```

---

## Modular Avatar コンポーネント

全てのMAコンポーネントは `--- Modular Avatar Components ---` セクション内に出力されます。

### MAMergeAnimator (ModularAvatarMergeAnimator)

AnimatorControllerのマージ設定を出力します。参照されているAnimatorControllerは `SerializeContext` に収集され、最後のAnimatorControllersセクションで完全出力されます。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Animator | `animator` | マージ対象のAnimatorController名 |
| Layer Type | `layerType` | マージ先レイヤータイプ（FX等） |
| Delete Attached Animator | `deleteAttachedAnimator` | アタッチされたAnimatorを削除するか |
| Path Mode | `pathMode` | パスの解決モード（Relative等） |
| Match Avatar Write Defaults | `matchAvatarWriteDefaults` | WDをアバターに合わせるか |
| Relative Path Root | `relativePathRoot.referencePath` | 相対パスのルート（設定時のみ） |

**出力例:**
```
  [MAMergeAnimator] @ HatToggle
    Animator: Hat_FX
    Layer Type: FX
    Delete Attached Animator: true
    Path Mode: Relative
    Match Avatar Write Defaults: true
```

---

### MAParameters (ModularAvatarParameters)

MAのパラメータ定義を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| nameOrPrefix | `nameOrPrefix` | パラメータ名またはプレフィックス |
| remapTo | `remapTo` | リマップ先パラメータ名 |
| internalParameter | `internalParameter` | 内部パラメータフラグ |
| isPrefix | `isPrefix` | プレフィックスモードフラグ |
| syncType | `syncType` | 同期タイプ（NotSynced, Bool, Int, Float） |
| localOnly | `localOnly` | ローカル専用フラグ |
| defaultValue | `defaultValue` | デフォルト値 |
| saved | `saved` | 保存フラグ |
| hasExplicitDefaultValue | `hasExplicitDefaultValue` | 明示的デフォルト値指定フラグ |

**出力フォーマット:**
```
    Parameters:
      {name} [{syncType}] (Default: {value}, Saved, LocalOnly, Internal, Prefix)
        Remap To: {remapTo}
```

括弧内のフラグはそれぞれ有効な場合のみ表示されます。`Remap To` はリマップ先が設定されている場合のみ表示されます。

**出力例:**
```
  [MAParameters] @ AvatarRoot
    Parameters:
      HatVisible [Bool] (Default: 1, Saved)
      Outfit [Int] (Saved, Internal)
        Remap To: AvatarOutfit
```

---

### MAMenuItem (ModularAvatarMenuItem)

MAのメニューアイテム設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Type | `Control.type` | コントロール型 |
| Parameter | `Control.parameter.name` | パラメータ名 |
| Value | `Control.value` | パラメータ値 |
| SubParameters | `Control.subParameters[].name` | サブパラメータ名リスト |

**出力例:**
```
  [MAMenuItem] @ HatToggle
    Type: Toggle
    Parameter: HatVisible = 1
```

**フォールバック:** `Control` プロパティが見つからない場合は `ComponentSerializer.Serialize()` による汎用出力にフォールバックします。

---

### MAMenuInstaller (ModularAvatarMenuInstaller)

メニューのインストール設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Menu To Append | `menuToAppend` | 追加するメニューアセット名 |
| Install Target | `installTargetMenu` | インストール先メニューアセット名 |

**出力例:**
```
  [MAMenuInstaller] @ Accessories
    Menu To Append: AccessoryMenu
    Install Target: MainMenu
```

---

### MABlendshapeSync (ModularAvatarBlendshapeSync)

ブレンドシェイプ同期設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Reference Mesh | `Bindings[].ReferenceMesh.referencePath` | 参照メッシュのパス |
| Blendshape | `Bindings[].Blendshape` | ソースブレンドシェイプ名 |
| Local Blendshape | `Bindings[].LocalBlendshape` | ローカルブレンドシェイプ名（異なる場合のみ表示） |

**出力フォーマット:**
```
    Bindings:
      {meshPath} :: {sourceShape}
      {meshPath} :: {sourceShape} -> {destShape}
```

ソースとデスティネーションが同名の場合は `->` 部分を省略します。

**出力例:**
```
  [MABlendshapeSync] @ Accessories/Hat
    Bindings:
      Body :: HatOn
      Body :: HatColor -> HatColorLocal
```

---

### MABoneProxy (ModularAvatarBoneProxy)

ボーンプロキシ設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Target | `target.referencePath` | ターゲットボーンのパス |
| Attachment Mode | `attachmentMode` | アタッチメントモード |

**出力例:**
```
  [MABoneProxy] @ Accessories/Hat
    Target: Armature/Hips/Spine/Chest/Head
    Attachment Mode: As Child At Root
```

---

### MAObjectToggle (ModularAvatarObjectToggle)

オブジェクトのON/OFF切り替え設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Object | `Objects[].Object.referencePath` または `Objects[].target` | 対象オブジェクトのパス |
| Active | `Objects[].Active` または `Objects[].active` | ON/OFF状態 |

**プロパティ名のバリエーション対応:**

MAのバージョンによってプロパティ名が異なる場合があるため、複数の名前を試行します。

| 試行順 | 配列プロパティ | オブジェクト参照 | アクティブフラグ |
|:--|:--|:--|:--|
| 1 | `Objects` | `Object` | `Active` |
| 2 | `m_objects` | `target` | `active` |

**出力例:**
```
  [MAObjectToggle] @ HatToggle
    Objects:
      Accessories/Hat -> ON
      Accessories/OldHat -> OFF
```

---

### MAMeshSettings (ModularAvatarMeshSettings)

メッシュ設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Inherit Bounds | `InheritBounds` | バウンズ継承設定（Enum） |
| Inherit Probe Anchor | `InheritProbeAnchor` | プローブアンカー継承設定（Enum） |
| Probe Anchor | `ProbeAnchor.referencePath` | プローブアンカーのパス |
| Bounds | `Bounds.m_Center` / `Bounds.m_Extent` | バウンズ（Center, Extent がゼロでない場合） |

**出力例:**
```
  [MAMeshSettings] @ Body
    Inherit Bounds: Set
    Inherit Probe Anchor: Set
    Probe Anchor: Armature/Hips
    Bounds: Center(0, 0.5, 0) Extent(1, 1, 1)
```

---

### MAScaleAdjuster (ModularAvatarScaleAdjuster)

スケール調整設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| Scale | `m_Scale` | スケール値 (x, y, z) |

**出力例:**
```
  [MAScaleAdjuster] @ Accessories/Wing
    Scale: (1.5, 1.5, 1.5)
```

---

### MAMergeBlendTree (ModularAvatarMergeBlendTree)

BlendTreeマージ設定を出力します。

| プロパティ | SerializedProperty名 | 説明 |
|:--|:--|:--|
| BlendTree | `BlendTree` または `m_blendTree` | BlendTreeアセット名 |
| Path Mode | `pathMode` | パスの解決モード |
| Layer Type | `layerType` | マージ先レイヤータイプ |

**出力例:**
```
  [MAMergeBlendTree] @ Accessories/FaceAnimation
    BlendTree: FaceBlendTree
    Path Mode: Relative
    Layer Type: FX
```

---

### MAWorldFixedObject (ModularAvatarWorldFixedObject)

マーカーコンポーネント。プロパティは出力しません。

**出力例:**
```
  [MAWorldFixedObject] @ Effects/WorldParticle
    (World Fixed)
```

---

### MAVisibleHeadAccessory (ModularAvatarVisibleHeadAccessory)

マーカーコンポーネント。プロパティは出力しません。

**出力例:**
```
  [MAVisibleHeadAccessory] @ Accessories/Glasses
    (Visible in First Person)
```

---

### MAMenuGroup (ModularAvatarMenuGroup)

マーカーコンポーネント。プロパティは出力しません。

**出力例:**
```
  [MAMenuGroup] @ MenuItems
    (Menu Group)
```

---

### 不明なMAコンポーネント

上記リストにない `ModularAvatar` プレフィックスを持つコンポーネントは、`ComponentSerializer.Serialize()` による汎用フォールバックで出力されます。

**フォールバック動作:**
- `SerializedProperty` のイテレータで全プロパティを走査
- Unity内部プロパティ（`m_Script`, `m_ObjectHideFlags` 等）はスキップ
- 各プロパティの型に応じた形式で出力
- 配列は最大50要素まで表示（超過分は `... (+N more)` と表示）

**出力例:**
```
  [MANewComponent] @ SomePath
    Some Property: someValue
    Some Array (3):
      [0] value1
      [1] value2
      [2] value3
```

---

## SkinnedMeshRenderer

**出力セクション:** `--- Renderers ---`

RendererSerializer が処理します。VRC固有コンポーネントではありませんが、アバター構成の重要な要素としてシリアライズ対象に含まれています。

| 出力情報 | 説明 |
|:--|:--|
| パス | アバタールートからの相対パス |
| アクティブ状態 | GameObjectが非アクティブの場合 `[inactive]` |
| 有効状態 | コンポーネントが無効の場合 `[disabled]` |
| Mesh | メッシュアセット名 |
| BlendShapes | ブレンドシェイプ一覧（インデックス、名前、非ゼロウェイト） |
| Materials | マテリアル名のカンマ区切り |

**ブレンドシェイプ出力ルール:**
- ウェイトが0の場合: `{index}: {shapeName}`
- ウェイトが0以外の場合: `{index}: {shapeName} = {weight}`

**出力例:**
```
  [SkinnedMeshRenderer] @ Body
    Mesh: Body_Mesh
    BlendShapes (52):
      0: vrc.blink_left
      1: vrc.blink_right
      2: vrc.lowerlid_left
      3: vrc.lowerlid_right
      10: Smile = 30
      11: Angry
    Materials: Body_Main, Body_Eye, Body_Mouth

  [SkinnedMeshRenderer] @ Accessories/Hat [inactive]
    Mesh: Hat_Mesh
    BlendShapes (2):
      0: HatOpen
      1: HatClose
    Materials: Hat_Mat
```
