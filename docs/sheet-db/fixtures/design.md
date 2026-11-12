# fixtures 設計

## アーキテクチャ

- 生成器は、xlsx に必要な最小限の部分（`[Content_Types].xml`・rels・`workbook.xml`・シート・共有文字列）を `ZipArchive` で書く。計測の試作で使った Python の生成器と同じ形（`../dev-notes.md`）
- 計測のテストは、`Stopwatch` で段階ごとに測り、結果を `Assert` のメッセージに入れて返す。MCP の `tests-run` の結果で値を読める

## インターフェース

```csharp
public sealed class FixtureScale
{
    public int Books, TablesPerBook, RowsPerTable, MaxRowsPerTable, Columns, RefColumns, DistinctStrings, Seed;
    public static FixtureScale Standard { get; }
}

public static class FixtureGenerator
{
    public static void Generate(string folder, FixtureScale scale);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| BCL（`System.IO.Compression`） | xlsx の書き出し |
| loader・window | 計測の対象 |
