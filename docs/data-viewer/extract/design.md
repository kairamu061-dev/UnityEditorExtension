# extract 設計

## データ構造

```csharp
public enum ColumnKind { Integer, Real, Bool, String, Enum, Count, Text }

public sealed class ColumnSnapshot
{
    public string Name;            // 入れ子は "stats.hp"
    public ColumnKind Kind;
    public Type ClrType;
    public long[] Integers;        // Integer・Enum・Count・Bool（0/1）
    public double[] Reals;
    public int[] Strings;          // String・Text の値（文字列表の番号）
    public BitSet HasValue;
    public int[] Display;          // 全部の列。表示と検索に使う文字列の番号
}

public sealed class TableSnapshot
{
    public string Path;            // "characterData.data"（識別。設定はこれで書く）
    public string Name;            // "Character"
    public Type RowType;
    public int RowCount;
    public ColumnSnapshot[] Columns;
}

public sealed class Snapshot
{
    public TableSnapshot[] Tables;
    public StringTable Strings;     // 全テーブルで 1 つ。Lowered（小文字の写し）を持つ
    public Problem[] Problems;
}
```

## 取り出しの仕組み

- 行の型と列（`FieldInfo` の並び。入れ子は経路）ごとに、`Action<object, Array>` を式木で作る。中身は「`List<T>`（または `T[]`）を型付きで受け取り、全要素の 1 列を型付きの配列に詰めるループ」。**測った**形（`../dev-notes.md`）と同じで、値を箱に入れない
- コンパイルした取り出しは、（行の型, 列の経路）ごとに覚えておく。ドメインリロードまで再利用する
- 文字列表への集約は `Dictionary<string, int>`（`StringComparer.Ordinal`）

## インターフェース

```csharp
// Editor: たどるフィールドを選ぶ（Unity のシリアライズの規則）
public static class SerializedFieldPolicy
{
    public static IReadOnlyList<FieldInfo> GetFields(Type type);
}

// Core: 渡されたフィールドで発見と取り出しを行う
public static class Extractor
{
    public static Snapshot Extract(object root, Func<Type, IReadOnlyList<FieldInfo>> fieldPolicy);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| BCL（`System.Reflection`・`System.Linq.Expressions`） | 発見と取り出し |
