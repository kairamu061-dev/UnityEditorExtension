# cache 設計

## データ構造

置き場所: `Library/Obelune/SheetDb/Cache/<データフォルダからの相対パスの SHA-1>.bin`

```
"OSDB"（4 バイト） | 形式の版（int） | 解釈の版（int） | 元のパス（string） | 元の大きさ（long） | 元の更新時刻 UTC の Ticks（long）
StringTable: 個数, 文字列…
テーブルの数, テーブルごとに:
  名前, 行数, ExcelRows[], 列の数, 列ごとに:
    名前, 型, RefTable, ExcelColumn, HasValue（ビット列）, 値の配列（型に応じて 1 つ。Ref は Ints と Strings の 2 つ）
問題の数, 問題…
```

- `BinaryWriter` / `BinaryReader`。配列は長さを先に書き、要素を続ける
- **形式の版**（`BookCache.FormatVersion`）は、この形を変えたら必ず上げる。上げ忘れると、古いキャッシュを新しい形として読み、値がずれる
- **解釈の版**（`BookParser.RulesVersion`、schema に置く）は、xlsx-reader と schema の出力が変わる修正（受け付ける書き方を増やす・数値の書き方を変える・ふりがなの扱いを変える など）をしたら必ず上げる。形が同じでも、上げ忘れると古い読み方の結果がキャッシュから出続け、ブックを保存し直すまで気づけない
- **推測**: 版を手で上げる代わりに、Core のアセンブリの `ModuleVersionId` を使えば自動になる。ただし Unity のコンパイルが毎回同じ ID を出すか（出さなければ、リロードのたびにキャッシュが全部無効になる）を測ってから決める

## インターフェース

```csharp
public static class BookCache
{
    public static void Write(string cacheDir, BookData book, FileStamp source);   // 一時ファイル → File.Replace / Move
    public static bool TryRead(string cacheDir, string bookPath, FileStamp source, out BookData book);
}

public readonly struct FileStamp { public readonly long Length; public readonly long LastWriteTicksUtc; }
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| schema | `BookData` |
