# エディタウィンドウ使用ガイド

## ウィンドウの開き方

Unity Editorのメニューバーから **Tools > Avatar Context** を選択します。

```
Unity メニューバー > Tools > Avatar Context
```

ウィンドウのタイトルは "Avatar Context"、最小サイズは 450 x 400 ピクセルです。通常のEditorWindowと同様にドッキング、リサイズ、フロート表示が可能です。

---

## ウィンドウ構成

ウィンドウは上から順に以下の要素で構成されています。

```
+------------------------------------------+
| Avatar Root: [        (ObjectField)     ] |
|                                          |
| > Options                                |
|   [ ] Avatar Descriptor                  |
|   [ ] Expression Parameters              |
|   [ ] Expression Menu                    |
|   [ ] Hierarchy                          |
|   [ ] Modular Avatar Components          |
|   [ ] VRC Components (PhysBone etc.)     |
|   [ ] Renderers (BlendShapes)            |
|   [ ] AnimatorControllers                |
|       [ ] AnimationClips in Controllers  |
|                                          |
| [Serialize] [Copy to Clipboard] [Save]   |
| Lines: 1,234  |  Characters: 45,678      |
|                                          |
| +--------------------------------------+ |
| |                                      | |
| |        (結果テキストエリア)            | |
| |                                      | |
| +--------------------------------------+ |
+------------------------------------------+
```

---

## アバタールートの設定

### ObjectField による設定

ウィンドウ上部の "Avatar Root" フィールドに、アバターのルートGameObjectをドラッグ&ドロップするか、フィールド右の丸いアイコンをクリックしてオブジェクトピッカーから選択します。

アバタールートとは、通常 `VRCAvatarDescriptor` コンポーネントが付いている最上位のGameObjectです。

### シーン選択からの自動検出

Avatar Root フィールドが空の状態で、Hierarchyウィンドウでオブジェクトを選択すると、選択したオブジェクトから親を辿って `VRCAvatarDescriptor` を持つGameObjectを自動検出し、Avatar Root に設定します。

**自動検出の条件:**
- Avatar Root が未設定（null）であること
- Hierarchy 上で何らかのGameObjectが選択されていること
- 選択されたGameObjectまたはその親に `VRCAvatarDescriptor` コンポーネントが存在すること

Avatar Root が既に設定されている場合、シーン選択による自動検出は行われません。

### 設定変更時の動作

Avatar Root を変更すると、前回のシリアライズ結果はクリアされます。

---

## オプション設定

"Options" フォールドアウトを展開すると、9つのトグルが表示されます。全てデフォルトでON（チェック済み）です。

### トグル一覧

| トグル名 | 対応オプション | 説明 |
|:--|:--|:--|
| Avatar Descriptor | `IncludeAvatarDescriptor` | Avatar Descriptor + Playable Layersセクション |
| Expression Parameters | `IncludeExpressionParameters` | Expression Parametersセクション |
| Expression Menu | `IncludeExpressionMenu` | Expression Menuセクション |
| Hierarchy | `IncludeHierarchy` | GameObjectツリーセクション |
| Modular Avatar Components | `IncludeMAComponents` | MA Componentsセクション |
| VRC Components (PhysBone etc.) | `IncludeVRCComponents` | PhysBones + Contactsセクション |
| Renderers (BlendShapes) | `IncludeRenderers` | Renderersセクション |
| AnimatorControllers | `IncludeAnimatorControllers` | AnimatorControllersセクション |
| AnimationClips in Controllers | `IncludeAnimationClips` | Controller内のAnimationClipデータ |

### ネストトグル

"AnimationClips in Controllers" は "AnimatorControllers" の子トグルです。"AnimatorControllers" がOFFの場合、"AnimationClips in Controllers" は自動的に無効化（グレーアウト）されます。

### 推奨設定パターン

| 目的 | 推奨設定 |
|:--|:--|
| 全情報を取得 | 全てON（デフォルト） |
| 構成概要のみ | AnimatorControllers: OFF |
| MA設定の確認 | Modular Avatar Components: ON、他はOFF |
| 出力サイズ削減（中） | AnimationClips in Controllers: OFF |
| 出力サイズ削減（大） | AnimatorControllers: OFF |
| PhysBone設定の確認 | VRC Components: ON、他はOFF |
| メッシュ/ブレンドシェイプの確認 | Renderers: ON、他はOFF |

---

## シリアライズの実行

1. Avatar Root にアバターのルートGameObjectを設定
2. 必要に応じてOptionsを調整
3. **Serialize** ボタンをクリック

Avatar Root が未設定の場合、Serialize ボタンは無効化されています。

シリアライズが完了すると、結果テキストがウィンドウ下部のテキストエリアに表示され、統計情報（行数、文字数）がボタンの下に表示されます。

