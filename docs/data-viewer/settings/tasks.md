# settings タスク

## 実装タスク一覧

- [ ] 設定のデータと保存
- [ ] 食い違いの検出（`SettingsValidator`）
- [ ] 設定の画面（window の中のペイン）

## テストの線引き

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| 保存の往復・食い違いの検出 | **書ける** | `Obelune.DataViewer.Core.Tests`・`Editor.Tests` |
| 設定の画面 | 実機が要る（操作の流れを人が見る） | E-01 |

## 依存関係

- extract の Snapshot → 食い違いの検出・設定の画面
