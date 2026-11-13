# source タスク

## 実装タスク一覧

- [ ] `IDataViewerSource` と `AssetSource`
- [ ] 読み直しの流れと世代番号
- [ ] 変更の検出
- [ ] 計測

## テストの線引き

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| すべて | **書ける**（テスト用のソースとアセットを作る） | `Obelune.DataViewer.Editor.Tests` |

## 依存関係

- extract・index → 読み直しの流れ
