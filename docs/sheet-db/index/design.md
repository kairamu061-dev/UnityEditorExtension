# index 設計

## データ構造

```csharp
public sealed class Database
{
    public IReadOnlyList<BookData> Books;
    public IReadOnlyList<TableData> Tables;          // テーブル名の重複を除いたもの
    public int FindTable(string name);               // Dictionary<string,int>
    public int FindRow(int table, in KeyValue key);  // 主キーの索引
    public RefColumn GetRef(int table, int column);  // 解決済みの参照
    public RefSourceList GetSources(int table, int row);
    public IReadOnlyList<Problem> Problems;          // 全ブックの問題＋テーブルをまたぐ問題
}

// 参照の解決結果。列ごとに行の数だけ
public sealed class RefColumn
{
    public int TargetTable;     // -1 = 参照先テーブルがない
    public int[] TargetRow;     // -1 = 参照切れ、-2 = 参照なし（空）
}
```

### 主キーの索引

- `int` の主キー: `Dictionary<long, int>`。`string` の主キー: `Dictionary<string, int>`（`StringComparer.Ordinal`）
- テーブルごとに作る。ブックを差し替えたときは、そのブックのテーブルの索引だけを作り直す

### 参照元の逆引き

- 参照先のテーブルごとに、CSR 形式（`offsets[row]..offsets[row+1]` の範囲に、参照元の（テーブル, 列, 行）を詰めた配列）で持つ
- 作るのは、全テーブルの参照の列を 1 回なめるだけ（数える → 位置を決める → 詰める）。ブックを差し替えたときは、参照先のテーブルを問わず作り直す（**推測**: 10 万行・参照の列が 1 テーブルに 2〜3 本なら数十 ms。fixtures I-06 で測る）

### 検索

- 文字列はブックごとの StringTable にまとまっている。**行をなめる前に、StringTable の側を探す**: 小文字にした文字列の写しを StringTable ごとに 1 回だけ作り、検索語を含む文字列の番号の集合（ビット列）を得る。そのあと、各列の `Strings[]` がその集合に入っている行を集める（整数の比較だけ）
- 数値の列は、検索で初めて使うときに `string` の書き方にした値を StringTable に加え、以後は文字列と同じに扱う
- 絞り込み: 新しい検索語が直前の検索語を含むとき、直前に当たった行だけを調べ直す
- 検索は `CancellationToken` を受け取る。打鍵が続いたら、前の検索を途中でやめる

## インターフェース

```csharp
public static class DatabaseBuilder
{
    public static Database Build(IReadOnlyList<BookData> books);
    public static Database ReplaceBook(Database current, BookData newBook);   // 新しい Database を返す
    public static Database RemoveBook(Database current, string bookPath);
}

public sealed class Searcher
{
    public Searcher(Database db);
    public SearchResult Search(string query, int tableOrAll, SearchResult previous, CancellationToken ct);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| schema | `BookData`・`TableData` |
