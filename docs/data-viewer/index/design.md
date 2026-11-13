# index 設計

## データ構造

```csharp
public sealed class Database
{
    public Snapshot Snapshot;
    public int FindTable(string path);
    public int FindRow(int table, in KeyValue key);       // 主キーの索引
    public RefColumn GetRef(int table, int column);       // 解決済みの参照（参照の列だけ）
    public RefSourceList GetSources(int table, int row);
    public IReadOnlyList<Problem> Problems;               // extract・settings・index の問題をまとめたもの
}

public sealed class RefColumn
{
    public int TargetTable;     // -1 = 解決できない
    public int[] TargetRow;     // -1 = 参照切れ、-2 = 参照なし
}
```

### 主キーの索引

- 整数・`enum` の主キー: `Dictionary<long, int>`。`string` の主キー: `Dictionary<int, int>`（文字列表の番号で引く。文字列表は Snapshot 全体で 1 つなので、同じ文字列は同じ番号）

### 参照元の逆引き

- 参照先のテーブルごとに、CSR 形式（`offsets[row]..offsets[row+1]` の範囲に、参照元の（テーブル, 列, 行）を詰めた配列）で持つ。全部の参照の列を 1 回なめて作る（数える → 位置を決める → 詰める）

### 検索

- **行をなめる前に、文字列表の側を探す**: 文字列表の小文字の写し（`Lowered`。extract が作ってある）から、検索語を含む文字列の番号の集合（ビット列）を得る。そのあと、各列の `Display[]` がその集合に入っている行を集める（整数の比較だけ）
- 絞り込み: 新しい検索語が直前の検索語を含むとき、直前に当たった行だけを調べ直す
- `CancellationToken` を受け取る。打鍵が続いたら、前の検索を途中でやめる
- Searcher は Database を読むだけで、何も書き足さない

## インターフェース

```csharp
public static class DatabaseBuilder
{
    public static Database Build(Snapshot snapshot, DataViewerSettings settings);   // スレッドプールで呼ぶ
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
| extract | `Snapshot` |
| settings | 主キーと参照の設定・食い違いの問題 |
