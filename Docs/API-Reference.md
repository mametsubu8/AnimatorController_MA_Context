# APIリファレンス

## 名前空間

すべてのpublicクラスは `AnimatorControllerMAContext.Editor` 名前空間に属します。

```csharp
using AnimatorControllerMAContext.Editor;
```

---

## AvatarContextSerializer

メインエントリーポイント。アバターのルートGameObjectを受け取り、全構成情報をテキスト形式に変換します。

### Serialize(GameObject)

```csharp
public static string Serialize(GameObject avatarRoot)
```

**説明:** デフォルトオプション（全セクション有効）でアバター構成をシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `avatarRoot` | `GameObject` | アバターのルートGameObject |

**戻り値:** シリアライズされたテキスト文字列。`avatarRoot` がnullの場合は `"[Error] Avatar root is null."` を返します。

**使用例:**
```csharp
string context = AvatarContextSerializer.Serialize(avatarRoot);
```

---

### Serialize(GameObject, ContextSerializeOptions)

```csharp
public static string Serialize(GameObject avatarRoot, ContextSerializeOptions options)
```

**説明:** 指定されたオプションに基づいてアバター構成をシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `options` | `ContextSerializeOptions` | シリアライズオプション（nullの場合はデフォルトが使用される） |

**戻り値:** シリアライズされたテキスト文字列。

**動作:**
1. ルートの `Animator` コンポーネントからAnimatorControllerを収集
2. `VRCComponentSerializer.SerializeAvatarDescriptor()` - Avatar Descriptor出力（Playable Layers含む。ここでもControllerを収集）
3. `VRCComponentSerializer.SerializeExpressionParameters()` - Expression Parameters出力
4. `VRCComponentSerializer.SerializeExpressionMenu()` - Expression Menu出力
5. `HierarchySerializer.Serialize()` - Hierarchy出力
6. `MAComponentSerializer.Serialize()` - Modular Avatarコンポーネント出力（MA Merge AnimatorのControllerも収集）
7. `VRCComponentSerializer.SerializePhysComponents()` - PhysBone / Contact出力
8. `RendererSerializer.Serialize()` - Renderer出力
9. 収集した全AnimatorControllerを `AnimatorControllerSerializer.Serialize()` で出力

**使用例:**
```csharp
var options = new ContextSerializeOptions
{
    IncludeAnimatorControllers = false,
    IncludeRenderers = false
};
string context = AvatarContextSerializer.Serialize(avatarRoot, options);
```

---

## ContextSerializeOptions

シリアライズ対象セクションを制御するオプションクラスです。

### フィールド

| フィールド | 型 | デフォルト | 説明 |
|:--|:--|:--:|:--|
| `IncludeHierarchy` | `bool` | `true` | Hierarchyセクションを含める |
| `IncludeAvatarDescriptor` | `bool` | `true` | Avatar Descriptor + Playable Layersセクションを含める |
| `IncludeExpressionParameters` | `bool` | `true` | Expression Parametersセクションを含める |
| `IncludeExpressionMenu` | `bool` | `true` | Expression Menuセクションを含める |
| `IncludeMAComponents` | `bool` | `true` | Modular Avatar Componentsセクションを含める |
| `IncludeVRCComponents` | `bool` | `true` | PhysBones / Contactsセクションを含める |
| `IncludeRenderers` | `bool` | `true` | Renderersセクションを含める |
| `IncludeAnimatorControllers` | `bool` | `true` | AnimatorControllersセクションを含める |
| `IncludeAnimationClips` | `bool` | `true` | AnimatorController内のAnimationClipデータを含める |

### 静的プロパティ

#### Default

```csharp
public static ContextSerializeOptions Default { get; }
```

**説明:** 全フィールドが `true` に設定された新しいインスタンスを返します。

### 注意事項

- `IncludeAnimationClips` は `IncludeAnimatorControllers` が `true` の場合にのみ効果があります
- `IncludeAnimationClips` を `false` にすると、AnimatorController内の `--- AnimationClips ---` セクションが省略され、出力サイズを大幅に削減できます
- EditorWindowでは `IncludeAnimationClips` トグルは `IncludeAnimatorControllers` がOFFのとき無効化されます

