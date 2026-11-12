# sheet-db タスク

個々のタスクはサブ項目にある。ここには順序と、サブ項目をまたぐタスクだけを置く。

## 実装タスク一覧

- [ ] パッケージの骨組み: `UnityProject/Packages/com.obelune.sheet-db/`（`package.json`・`README.md`・`CHANGELOG.md`・4 つの asmdef。`design.md` のアセンブリの表）
  - 骨組みができたらすぐ、空のテストを 1 つ置き、`tests | where fullName starts-with "Obelune.SheetDb"` で見つかるかを測る。`docs/testing.md` で測ったのは `Assets/` の asmdef だけで、埋め込みパッケージの中のテストが見つかるかはまだ測っていない。**推測**: 見つからなければ `manifest.json` に `"testables": ["com.obelune.sheet-db"]` が要る。結果を `docs/testing.md` に書く
- [ ] [fixtures](./fixtures/tasks.md) の生成器
- [ ] [xlsx-reader](./xlsx-reader/tasks.md)
- [ ] [schema](./schema/tasks.md)
- [ ] [index](./index/tasks.md)
- [ ] [cache](./cache/tasks.md)
- [ ] [loader](./loader/tasks.md)
- [ ] [window](./window/tasks.md)
- [ ] [fixtures](./fixtures/tasks.md) の計測をすべて通し、値を `dev-notes.md` に残す
- [ ] 実物の Excel での確認（`test-cases.md` の E-01）

## テストの線引き

全体の線は `docs/testing.md`。この機能では次のとおり。

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| xlsx-reader・schema・index・cache（`Obelune.SheetDb.Core`） | **書ける**（純粋な処理） | `Obelune.SheetDb.Core.Tests`（MCP 経由） |
| loader・window の振る舞い・性能の目標 | **書ける**（エディタの中で） | `Obelune.SheetDb.Editor.Tests`（MCP 経由） |
| 見た目・操作の手触り・実物の Excel との往復 | 実機が要る | 各 `test-cases.md` の E 系 |

## 依存関係

- パッケージの骨組み → すべて
- fixtures の生成器 → xlsx-reader の速さの計測
- xlsx-reader → schema → index・cache → loader → window
- schema のデータ構造が決まれば、index と cache は並行して進められる
