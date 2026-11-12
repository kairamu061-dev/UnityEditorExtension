# loader 設計

## アーキテクチャ

```
[メインスレッド]                                   [スレッドプール]
 変更の検出 ──→ 判定（足された・変わった・消えた）
              └─ 1 冊ごとに Task.Run ───────────→ BookCache.TryRead ?? (XlsxWorkbook → BookParser → BookCache.Write)
                                                        │
 EditorApplication.update ←── ConcurrentQueue<BookData> ┘
   └─ 溜まった分をまとめて DatabaseBuilder.ReplaceBook（スレッドプールで）→ 終わったら Current に代入 → 変更を通知
```

- 差し替えの計算（`ReplaceBook`）もスレッドプールで行い、メインスレッドでは代入と通知だけをする
- 世代番号を持ち、古い読み込みの結果が新しい結果を上書きしないようにする

## データ構造

```csharp
[Serializable] class SheetDbSettings { public string dataFolder; }   // ProjectSettings/Obelune.SheetDb.json

public sealed class LoadReport
{
    public int Books, FromCache, Parsed, Failed;
    public double DiscoverMs, CacheReadMs, ParseMs, BuildMs, TotalMs;   // 並列の部分は合計ではなく経過時間
}
```

## インターフェース

```csharp
public static class SheetDbLoader
{
    public static Database Current { get; }          // メインスレッドから読む
    public static event Action<Database> Changed;
    public static void RequestRefresh();            // 判定と読み込みを始める
}

public static class SheetDbDiagnostics
{
    public static LoadReport Last { get; }           // MCP の script-execute から読む
}

// 判定は純粋な関数にして、テストで確かめる
public static class BookDiff
{
    public static BookChanges Compare(IReadOnlyDictionary<string, FileStamp> before, IReadOnlyDictionary<string, FileStamp> after);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| xlsx-reader・schema・index・cache | 読み込みの各段階 |
| UnityEditor（`EditorApplication.update`・`focusChanged`） | メインスレッドへの受け渡し・変更の検出 |