---

## SerializeContext

シリアライズ中の内部状態を保持するクラスです。アクセス修飾子は `internal` ですが、staticユーティリティメソッドは `public` です。

### プロパティ

| プロパティ | 型 | 説明 |
|:--|:--|:--|
| `Options` | `ContextSerializeOptions` | シリアライズオプション（読み取り専用） |
| `Controllers` | `HashSet<AnimatorController>` | 収集されたAnimatorControllerの集合 |
| `VisitedMenus` | `HashSet<Object>` | 訪問済みメニューアセットの集合（循環参照防止） |

### GetRelativePath(Transform, Transform)

```csharp
public static string GetRelativePath(Transform root, Transform target)
```

**説明:** ルートTransformからターゲットTransformへの相対パスを算出します。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `root` | `Transform` | 基点となるルートTransform |
| `target` | `Transform` | パスを取得したいTransform |

**戻り値:** スラッシュ区切りの相対パス文字列。`target` が `root` 自身の場合はルートの `name` を返します。

**使用例:**
```csharp
// root = "Avatar", target = "Avatar/Armature/Hips/Spine"
string path = SerializeContext.GetRelativePath(root, target);
// => "Armature/Hips/Spine"
```

---

### GetComponentDisplayName(Component)

```csharp
public static string GetComponentDisplayName(Component comp)
```

**説明:** コンポーネントの表示名を取得します。Modular Avatarコンポーネントの場合は短縮名を返します。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `comp` | `Component` | 表示名を取得するコンポーネント |

**戻り値:** コンポーネントの表示名。`comp` がnullの場合は `"(Missing Script)"` を返します。

**名前変換ルール:**
- `ModularAvatarMergeAnimator` -> `MAMergeAnimator`
- `ModularAvatarParameters` -> `MAParameters`
- `ModularAvatarMenuItem` -> `MAMenuItem`
- その他 `ModularAvatar*` -> `MA*`
- 上記以外はそのまま型名を返す

---

## ComponentSerializer

`SerializedProperty` を使った汎用コンポーネントシリアライザです。専用シリアライザがカバーしないコンポーネントのフォールバックとして使用されます。

### Serialize(StringBuilder, Component, string)

```csharp
public static void Serialize(StringBuilder sb, Component component, string indent)
```

**説明:** コンポーネントの全プロパティをkey-value形式でシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `component` | `Component` | シリアライズ対象のコンポーネント |
| `indent` | `string` | 各行の先頭に付加するインデント文字列 |

**動作:**
- `SerializedObject` のイテレータで全プロパティを走査
- 内部プロパティ（`m_ObjectHideFlags`, `m_Script`, `m_Name` 等）はスキップ
- 各プロパティは `SerializeProperty()` で型に応じた出力

**スキップされるプロパティ名:**
`m_ObjectHideFlags`, `m_Script`, `m_Name`, `m_EditorHideFlags`, `m_EditorClassIdentifier`, `m_GameObject`, `m_CorrespondingSourceObject`, `m_PrefabInstance`, `m_PrefabAsset`

---

### SerializeProperty(StringBuilder, SerializedProperty, string)

```csharp
public static void SerializeProperty(StringBuilder sb, SerializedProperty prop, string indent)
```

**説明:** 個別のSerializedPropertyを型に応じた形式で出力します。

**対応するプロパティ型:**

| SerializedPropertyType | 出力形式 |
|:--|:--|
| Integer | `{displayName}: {intValue}` |
| Boolean | `{displayName}: true/false` |
| Float | `{displayName}: {floatValue:G}` |
| String | `{displayName}: {stringValue}` |
| Enum | `{displayName}: {enumDisplayName}` |
| ObjectReference | `{displayName}: {objectName}` (nullの場合 `(none)`) |
| Vector2 | `{displayName}: ({x}, {y})` |
| Vector3 | `{displayName}: ({x}, {y}, {z})` |
| Vector4 | `{displayName}: ({x}, {y}, {z}, {w})` |
| Quaternion | `{displayName}: ({eulerX}, {eulerY}, {eulerZ})` |
| Color | `{displayName}: ({r}, {g}, {b}, {a})` |
| Bounds | `{displayName}: Center({cx}, {cy}, {cz}) Size({sx}, {sy}, {sz})` |
| AnimationCurve | `{displayName}: AnimationCurve ({keyCount} keys)` |
| LayerMask | `{displayName}: {intValue}` |
| 配列 | `SerializeArray()` に委譲 |
| その他 | `{displayName}: [{propertyType}]` |

