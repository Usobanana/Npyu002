# Unity + Claude MCP 操作可能範囲と制限事項

> 作成日: 2026-04-04
> 動作確認環境: Unity 6.4 (6000.4.1f1) / mcp-unity v1.2.0

実際の開発で判明した「できること / できないこと / 回避策」のノウハウ集。

---

## ✅ Claude から直接できる操作

### シーン操作
| 操作 | ツール | 備考 |
|---|---|---|
| シーン作成 | `create_scene` | プレイモード中は不可 |
| シーン保存 | `save_scene` | |
| シーン情報取得 | `get_scene_info` | |
| GameObject 作成 | `execute_menu_item` | `"GameObject/3D Object/Cube"` など |
| Empty Object 作成 | `execute_menu_item` | `"GameObject/Create Empty"` |
| 名前変更・Tag 変更 | `update_gameobject` | layer は数値で指定可 |
| 位置・回転・スケール | `set_transform` / `move_gameobject` など | |

### コンポーネント操作
| 操作 | ツール | 備考 |
|---|---|---|
| コンポーネント追加 | `update_component` | componentName に型名を指定 |
| 数値フィールドの設定 | `update_component` | float, int, bool, Vector3 等 |
| 文字列・Enum フィールド | `update_component` | |
| **オブジェクト参照 (Transform / GameObject)** | `update_component` | ❌ 文字列パスでは設定不可（後述） |
| **LayerMask フィールド** | `update_component` | ❌ 整数変換不可（後述） |

### アセット操作
| 操作 | ツール | 備考 |
|---|---|---|
| マテリアル作成 | `create_material` | `name` + `savePath` 必須 |
| マテリアル割り当て | `assign_material` | `objectPath` + `materialPath` |
| Prefab 作成 | `create_prefab` | |

### スクリプト操作
| 操作 | 備考 |
|---|---|
| C# スクリプト作成・編集 | Write / Edit ツールでファイルを直接作成 |
| コンパイル | `recompile_scripts` |
| カスタム MenuItem 実行 | `execute_menu_item` でスクリプトの [MenuItem] を呼べる |

### ファイル操作
| 操作 | 備考 |
|---|---|
| ProjectSettings 直接編集 | `TagManager.asset` などを Read/Edit で変更可能 |
| Layer 追加 | `TagManager.asset` を直接編集して追加 → Unity が自動認識 |

---

## ❌ 制限事項と回避策

### 1. オブジェクト参照フィールドへの代入（Transform[], GameObject 等）

**制限:** `update_component` でオブジェクト参照を文字列パスで設定しようとするとエラー

```
Error converting value "PatrolPoint1" to type 'UnityEngine.Transform'
```

**回避策A（推奨）: コードで自動検索**
```csharp
// Inspector アサインの代わりにタグや名前で自動取得
void Start()
{
    patrolPoints = GameObject.FindGameObjectsWithTag("PatrolPoint")
        .Select(g => g.transform).ToArray();
}
```

**回避策B:** Inspector での手動ドラッグ＆ドロップ

---

### 2. LayerMask フィールドへの整数代入

**制限:** `{"enemyLayer": 64}` のような整数では LayerMask 型に変換できない

```
Error converting value 64 to type 'UnityEngine.LayerMask'
```

**回避策A（推奨）: コードで Layer 名から取得**
```csharp
void Start()
{
    enemyLayer = LayerMask.GetMask("Enemy");
}
```

**回避策B:** LayerMask フィールドをやめて tag 判定にする
```csharp
// OverlapSphere 後にタグでフィルタ
if (hit.CompareTag("Enemy")) { ... }
```

---

### 3. NavMesh ベイク

**制限:**
- Unity 6 では `StaticEditorFlags.NavigationStatic` が削除済み
- `UnityEditor.AI.NavMeshBuilder.BuildNavMesh()` も非推奨
- MenuItem スクリプトでコンパイルエラーが発生するとメニューが登録されない

**回避策A: NavMeshSurface コンポーネントを MCP で追加し、手動でベイク**
```
Claude が実行:
update_component("Ground", "NavMeshSurface")

ユーザーが実行:
Inspector の NavMeshSurface > Bake ボタン
```

**回避策B: MenuItem スクリプトを NavMeshSurface 使用で作成**
```csharp
using Unity.AI.Navigation;

[MenuItem("Tools/Bake NavMesh")]
public static void Bake()
{
    var surface = GameObject.Find("Ground").GetComponent<NavMeshSurface>();
    surface.BuildNavMesh();
}
```
> ⚠️ コンパイルエラーが残っている状態では MenuItem が登録されない。
> エラー解消後に Unity を再起動すると確実に反映される。

---

### 4. プレイモード中の操作

**制限:** シーン作成・コンポーネント追加など多くの操作がプレイモード中は不可

**対応:** `execute_menu_item("Edit/Play")` でプレイモードをトグル可能

---

## 🔁 バッチ実行のすすめ

複数操作は `batch_execute` でまとめると効率的（10〜100x 高速）:

```json
{
  "operations": [
    {"tool": "update_gameobject", "params": {"objectPath": "Enemy", "gameObjectData": {"layer": 6}}},
    {"tool": "update_component", "params": {"objectPath": "Enemy", "componentName": "Health", "componentData": {"maxHP": 100}}}
  ]
}
```

---

## 📋 まとめ: Inspector 手動作業が必要なケース

| 作業 | 回避策の有無 |
|---|---|
| Transform[] / GameObject[] の参照アサイン | ✅ コードで `Find` / `FindGameObjectsWithTag` |
| LayerMask フィールド | ✅ コードで `LayerMask.GetMask()` |
| NavMesh ベイク（ボタン押下） | ⚠️ NavMeshSurface 追加は MCP、Bake のみ手動 |
| Animator Controller のワイヤリング | ❌ 現状は手動 |
| UI の参照アサイン（ Canvas / Slider 等） | ❌ 現状は手動（コードで `FindObjectOfType` で回避可） |
