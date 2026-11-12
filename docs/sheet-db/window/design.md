# window 設計

## アーキテクチャ

```
SheetDbWindow（EditorWindow）
 ├─ TablePane      … TreeView（ブック → テーブル）
 ├─ RowPane        … MultiColumnListView（itemsSource = 表示する行の番号の int[]）
 ├─ DetailPane     … ScrollView（参照先・参照元のボタン）
 ├─ ProblemPane    … ListView
 └─ StatusBar
SheetDbLoader.Changed → Database を受け取り、各ペインを作り直す
```

- **列はテーブルを選ぶたびに作り直す。**`bindCell` は `ColumnData.Display[row]` の文字列を `Label.text` に入れるだけにし、文字列を作らない（数値の書き方は schema が読み込みのときに作ってある）
- 行の一覧の `itemsSource` は「表示する行の番号」の配列。並べ替え・検索は、この配列を差し替えるだけで済む
- 選択などの状態は `[SerializeField]` でウィンドウに持ち、ドメインリロードを越える

## インターフェース

```csharp
public sealed class SheetDbWindow : EditorWindow
{
    [MenuItem("Tools/Obelune/Sheet DB")] static void Open();
    public void Reveal(string table, int row);   // 移動。fixtures の計測からも呼ぶ
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| loader | Database と変更の通知 |
| index | 検索・参照 |
| UI Toolkit | 画面 |
