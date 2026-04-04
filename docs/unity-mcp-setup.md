# Unity + Claude MCP 開発環境構築ガイド

> 調査日: 2026-04-04

## 概要

Unity Editor と Claude Code を MCP (Model Context Protocol) 経由で接続し、AI によって Unity 開発を自動化・支援する環境の構築手法をまとめる。

アーキテクチャの基本構造は以下のとおり:

```
Claude Code ←→ MCP Server ←→ Unity Editor Plugin (Local Server)
```

---

## 主要な選択肢

### 1. 公式パッケージ: `com.unity.ai.assistant` (推奨: Unity 6 ユーザー)

| 項目 | 詳細 |
|---|---|
| 提供元 | Unity Technologies (公式) |
| Unity バージョン | Unity 6 (6000.0+) |
| 追加依存 | なし (relay binary が自動インストール) |
| 通信方式 | IPC / stdio (relay binary 経由) |
| 現状 | pre-release (v2.x.x-pre) |

**特徴:**
- 依存関係なしで最もシンプルなセットアップ
- Unity 公式メンテナンス
- Edit > Project Settings > AI > Unity MCP で設定
- Relay binary: `~/.unity/relay/` に自動インストール

**公式ドキュメント:**
- https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-overview.html
- https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-get-started.html

---

### 2. CoderGamester/mcp-unity (推奨: Unity 6, Node.js ベース)

| 項目 | 詳細 |
|---|---|
| 提供元 | Community (OSS / MIT) |
| Unity バージョン | Unity 6+ |
| 追加依存 | Node.js 18+, npm 9+ |
| 通信方式 | WebSocket (default port 8090) |
| ツール数 | 35+ |

**提供ツール (主要なもの):**
- Scene / GameObject: 作成・ロード・保存・削除・移動・回転・スケール・親子関係変更
- Component 操作: フィールド更新・追加・削除
- Prefab / Asset: Prefab 作成・AssetDatabase からの追加
- MenuItem 実行: 任意の MenuItem 関数をトリガー
- Test Runner: Unity テスト実行
- Material: 作成・割り当て・変更
- Package Manager: パッケージインストール
- バッチ実行: 複数操作のアトミック実行
- Console ログアクセス

**Claude Code 設定 (`~/.claude/settings.json`):**
```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["ABSOLUTE/PATH/TO/mcp-unity/Server~/build/index.js"]
    }
  }
}
```

**GitHub:** https://github.com/CoderGamester/mcp-unity

---

### 3. IvanMurzak/Unity-MCP (推奨: 全 Unity バージョン対応 / Runtime 操作が必要な場合)

| 項目 | 詳細 |
|---|---|
| 提供元 | Community (OSS) |
| Unity バージョン | 全バージョン対応 |
| 追加依存 | CLI ツール (`unity-mcp-cli`) or 手動インストール |
| 通信方式 | stdio または HTTP |
| ツール数 | 100+ |

**特徴:**
- **Runtime 対応**: コンパイル済みゲーム内での AI 操作 (NPC 制御・インゲームデバッグ)
- Roslyn ベースの C# コンパイルと実行 (runtime)
- リフレクションによる任意 C# メソッド呼び出し (private メソッドも対応)
- 1行のコードで任意の C# メソッドを MCP ツールとして公開可能
- Claude Code, Gemini, Copilot, Cursor, Windsurf 対応

**GitHub:** https://github.com/IvanMurzak/Unity-MCP

---

### 4. CoplayDev/unity-mcp (推奨: Unity 2021.3 LTS+ / ツール数最多)

| 項目 | 詳細 |
|---|---|
| 提供元 | Community (Coplay Platform) |
| Unity バージョン | 2021.3 LTS+ |
| 追加依存 | Python 3.10+, uv パッケージマネージャー, Node.js |
| 通信方式 | HTTP (localhost:8080) |
| ツール数 | 86+ (最多) |

**特徴:**
- Unity 専用にチューニングされたシステムプロンプト
- マルチ Unity インスタンス対応
- Roslyn スクリプト検証 (型チェック)
- バッチ実行で個別呼び出しより 10〜100 倍高速
- 40+ カテゴリ: physics, animation, graphics, build, profiling, UI, VFX 等
- リアルタイムなエディタ UI からのツール切り替え

