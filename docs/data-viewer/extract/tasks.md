# extract タスク

## 実装タスク一覧

- [ ] `SerializedFieldPolicy`（Editor）
- [ ] テーブルの発見・名前・識別
- [ ] 列の型ごとの取り出し（式木）
- [ ] 入れ子・配列・その他の型
- [ ] 文字列表と表示・検索の派生データ

## テストの線引き

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| 発見・取り出し・派生データ | **書ける**（テスト用の型を作る） | `Obelune.DataViewer.Core.Tests`（`SerializedFieldPolicy` は Editor.Tests） |
| 速さ | **書ける** | fixtures I-02 |

## 依存関係

- 発見 → 取り出し → 派生データ
