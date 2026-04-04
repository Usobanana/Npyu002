# 申し送り事項 / Session Handoff

## プロジェクト概要
- **リポジトリ**: `C:\GitHub\Npyu002`
- **Unity プロジェクト**: `Npyu002Unity/`
- **WebGL ビルド出力**: `docs/`（GitHub Pages で公開済み）
- **namespace**: `ActionGame`

---

## 現在の状態（2026-04-04）

### 動作確認済み
- プレイヤー移動・ジャンプ・攻撃（PC / モバイル対応）
- エネミー AI（BT: 巡回 → 追跡 → 攻撃）× 3体
- HP バー、スコア、BGM / SE
- HIT / 死亡パーティクルエフェクト（マテリアル設定済み）
- GAME OVER / WIN 画面
- X Bot キャラクター（Mixamo）+ Animator Controller

### シーン構成
- `Assets/Scenes/ActionGame.unity` — メインゲーム
- `Assets/MainMenu.unity` — メインメニュー
- Build Settings: MainMenu(0) → ActionGame(1)

---

## 直近の作業ログ

| 日付 | 内容 |
|------|------|
| 2026-04-04 | パーティクルマゼンタ修正（Particles/Standard Unlit マテリアル作成） |
| 2026-04-04 | エネミー攻撃時の SpawnHit 追加 |
| 2026-04-04 | X Bot (Mixamo) キャラクター導入 |
| 2026-04-04 | PlayerAnimator.controller 作成（Idle/Run/Attack/Jump/HitReact/Death） |
| 2026-04-04 | Run・Idle の Loop Time 設定 |

---

## 次にやること（TODO）

- [ ] エネミーにも 3D キャラクターを適用（現状カプセル）
- [ ] Attack アニメーションのタイミングと実際のダメージ判定を合わせる
- [ ] WebGL 再ビルド → GitHub Pages にプッシュ
- [ ] MainMenu シーンの見た目改善

---

## 重要ファイル一覧

| パス | 役割 |
|------|------|
| `Assets/Scripts/Player/PlayerController.cs` | 移動・カメラ・ジャンプ |
| `Assets/Scripts/Player/PlayerCombat.cs` | 攻撃判定・OnAttack イベント |
| `Assets/Scripts/Player/PlayerAnimationController.cs` | Animator 駆動 |
| `Assets/Scripts/Enemy/EnemyBT.cs` | BT ベース AI |
| `Assets/Scripts/UI/GameManager.cs` | 勝敗管理（複数エネミー対応） |
| `Assets/Scripts/UI/GameUI.cs` | HUD・パネル制御 |
| `Assets/Scripts/Combat/EffectManager.cs` | パーティクル管理 |
| `Assets/Scripts/Combat/Health.cs` | HP 管理（OnDeath / OnHealthChanged） |
| `Assets/Scripts/Editor/UISetupTool.cs` | Canvas 一括セットアップ |
| `Assets/Scripts/Editor/CharacterSetupTool.cs` | キャラクター・アニメ設定 |
| `Assets/Scripts/Editor/PolishSetupTool.cs` | パーティクル Prefab 生成 |
| `Assets/Scripts/Editor/AudioGeneratorTool.cs` | 手続き的 SE 生成 |
| `Assets/Animations/PlayerAnimator.controller` | Player Animator |
| `Assets/Characters/X Bot.fbx` | Mixamo キャラクター |
| `Assets/Characters/Animations/` | 6 アニメーション FBX |

---

## MCP / ビルドの制限事項

- **再生・停止**: MCP から不可 → Unity エディタで手動
- **ビルド**: MCP からタイムアウト → `File > Build Settings > Build` で手動
- **WebGL 設定**: `webGLCompressionFormat: 2`（Disabled）設定済み
- **Assets/Refresh** を新規ファイル追加後・recompile 前に必ず実行すること

---

## Tools メニュー（Editor スクリプト）

| メニュー | 説明 |
|----------|------|
| `Tools/ActionGame/Setup UI` | Canvas / HUD 一括生成 |
| `Tools/ActionGame/Fix EventSystem Input Module` | InputSystemUIInputModule に差し替え |
| `Tools/ActionGame/Wire GameManager` | GameManager の HP 参照をワイヤリング |
| `Tools/ActionGame/Setup Polish Effects` | パーティクル Prefab + マテリアル生成 |
| `Tools/ActionGame/Setup Animator Controller` | PlayerAnimator.controller 再生成 |
| `Tools/ActionGame/Setup Animation Imports` | アニメ FBX を Humanoid に一括設定 |
| `Tools/ActionGame/Setup Player Character` | X Bot 配置・Animator 設定 |
| `Tools/ActionGame/Generate Audio Clips` | 手続き的 SE 生成 |
