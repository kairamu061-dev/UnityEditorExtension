# window 設計

## アーキテクチャ

```
DataViewerWindow（EditorWindow）
 ├─ TablePane      … ListView（テーブル）
 ├─ RowPane        … MultiColumnListView（itemsSource = 表示する行の番号の int[]）
 ├─ DetailPane     … ScrollView（参照先・参照元のボタン）
 ├─ ProblemPane    … ListView
 ├─ SettingsPane   … settings の画面
 └─ StatusBar
DataViewerLoader.Changed → Database を受け取り、各ペインを作り直す
```

- **列はテーブルを選ぶたびに作り直す。**`bindCell` は `ColumnSnapshot.Display[row]` の文字列を `Label.text` に入れるだけにし、文字列を作らない（表示の文字列は extract が作ってある）
- 行の一覧の `itemsSource` は「表示する行の番号」の配列。並べ替え・検索は、この配列を差し替えるだけで済む
- 並べ替えの順は列ごとに一度だけ計算し、ウィンドウの側に覚えておく（Database には書かない）
- 選択などの状態は `[SerializeField]` でウィンドウに持ち、ドメインリロードを越える。テーブルはパスで覚える

## インターフェース

```csharp
public sealed class DataViewerWindow : EditorWindow
{
    [MenuItem("Tools/Obelune/Data Viewer")] static void Open();
    public void Reveal(string tablePath, int row);   // 移動。fixtures の計測からも呼ぶ
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| source | Database と変更の通知 |
| index | 検索・参照 |
| settings | 設定の画面 |
| UI Toolkit | 画面 |
