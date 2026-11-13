# annotations 設計

## データ構造

```csharp
public sealed class AnnotationSet
{
    public string Name;
    public IReadOnlyDictionary<long, Annotation> ByInteger;
    public IReadOnlyDictionary<string, Annotation> ByString;
}
public readonly struct Annotation { public readonly string Label; public readonly string Note; }

// settings に足す
[Serializable] public sealed class AnnotationBinding
{
    public string set;          // 集まりの名前
    public string table;        // テーブルのパス
    public string column;       // 空なら、そのテーブルの主キー（参照の列にも伝わる）
}
```

- 名前は表示と検索の派生データとして、索引の作成のときに `Display` とは別の配列（列ごとの名前の文字列の番号）に入れる。Database ができたあとで書き足さない（`../design.md` のスレッド）
- 参照の列への伝わり方: 参照の列の値は参照先の主キーの値なので、参照先の主キーに結び付いた集まりで引く

## インターフェース

```csharp
public interface IAnnotationProvider       // 利用者が Editor のアセンブリで実装する
{
    IEnumerable<AnnotationSet> LoadSets();  // メインスレッドで呼ばれる
    event Action Changed;
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| settings | 結び付けの保存 |
| index | 名前の派生データ・検索 |
| window | 表示 |
