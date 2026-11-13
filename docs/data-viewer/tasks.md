# data-viewer タスク

個々のタスクはサブ項目にある。ここには順序と、サブ項目をまたぐタスクだけを置く。

## 実装タスク一覧

- [ ] パッケージの骨組み: `UnityProject/Packages/com.obelune.data-viewer/`（`package.json`・`README.md`・`CHANGELOG.md`・4 つの asmdef。`design.md` のアセンブリの表）
  - 骨組みができたらすぐ、空のテストを 1 つ置き、`tests | where fullName starts-with "Obelune.DataViewer"` で見つかるかを測る。`docs/testing.md` で測ったのは `Assets/` の asmdef だけ。**推測**: 見つからなければ `manifest.json` に `"testables": ["com.obelune.data-viewer"]` が要る。結果を `docs/testing.md` に書く
- [ ] [fixtures](./fixtures/tasks.md) の生成器（例の DataContainer の形の型と、10 万行のアセット）
- [ ] [extract](./extract/tasks.md)
- [ ] [settings](./settings/tasks.md) の保存と食い違いの検出
- [ ] [index](./index/tasks.md)
- [ ] [source](./source/tasks.md)
- [ ] [window](./window/tasks.md)（設定の画面を含む）
- [ ] [fixtures](./fixtures/tasks.md) の計測をすべて通し、値を `dev-notes.md` に残す

## テストの線引き

全体の線は `docs/testing.md`。この機能では次のとおり。

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| 取り出し・index（`Obelune.DataViewer.Core`） | **書ける**（テスト用の型を直接作る） | `Obelune.DataViewer.Core.Tests`（MCP 経由） |
| たどるフィールドの判定・source・settings・性能の目標 | **書ける**（エディタの中で） | `Obelune.DataViewer.Editor.Tests`（MCP 経由） |
| 見た目・操作の手触り | 実機が要る | 各 `test-cases.md` の E 系 |

## 依存関係

- パッケージの骨組み → すべて
- fixtures の生成器 → extract の速さの計測
- extract → index → window
- settings のデータ構造 → index
- source → window