```
Lines: 1,234  |  Characters: 45,678
```

---

## 結果のコピー・保存

### クリップボードにコピー

**Copy to Clipboard** ボタンをクリックすると、結果テキストがシステムクリップボードにコピーされます。コピー完了時にウィンドウ上部に "Copied to clipboard" 通知が表示されます。

コピーしたテキストは、そのままAI（Claude、GPT等）のチャットに貼り付けて使用できます。

### ファイルに保存

**Save to File** ボタンをクリックすると、ファイル保存ダイアログが開きます。

- デフォルトファイル名: `{AvatarName}_context.txt`
- エンコーディング: UTF-8
- 保存先: 任意のフォルダ

保存完了時にウィンドウ上部に "Saved" 通知が表示され、Console に保存先パスがログ出力されます。

```
[Avatar Context] Saved to C:/Users/.../AvatarName_context.txt
```

### ボタンの有効/無効状態

| ボタン | 有効条件 |
|:--|:--|
| Serialize | Avatar Root が設定済み |
| Copy to Clipboard | シリアライズ結果が存在する |
| Save to File | シリアライズ結果が存在する |

---

## ヘルプテキスト

シリアライズ前（結果が空の状態）では、テキストエリアの代わりに以下のヘルプメッセージが表示されます。

```
Avatar RootにアバターのルートGameObjectを設定し、Serializeをクリックしてください。

生成されるコンテキストには以下が含まれます:
- VRC Avatar Descriptor (Playable Layers, Expressions)
- Modular Avatar コンポーネント設定
- PhysBone / Contact コンポーネント
- SkinnedMeshRenderer (BlendShapes)
- AnimatorController (AnimatorController Contextパッケージ利用)
```

---

## AI活用のヒント

### 基本的な使い方

1. ウィンドウでアバターをシリアライズ
2. **Copy to Clipboard** でコピー
3. AIチャット（Claude、GPT等）に貼り付け
4. アバター構成に関する質問や指示を添える

### プロンプト例

**構成の概要把握:**
```
以下はVRChatアバターの構成データです。アバターの全体構成を要約してください。

[ここにシリアライズ結果を貼り付け]
```

**設定のレビュー:**
```
以下のVRChatアバター構成を確認し、改善点や問題点があれば指摘してください。
特にPhysBoneの設定値とExpression Parametersの同期コストに注目してください。

[ここにシリアライズ結果を貼り付け]
```

**MA構成の分析:**
```
以下のアバターのModular Avatarコンポーネント構成を分析してください。
各MAコンポーネントがどのように連携しているか説明してください。

[ここにシリアライズ結果を貼り付け]
```

**AnimatorControllerの編集支援:**
```
以下のアバターのFXレイヤーに新しいToggle機能を追加したいです。
「Glasses」というオブジェクトのON/OFFを切り替えるレイヤーを、
既存の構成に合わせた形で設計してください。

[ここにシリアライズ結果を貼り付け]
```

### コンテキスト制限への対処

AIにはコンテキスト（入力テキスト）の上限があります。アバターの構成が複雑な場合、全セクションを含めるとコンテキスト上限を超える可能性があります。

**対処法:**

1. **セクション選択** - 質問に関係するセクションのみONにする
   - PhysBoneについて質問したい場合: VRC Components のみON
   - MA構成について質問したい場合: Modular Avatar Components のみON

2. **AnimationClips の除外** - AnimationClipsはテキスト量が非常に大きくなるため、AnimatorControllerの構造だけ必要な場合は "AnimationClips in Controllers" をOFFにする

3. **AnimatorControllers の除外** - AnimatorControllerの情報が不要な場合は "AnimatorControllers" 自体をOFFにする（最も効果的なサイズ削減）

4. **ファイル保存の活用** - Claudeなどの一部のAIではファイルアップロードに対応しています。大きなコンテキストはファイルとして保存し、アップロードする方が確実です

### テキストサイズの目安

典型的なVRChatアバターでの各セクションの概算サイズは以下の通りです（実際のサイズはアバターの複雑さによって大きく異なります）。

| セクション | 概算サイズ |
|:--|:--|
| Avatar Descriptor + Playable Layers | 500 - 1,000 文字 |
| Expression Parameters | 500 - 3,000 文字 |
| Expression Menu | 500 - 5,000 文字 |
| Hierarchy | 2,000 - 20,000 文字 |
| Modular Avatar Components | 1,000 - 10,000 文字 |
| PhysBones + Contacts | 1,000 - 10,000 文字 |
| Renderers | 1,000 - 20,000 文字 |
| AnimatorControllers（Clips除外） | 5,000 - 50,000 文字 |
| AnimationClips | 10,000 - 200,000 文字 |
