# xlsx-reader 設計

## 技術選定

| 技術 | 用途 | 選定理由 |
|------|------|----------|
| `ZipArchive`（読み取り） | xlsx を開く | BCL。両方の API 互換レベルで追加の設定なしに使えることを**測った** |
| `XmlReader`（前から順に読む） | シートの XML | DOM を作らない。2 万行で約 240 ms（**測った**） |

## アーキテクチャ

```
XlsxWorkbook.Open(stream)
  ├─ workbook.xml + workbook.xml.rels → Sheets（名前, 場所）
  ├─ sharedStrings.xml → string[]（最初に 1 回だけ全部読む。セルから番号で引くため）
  └─ OpenSheet(sheet) → XlsxSheetReader（Read(out XlsxCell) を繰り返す）
```

## データ構造

```csharp
public enum XlsxCellKind { Number, Text, Bool, Error, NoValue }

public readonly struct XlsxCell
{
    public readonly int Row;        // 0 始まり
    public readonly int Column;     // 0 始まり（A = 0）
    public readonly XlsxCellKind Kind;
    public readonly double Number;  // Number・Bool（0/1）
    public readonly string Text;    // Text・Error（"#N/A" など）
    public readonly bool HasFormula;
}
```

- `struct` で返し、セルごとのオブジェクトを作らない
- 文字列は共有文字列の表の要素をそのまま返す（コピーしない）

## インターフェース

```csharp
public sealed class XlsxWorkbook : IDisposable
{
    public static XlsxWorkbook Open(Stream stream);          // stream の所有権を受け取る
    public static XlsxWorkbook Open(string path);            // FileShare.ReadWrite | Delete で開く
    public IReadOnlyList<XlsxSheetInfo> Sheets { get; }
    public XlsxSheetReader OpenSheet(XlsxSheetInfo sheet);
}

public sealed class XlsxSheetReader : IDisposable
{
    public bool Read(out XlsxCell cell);                     // 行の順、行の中は列の順
}

public sealed class XlsxFormatException : Exception { }
```

- 数値の解釈は `CultureInfo.InvariantCulture`。OS の地域設定で小数点が変わっても同じ値になる

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| BCL のみ | — |
