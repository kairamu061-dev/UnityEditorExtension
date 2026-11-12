# schema タスク

## 実装タスク一覧

- [ ] `TableData`・`ColumnData`・`StringTable`・`Problem` の型
- [ ] 見出しの解釈と、読まないシート・列
- [ ] 型の変換と問題の記録

## テストの線引き

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| すべて | **書ける**（セルの並びを直接与える。xlsx も要らない） | `Obelune.SheetDb.Core.Tests` |

## 依存関係

- xlsx-reader の `XlsxCell` → この項目（ただしテストはセルを直接与えるので、xlsx-reader の完成を待たない）
