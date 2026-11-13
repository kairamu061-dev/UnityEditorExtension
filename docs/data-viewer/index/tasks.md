# index タスク

## 実装タスク一覧

- [ ] Database と主キーの索引
- [ ] 参照の解決・参照切れ・参照なしの値
- [ ] 参照元の逆引き
- [ ] 検索と絞り込み

## テストの線引き

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| すべての機能 | **書ける**（Snapshot を直接組み立てる） | `Obelune.DataViewer.Core.Tests` |
| 速さ | **書ける**（fixtures の規模のデータで計測） | fixtures I-03・I-05・I-06 |

## 依存関係

- extract の Snapshot・settings のデータ構造 → この項目
