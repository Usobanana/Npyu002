# Unity + Claude MCP 接続 クイックスタートガイド

> 作成日: 2026-04-04
> 動作確認環境: Unity 6.4 (6000.4.1f1) / Claude.ai デスクトップアプリ / Windows 11

## 構成

```
Claude.ai デスクトップアプリ ←→ Node.js MCP Server ←→ Unity Editor (WebSocket :8090)
```

使用パッケージ: [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) v1.2.0

---

## 前提条件

- Unity 6 インストール済み
- Node.js 18+ / npm 9+ インストール済み
- Claude.ai デスクトップアプリ インストール済み

---

## 手順

### 1. mcp-unity リポジトリをクローン & ビルド

```bash
cd C:/GitHub
git clone https://github.com/CoderGamester/mcp-unity.git
cd mcp-unity/Server~
npm install
npm run build
```

ビルド成功すると `Server~/build/index.js` が生成される。

### 2. Unity パッケージを追加

`Packages/manifest.json` の `dependencies` 先頭に追加:

```json
{
  "dependencies": {
    "com.gamelovers.mcp-unity": "https://github.com/CoderGamester/mcp-unity.git",
    ...
  }
}
```

> **注意:** パッケージ名は `com.gamelovers.mcp-unity`（`com.cogamester` ではない）

Unity を開くと Package Manager が自動でインストールする。

### 3. Claude.ai デスクトップアプリの MCP 設定

`%APPDATA%\Claude\claude_desktop_config.json` に `mcpServers` を追加:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["C:/GitHub/mcp-unity/Server~/build/index.js"]
    }
  }
}
```

> **注意:** `~/.claude/settings.json` はClaude Code CLI 用。
> デスクトップアプリには `claude_desktop_config.json` が必要。

### 4. Unity でサーバーを起動

```
Tools > MCP Unity > Server Window > Start Server
```

Status が **Server Online** になることを確認。

### 5. Claude.ai デスクトップアプリを再起動

再起動後、`mcp__mcp-unity__*` ツール群が使用可能になる。

---

## 動作確認

Claude に以下を依頼して確認:

```
現在の Unity シーンの情報を取得して
```

`get_scene_info` ツールが呼ばれ、シーン名・オブジェクト数が返れば成功。

---

## 利用可能な主なツール

| ツール | 説明 |
|---|---|
| `get_scene_info` | シーン情報取得 |
| `get_gameobject` | GameObject 情報取得 |
| `update_gameobject` | GameObject 作成・更新 |
| `execute_menu_item` | メニュー項目の実行 |
| `create_scene` | シーン作成 |
| `load_scene` / `save_scene` | シーンのロード・保存 |
| `update_component` | コンポーネントのフィールド更新 |
| `create_material` | マテリアル作成 |
| `run_tests` | テスト実行 |
| `get_console_logs` | Console ログ取得 |

---

## トラブルシューティング

### "does not match the name" エラー
manifest.json のパッケージ名を `com.gamelovers.mcp-unity` に修正する。

### "Cannot find Claude Desktop config file" エラー
Unity Console に表示されるが無視して OK。
`claude_desktop_config.json` に `mcpServers` を設定し、デスクトップアプリを再起動することで解消する。

### コネクタに mcp-unity が表示されない
デスクトップアプリをタスクトレイも含めて完全終了してから再起動する。