---

### GetEnumValue(SerializedProperty)

```csharp
public static string GetEnumValue(SerializedProperty prop)
```

**説明:** Enum型のSerializedPropertyから表示名を取得します。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `prop` | `SerializedProperty` | Enum型のプロパティ |

**戻り値:** `enumDisplayNames` から取得した表示名。取得できない場合は `intValue` の文字列表現。`prop` がnullの場合は `"(null)"`。

### 配列の表示制限

配列プロパティは最大50要素まで表示されます。超過分は `... (+N more)` と表示されます。

---

## HierarchySerializer

GameObjectツリーをインデント形式でシリアライズします。

### Serialize(StringBuilder, Transform)

```csharp
public static void Serialize(StringBuilder sb, Transform root)
```

**説明:** ルートTransform以下の全GameObjectツリーをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `root` | `Transform` | ツリーのルートTransform |

**動作:**
- `--- Hierarchy ---` ヘッダーを出力
- ルートから再帰的に全子オブジェクトを走査
- 各GameObjectのコンポーネント注釈を `[]` 内に表示
- 2スペースずつインデントを深くする
- Transformコンポーネントは表示対象外
- 非アクティブGameObjectは `[inactive]` マーク
- SkinnedMeshRendererはブレンドシェイプ数を括弧付きで表示

---

## VRCComponentSerializer

VRC固有コンポーネントの専門シリアライザです。

### SerializeAvatarDescriptor(StringBuilder, GameObject, SerializeContext)

```csharp
public static void SerializeAvatarDescriptor(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
```

**説明:** VRCAvatarDescriptorの設定とPlayable Layersをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `ctx` | `SerializeContext` | シリアライズコンテキスト |

**動作:**
- `avatarRoot` から `VRCAvatarDescriptor` コンポーネントを型名で検索
- 見つからない場合は何も出力しない
- View Position, Lip Sync, Eye Look, Auto Footsteps, Auto Locomotion を出力
- Playable Layers（Base, Additive, Gesture, Action, FX + Special Layers）を出力
- 割り当てられたAnimatorControllerを `ctx.CollectController()` で収集

---

### SerializeExpressionParameters(StringBuilder, GameObject, SerializeContext)

```csharp
public static void SerializeExpressionParameters(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
```

**説明:** VRC Expression Parametersのパラメータ一覧と同期コストをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `ctx` | `SerializeContext` | シリアライズコンテキスト |

**動作:**
- `customExpressions` がfalseの場合はスキップ
- `expressionParameters` アセットがnullの場合はスキップ
- 各パラメータの型、名前、デフォルト値、Saved/Syncedフラグを出力
- Syncedパラメータの同期コスト合計を計算しヘッダーに含める

---

### SerializeExpressionMenu(StringBuilder, GameObject, SerializeContext)

```csharp
public static void SerializeExpressionMenu(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
```

**説明:** VRC Expression Menuのコントロールツリーを再帰的にシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `ctx` | `SerializeContext` | シリアライズコンテキスト |

**動作:**
- `customExpressions` がfalseの場合はスキップ
- `expressionsMenu` アセットがnullの場合はスキップ
- `ctx.VisitedMenus` をクリアしてから再帰展開開始
- サブメニューは再帰的に展開（循環参照はVisitedMenusで防止）
- Puppetコントロールのサブパラメータも出力

---

### SerializePhysComponents(StringBuilder, GameObject, SerializeContext)

```csharp
public static void SerializePhysComponents(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
```

**説明:** アバター内のVRC PhysBone, PhysBone Collider, Contact Sender, Contact Receiverをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `ctx` | `SerializeContext` | シリアライズコンテキスト |

