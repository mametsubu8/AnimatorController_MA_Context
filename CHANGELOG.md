# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-04-28

### Added

- アバター構成テキスト出力（11セクション）
  - Avatar Descriptor（View Position、Lip Sync、Eye Look、Playable Layers）
  - Expression Parameters（同期コスト計算付き）
  - Expression Menu（再帰的メニューツリー、循環参照防止）
  - Hierarchy（GameObject ツリー、コンポーネント注釈、inactive 表示）
  - Modular Avatar コンポーネント（13種の専用シリアライズ）
  - VRC PhysBone / PhysBoneCollider
  - VRC Contact Sender / Receiver
  - SkinnedMeshRenderer（メッシュ、BlendShape、マテリアル）
  - AnimatorController（AnimatorController Context に委譲）
- EditorWindow（9つのセクション選択トグル、クリップボード/ファイル出力）
- SerializedProperty ベースの型非依存設計（VRC SDK / MA 未導入でもコンパイル可能）
- 条件付きコンパイル（`AMAC_VRC_SDK`、`AMAC_MODULAR_AVATAR`）
- MA コンポーネント 13種の専用シリアライズ
  - MAMergeAnimator, MAParameters, MAMenuItem, MAMenuInstaller
  - MABlendshapeSync, MABoneProxy, MAObjectToggle, MAMeshSettings
  - MAScaleAdjuster, MAMergeBlendTree, MAWorldFixedObject
  - MAVisibleHeadAccessory, MAMenuGroup
- 未知コンポーネントの汎用フォールバックシリアライズ
- プロパティ名バリアント対応（MA バージョン差異吸収）
