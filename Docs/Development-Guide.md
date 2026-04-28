# 開発ガイド

## 開発環境セットアップ

### 必要な環境

| 項目 | 要件 |
|:--|:--|
| Unity | 2022.3 以上 |
| C# | .NET Standard 2.1 |
| 依存パッケージ | `com.mame8.animator-controller-context` v0.1.0 以上 |

### オプション依存

| パッケージ | 用途 |
|:--|:--|
| VRChat Avatars SDK (`com.vrchat.avatars`) | VRCコンポーネントのシリアライズ |
| Modular Avatar (`nadena.dev.modular-avatar`) | MAコンポーネントのシリアライズ |

オプション依存パッケージがなくてもコンパイル・動作します。対応セクションがスキップされるだけです。

### セットアップ手順

1. Unity 2022.3 以上のプロジェクトを用意
2. `com.mame8.animator-controller-context` v0.1.0 以上をインストール
3. `Assets/AnimatorControllerMAContext/` フォルダをプロジェクトに配置
4. 必要に応じて VRChat SDK / Modular Avatar をインストール
5. Unity Editorでコンパイルエラーがないことを確認
6. `Tools > Avatar Context` でウィンドウが開くことを確認

---

## 条件コンパイルフラグ

### フラグ定義

| フラグ | 定義条件 | asmdef設定 |
|:--|:--|:--|
| `AMAC_VRC_SDK` | `com.vrchat.avatars` がインストール済み | `versionDefines` で自動定義 |
| `AMAC_MODULAR_AVATAR` | `nadena.dev.modular-avatar` がインストール済み | `versionDefines` で自動定義 |

### asmdef の versionDefines 設定

```json
{
  "versionDefines": [
    {
      "name": "com.vrchat.avatars",
      "expression": "",
      "define": "AMAC_VRC_SDK"
    },
    {
      "name": "nadena.dev.modular-avatar",
      "expression": "",
      "define": "AMAC_MODULAR_AVATAR"
    }
  ]
}
```

`expression` が空の場合、パッケージが存在すればバージョンに関係なくフラグが定義されます。

### コード内での使用例

```csharp
#if AMAC_VRC_SDK
// VRC SDK がインストールされている場合のみ実行されるコード
VRCComponentSerializer.SerializeAvatarDescriptor(sb, avatarRoot, ctx);
#endif

#if AMAC_MODULAR_AVATAR
// Modular Avatar がインストールされている場合のみ実行されるコード
MAComponentSerializer.Serialize(sb, avatarRoot, ctx);
#endif
```

---

## asmdef 設定

### Assembly定義

```json
{
  "name": "AnimatorControllerMAContext.Editor",
  "rootNamespace": "AnimatorControllerMAContext.Editor",
  "references": [
    "AnimatorControllerContext.Editor",
    "VRC.SDK3A",
    "VRC.SDKBase",
    "VRC.SDK3.Dynamics.PhysBone",
    "VRC.SDK3.Dynamics.Contact",
    "nadena.dev.modular-avatar.core",
    "nadena.dev.ndmf.runtime"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [...]
}
```

**ポイント:**
- `includePlatforms: ["Editor"]` - Editor専用アセンブリ。ランタイムでは使用不可
- Assembly referenceはソフトリンク。未インストールのパッケージへの参照は無視される
- `AnimatorControllerContext.Editor` は必須依存（`package.json` でも宣言）
- VRC SDK / MA の assembly reference は未インストール時に自動スキップ

---

## SerializedPropertyベース設計

### 設計の理由

本パッケージでは、VRC SDK や Modular Avatar のC# APIを直接参照せず、`SerializedProperty` 経由でコンポーネントのプロパティにアクセスしています。

```csharp
// 直接参照（使用しない）:
// VRCAvatarDescriptor descriptor = comp as VRCAvatarDescriptor;
// Vector3 viewPos = descriptor.ViewPosition;

// SerializedPropertyベース（採用）:
var so = new SerializedObject(comp);
var viewPos = so.FindProperty("ViewPosition");
Vector3 v = viewPos.vector3Value;
```

**この設計の利点:**

1. **SDKバージョン耐性** - プロパティ名が変わらない限り、SDKのAPI変更に影響されない
2. **ソフトリンク** - C# の型参照がないため、SDKがインストールされていなくてもコンパイル可能
3. **統一的なアクセスパターン** - VRCコンポーネントもMAコンポーネントも同じパターンでアクセス
4. **汎用フォールバック** - 未知のコンポーネントも `ComponentSerializer` で汎用出力可能