**動作:**
- `GetComponentsInChildren<Component>(true)` で全コンポーネントを取得（非アクティブ含む）
- 型名で分類: `VRCPhysBone`, `VRCPhysBoneCollider`, `VRCContactSender`, `VRCContactReceiver`
- PhysBone / Colliderは `--- PhysBones ---` セクション
- Contact Sender / Receiverは `--- Contacts ---` セクション
- 各コンポーネントのプロパティをSerializedProperty経由で出力

---

## MAComponentSerializer

Modular Avatarコンポーネントの専門シリアライザです。

### Serialize(StringBuilder, GameObject, SerializeContext)

```csharp
public static void Serialize(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
```

**説明:** アバター内の全Modular Avatarコンポーネントをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |
| `ctx` | `SerializeContext` | シリアライズコンテキスト |

**動作:**
- `GetComponentsInChildren<Component>(true)` で全コンポーネントを取得
- `GetType().Name.StartsWith("ModularAvatar")` でMAコンポーネントを判定
- 型名でディスパッチし、専用シリアライザまたは汎用フォールバックを使用
- MA Merge AnimatorのAnimatorController参照を `ctx.CollectController()` で収集
- MAコンポーネントが0件の場合はセクション自体を出力しない

**対応コンポーネントと専用シリアライザ:**

| 型名 | メソッド |
|:--|:--|
| `ModularAvatarMergeAnimator` | `SerializeMergeAnimator()` |
| `ModularAvatarParameters` | `SerializeParameters()` |
| `ModularAvatarMenuItem` | `SerializeMenuItem()` |
| `ModularAvatarMenuInstaller` | `SerializeMenuInstaller()` |
| `ModularAvatarBlendshapeSync` | `SerializeBlendshapeSync()` |
| `ModularAvatarBoneProxy` | `SerializeBoneProxy()` |
| `ModularAvatarObjectToggle` | `SerializeObjectToggle()` |
| `ModularAvatarMeshSettings` | `SerializeMeshSettings()` |
| `ModularAvatarScaleAdjuster` | `SerializeScaleAdjuster()` |
| `ModularAvatarMergeBlendTree` | `SerializeMergeBlendTree()` |
| `ModularAvatarWorldFixedObject` | マーカー出力 `(World Fixed)` |
| `ModularAvatarVisibleHeadAccessory` | マーカー出力 `(Visible in First Person)` |
| `ModularAvatarMenuGroup` | マーカー出力 `(Menu Group)` |
| その他 | `ComponentSerializer.Serialize()` |

---

## RendererSerializer

SkinnedMeshRendererのシリアライザです。

### Serialize(StringBuilder, GameObject)

```csharp
public static void Serialize(StringBuilder sb, GameObject avatarRoot)
```

**説明:** アバター内の全SkinnedMeshRendererのメッシュ情報、ブレンドシェイプ、マテリアルをシリアライズします。

**パラメータ:**

| パラメータ | 型 | 説明 |
|:--|:--|:--|
| `sb` | `StringBuilder` | 出力先のStringBuilder |
| `avatarRoot` | `GameObject` | アバターのルートGameObject |

**動作:**
- `GetComponentsInChildren<SkinnedMeshRenderer>(true)` で全SMRを取得（非アクティブ含む）
- 各SMRについてパス、アクティブ状態、有効状態、メッシュ名、ブレンドシェイプ一覧、マテリアル一覧を出力
- SMRが0件の場合はセクション自体を出力しない
- ブレンドシェイプはインデックス、名前、非ゼロウェイトを出力
- マテリアルはカンマ区切りの名前リスト

---

## AvatarContextWindow

Unity EditorWindow。`Tools > Avatar Context` メニューから開きます。

### ShowWindow()

```csharp
[MenuItem("Tools/Avatar Context")]
public static void ShowWindow()
```

**説明:** Avatar Contextウィンドウを開くか、既に開いている場合はフォーカスします。

**動作:**
- ウィンドウの最小サイズは 450 x 400 ピクセル
- タイトルは "Avatar Context"

詳細な使い方は [EditorWindow-Guide.md](EditorWindow-Guide.md) を参照してください。