**ドキュメント:** https://docs.coplay.dev/coplay-mcp/claude-code-guide
**GitHub:** https://github.com/CoplayDev/unity-mcp

---

## 選択指針

```
Unity 6 を使う
├─ 依存関係をゼロにしたい → 公式 com.unity.ai.assistant
└─ Node.js が使える / ツールが充実している方が良い → CoderGamester/mcp-unity

Unity 2021〜2022 LTS を使う
├─ ツール数を最大化したい → CoplayDev/unity-mcp (Python 必要)
└─ Runtime での AI 操作が必要 → IvanMurzak/Unity-MCP
```

**今回のプロジェクト推奨:**
- Unity 6 を使用するなら **CoderGamester/mcp-unity** (実績あり・ドキュメント充実・Node.js のみ)
- それ以前のバージョンなら **IvanMurzak/Unity-MCP** (全バージョン対応・拡張性高い)

---

## 可能な操作 / 制限事項

### 可能な操作
- GameObject の作成・変更・削除・親子関係変更
- Component の追加・削除・設定
- Scene の作成・ロード・保存
- C# スクリプトの作成・編集・コンパイル
- Material / Prefab / Asset の作成・管理
- Package Manager によるパッケージインストール
- Test Runner の実行・結果取得
- Console ログの読み取り
- 任意の MenuItem 関数の呼び出し

### 現時点の制限
- ビジュアル品質の主観的判断はできない (スクリーンショット経由のみ)
- Unity Inspector / EditorWindow へのリアルタイム直接接続は未対応
- 大気感・ライティングの「雰囲気」など高レベルなデザイン判断は人間のレビューが必要
- 完全自律実行は技術タスクに限定; クリエイティブ方向性は人間主導が現実的

---

## セットアップ手順 (CoderGamester/mcp-unity を選択した場合)

### 前提条件
- Unity 6 インストール済み
- Node.js 18+ / npm 9+ インストール済み
- Claude Code インストール済み

### 手順

1. **Unity Package Manager でパッケージ追加**
   ```
   Window > Package Manager > Add package from git URL
   https://github.com/CoderGamester/mcp-unity.git
   ```

2. **MCP Unity サーバーのビルド**
   ```bash
   cd <mcp-unity package path>/Server~
   npm install
   npm run build
   ```

3. **Unity エディタでサーバー起動**
   ```
   Tools > MCP Unity > Server Window > Start Server
   ```

4. **Claude Code の MCP 設定**
   ```bash
   claude mcp add-json "mcp-unity" '{"command":"node","args":["ABSOLUTE/PATH/TO/mcp-unity/Server~/build/index.js"]}'
   ```
   または `~/.claude/settings.json` に直接記述。

5. **接続確認**
   ```bash
   claude mcp list
   ```

---

## セットアップ手順 (公式 com.unity.ai.assistant を選択した場合)

### 前提条件
- Unity 6 (6000.0+)

### 手順

1. **パッケージインストール**
   - Package Manager > Unity Registry で `AI Assistant` を検索してインストール

2. **MCP 設定確認**
   ```
   Edit > Project Settings > AI > Unity MCP
   → ステータスが "Running" になっていることを確認
   ```
   relay binary が `~/.unity/relay/` に自動インストールされる。

3. **Claude Code の MCP 設定**
   - Windows の場合: `%APPDATA%\.claude\settings.json` に relay binary のパスを設定
   ```json
   {
     "mcpServers": {
       "unity": {
         "command": "C:/Users/<USER>/.unity/relay/relay_win_x64.exe",
         "args": ["--mcp"]
       }
     }
   }
   ```

4. **初回接続の承認**
   - Unity の MCP 設定パネルで "Pending" 接続リクエストを承認

---

## 参考リンク

- [Unity MCP 公式ドキュメント](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-overview.html)
- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity)
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
- [Coplay Claude Code ガイド](https://docs.coplay.dev/coplay-mcp/claude-code-guide)
- [Unity Asset Store: MCP for Unity](https://assetstore.unity.com/packages/tools/generative-ai/mcp-for-unity-ai-driven-development-329908)