**コンポーネント型判定:**

```csharp
// 型名の文字列比較で判定
string typeName = component.GetType().Name;
if (typeName == "VRCAvatarDescriptor") { ... }
if (typeName.StartsWith("ModularAvatar")) { ... }
```

### FindProperty が null を返す場合

SDKのバージョンアップでプロパティ名が変更される可能性があるため、`FindProperty()` の戻り値は必ずnullチェックします。

```csharp
var prop = so.FindProperty("someProperty");
if (prop != null)
{
    sb.AppendLine($"{indent}Some Property: {prop.floatValue:G}");
}
// prop が null の場合は、そのフィールドをスキップ
```

### SerializedObject の Dispose

`SerializedObject` は使用後に必ず `Dispose()` を呼び出します。

```csharp
var so = new SerializedObject(comp);
try
{
    // プロパティアクセス
}
finally
{
    so.Dispose(); // ← 必須
}

// または簡潔に
var so = new SerializedObject(comp);
// ... プロパティアクセス ...
so.Dispose();
```

---

## コーディング規約

### 新しいMAコンポーネントの対応追加

新しいModular Avatarコンポーネントに対応する場合、`MAComponentSerializer` に専用メソッドを追加します。

**手順:**

1. `SerializeMAComponent()` の switch文に新しい case を追加

```csharp
case "ModularAvatarNewComponent":
    SerializeNewComponent(sb, comp, indent);
    break;
```

2. 専用シリアライズメソッドを実装

```csharp
private static void SerializeNewComponent(StringBuilder sb, Component comp, string indent)
{
    var so = new SerializedObject(comp);

    var someProp = so.FindProperty("someProperty");
    if (someProp != null)
        sb.AppendLine($"{indent}Some Property: {someProp.stringValue}");

    so.Dispose();
}
```

3. 実装前に Unity Inspector のデバッグモードでプロパティ名を確認

### 型名判定

MAコンポーネントの型名は `GetType().Name` で取得し、完全一致で判定します。

```csharp
// 正しい判定
case "ModularAvatarMergeAnimator":

// MAコンポーネント全体の判定
if (comp.GetType().Name.StartsWith("ModularAvatar"))
```

VRCコンポーネントの型名判定も同様です。

```csharp
case "VRCPhysBone":
case "VRCPhysBoneCollider":
case "VRCContactSender":
case "VRCContactReceiver":
```

### 不明コンポーネントのフォールバック

専用シリアライザがカバーしないMAコンポーネントは、自動的に `ComponentSerializer.Serialize()` にフォールバックします。

```csharp
default:
    ComponentSerializer.Serialize(sb, comp, indent);
    break;
```

これにより、MAの新バージョンで追加されたコンポーネントも、最低限の情報が出力されます。

### プロパティ名のバリエーション対応

MAのバージョンによってプロパティ名が異なる場合があります。複数の候補を試行するパターンを使用してください。

```csharp
// プライマリ名で試行、失敗したらフォールバック名
var objects = so.FindProperty("Objects");
if (objects == null || !objects.isArray || objects.arraySize == 0)
{
    objects = so.FindProperty("m_objects");
}

// 子プロパティでも同様
var objRef = entry.FindPropertyRelative("Object")
          ?? entry.FindPropertyRelative("target");
var active = entry.FindPropertyRelative("Active")
          ?? entry.FindPropertyRelative("active");
```

### SerializedObject の Dispose

`SerializedObject` は必ず `try-finally` ブロック内で使用すること:

```csharp
var so = new SerializedObject(comp);
try
{
    // プロパティアクセス
}
finally
{
    so.Dispose();
}
```

### オプションプロパティ

新規オプションプロパティは public field ではなく auto-property (`{ get; set; }`) を使用すること:

```csharp
// 正しい
public bool IncludeHierarchy { get; set; } = true;

// 誤り
public bool IncludeHierarchy = true;
```

### ルックアップ用コレクション

頻繁にルックアップされるコレクション（スキップ対象プロパティ名など）は `HashSet<string>` を使用すること:

