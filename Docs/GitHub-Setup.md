# GitHub セットアップ手順

AnimatorController Context (Base) と同じ git 構成パターンを使用する。

## 構成概要

```
D:\Project_D\AnimatorController_MA_Context\     ← .git はここ
└── Assets\AnimatorControllerMAContext\          ← GIT_WORK_TREE はここ
    ├── .github/workflows/                      ← GitHub Actions（GitHub上ではリポルート）
    ├── .gitignore
    ├── CLAUDE.md
    ├── CHANGELOG.md
    ├── LICENSE
    ├── README.md
    ├── Website/                                ← VCC ランディングページ
    ├── Docs/
    ├── Editor/
    └── package.json
```

GitHub 上では `Assets/AnimatorControllerMAContext/` の中身がリポのルートとして見える。

## 手順

### 1. Git 初期化

```bash
cd D:\Project_D\AnimatorController_MA_Context

# .git をプロジェクトルートに作成
git init

# work tree をパッケージディレクトリに設定
git config core.worktree Assets/AnimatorControllerMAContext
```

### 2. 初期コミット

```bash
# GIT_WORK_TREE を指定してファイル操作
set GIT_WORK_TREE=Assets/AnimatorControllerMAContext

git add -A
git commit -m "feat: Initial release v1.0.0 — AnimatorController MA Context"
```

### 3. GitHub リポジトリ作成 & プッシュ

```bash
# GitHub にリポを作成（gh CLI）
gh repo create mametsubu8/AnimatorController_MA_Context --public --source=. --push

# または手動で remote 追加
git remote add origin https://github.com/mametsubu8/AnimatorController_MA_Context.git
git branch -M main
git push -u origin main
```

### 4. GitHub Pages 有効化

1. GitHub リポの Settings → Pages
2. Source: "GitHub Actions" を選択
3. build-listing.yml が自動でデプロイする

### 5. 初回リリース

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions が自動実行:
- `release.yml` → ZIP 生成 + GitHub Release 作成
- `build-listing.yml` → VPM リスティング生成 + Pages デプロイ

### 6. VCC ランディングページ確認

`https://mametsubu8.github.io/AnimatorController_MA_Context/` にアクセスして確認。

## Base Package の VPM リスティングに統合する場合

Base (`AnimatorController_Context`) の `Website/source.json` に MA パッケージも追加:

```json
{
  "name": "AnimatorController Tools",
  "id": "com.mame8.vpm",
  "url": "https://mametsubu8.github.io/AnimatorController_Context/vpm.json",
  "author": "mame8",
  "packages": {
    "com.mame8.animator-controller-context": {
      "versions": {}
    },
    "com.mame8.animator-controller-ma-context": {
      "versions": {}
    }
  }
}
```

Base の `build-listing.yml` も MA リポのリリースを取得するよう拡張が必要。
これにより、ユーザーは VCC に1つのソースを追加するだけで両パッケージが見える。

## 日常の開発フロー

```
コード修正 → テスト → コミット → プッシュ
  バージョンアップ時:
    package.json の version 更新 → コミット → タグ push → 自動リリース
```

## 注意事項

- `GIT_WORK_TREE` を指定しないと `git add` がルートの `.gitignore` 等を拾う
- commit / log / diff は `GIT_WORK_TREE` 不要（Base の CLAUDE.md に記載あり）
- ルートの `.gitignore` は Unity プロジェクト用（git 管理対象外）
