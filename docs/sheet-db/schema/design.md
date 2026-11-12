# schema 設計

## データ構造

```csharp
public enum ColumnType { Int, Float, String, Bool, Ref }

public sealed class ColumnData
{
    public string Name;
    public ColumnType Type;
    public string RefTable;          // Type == Ref のときだけ
    public int ExcelColumn;          // 元の列番号（問題の位置と「Excel で開く」のため）
    // 値: 型に応じてどれか 1 つだけを使う。長さはすべて RowCount
    public long[] Ints;
    public double[] Floats;
    public int[] Strings;            // StringTable の番号
    public bool[] Bools;
    public BitSet HasValue;          // 値なしのとき false
    public int[] Display;            // 表示と検索に使う文字列の StringTable の番号。数値・真偽値の列も含め、全部の列で作る
}

public sealed class TableData
{
    public string BookPath;          // データフォルダからの相対パス
    public string Name;              // シート名
    public int RowCount;
    public ColumnData[] Columns;     // Columns[0] が主キー
    public int[] ExcelRows;          // データの行ごとの、元の Excel の行番号（1 始まり）
    public StringTable Strings;      // このブックの文字列表（ブック内のテーブルで共有）
}

public sealed class BookData
{
    public string Path;
    public TableData[] Tables;
    public Problem[] Problems;
}

public readonly struct Problem
{
    public readonly ProblemKind Kind;
    public readonly string BookPath, Table, Column;
    public readonly int ExcelRow;    // 0 = 行に紐づかない
    public readonly string Detail;
}
```

- `Ref` の列は、読み込みの時点では参照先の主キーの型が分からない（別のブックにあるかもしれない）。値は `Ints`（数値のセル）と `Strings`（文字列のセル）の両方に「生の値」を入れ、index で参照先の型に合わせて解決する
- `StringTable` はブックごと。ブックの読み込みを並列に行うため、ブックをまたいで共有しない
- **表示と検索のための派生データ（`Display` と、StringTable の小文字の写し `Lowered`）は、`BookParser` が読み込みのときに作る。**読み込みは裏のスレッドで行うので、エディタを止めない。作ったものはキャッシュにも入る。Database ができたあとは、誰もこれを書き換えない（`../design.md` のスレッド。読み取り専用の約束を守るため）

## インターフェース

```csharp
public static class BookParser
{
    public static BookData Parse(XlsxWorkbook book, string bookPath);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| xlsx-reader | セルの読み出し |