```csharp
// 正しい（O(1) ルックアップ）
private static readonly HashSet<string> SkippedPropertyNames = new HashSet<string>
{
    "m_ObjectHideFlags", "m_Script", "m_Enabled"
};

// 誤り（O(n) ルックアップ）
private static readonly string[] SkippedPropertyNames = new[]
{
    "m_ObjectHideFlags", "m_Script", "m_Enabled"
};
```

### 配列の表示制限

大きな配列を全て出力するとテキストが膨大になるため、`ComponentSerializer` では50要素の上限を設けています。

```csharp
private const int MaxArrayDisplayCount = 50;

int maxShow = System.Math.Min(prop.arraySize, MaxArrayDisplayCount);
for (int i = 0; i < maxShow; i++)
{
    // 要素を出力
}
if (prop.arraySize > maxShow)
{
    sb.AppendLine($"{indent}  ... (+{prop.arraySize - maxShow} more)");
}
```

### インデントの規約

| 対象 | インデント | 文字列表現 |
|:--|:--|:--|
| セクションヘッダー | なし | `""` |
| トップレベルプロパティ | 2スペース | `"  "` |
| コンポーネントヘッダー | 2スペース | `"  "` |
| コンポーネントプロパティ | 4スペース | `"    "` |
| ネストされた子プロパティ | 6スペース | `"      "` |
| Hierarchyルート | 2スペース | `"  "` |
| Hierarchy子 | +2スペース/階層 | `"    "`, `"      "`, ... |

### 数値フォーマット

float値は `:G` フォーマットで出力します。これにより不要な末尾のゼロが除去されます。

```csharp
sb.AppendLine($"{indent}Pull: {prop.floatValue:G}");
// 0.2 → "0.2" (not "0.200000")
// 1 → "1" (not "1.000000")
```

---

## AnimatorController Context パッケージとの連携

### Controller の収集

シリアライズ中に発見された AnimatorController は `SerializeContext.Controllers` (HashSet) に収集されます。

**収集元:**
1. アバタールートの `Animator` コンポーネント（`AvatarContextSerializer` 内）
2. VRC Avatar Descriptor の Playable Layers（`VRCComponentSerializer.SerializePlayableLayers()` 内）
3. MA Merge Animator の参照Controller（`MAComponentSerializer.SerializeMergeAnimator()` 内）

```csharp
// Controller を収集
ctx.CollectController(controllerObject);

// CollectController の実装
public void CollectController(Object obj)
{
    if (obj is AnimatorController ac)
        Controllers.Add(ac);
}
```

HashSet を使用しているため、同じControllerが複数箇所から参照されても重複出力されません。

### AnimatorControllerSerializer の呼び出し

最後のセクションで、収集した全Controllerを `AnimatorControllerSerializer.Serialize()` で出力します。

```csharp
var serializeOptions = new SerializeOptions
{
    IncludeAnimationClips = ctx.Options.IncludeAnimationClips
};

foreach (var controller in ctx.Controllers)
{
    if (controller == null) continue;
    sb.AppendLine(AnimatorControllerSerializer.Serialize(controller, serializeOptions));
}
```

`ContextSerializeOptions.IncludeAnimationClips` は AnimatorController Context パッケージの `SerializeOptions.IncludeAnimationClips` に透過的に渡されます。

---

## テスト

### 手動テスト手順

現在、自動テストは未実装です。以下の手動テスト手順で品質を確認してください。

1. **VRC SDK + MA インストール済み環境**
   - 各種構成のアバターでSerializeを実行
   - 全セクションが正しく出力されること
   - AnimatorControllerセクションが正しくシリアライズされること

2. **オプション制御**
   - 各オプションをOFFにして、対応セクションが出力されないことを確認
   - `IncludeAnimatorControllers` OFF時に `IncludeAnimationClips` トグルが無効化されること

3. **エクスポート**
   - クリップボードコピーが動作すること
   - ファイル保存が動作すること（UTF-8エンコーディング）

4. **SDK未インストール環境**
   - VRC SDK / MA 未インストール環境でコンパイルエラーにならないこと
   - 対応セクションが出力されないこと（エラーにはならない）

5. **エッジケース**
   - avatarRoot が null の場合にエラーメッセージが返ること
   - Missing Script があるGameObjectでクラッシュしないこと
   - 循環参照するExpression Menuで無限ループしないこと
   - ブレンドシェイプが0個のSkinnedMeshRendererが正しく処理されること
   - 非アクティブなGameObjectのコンポーネントがスキップされないこと（`includeInactive: true`）
