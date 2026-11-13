# fixtures 設計

## アーキテクチャ

- テストのアセンブリに、例の形の型（`FixtureContainer : ScriptableObject` と、`List<Data>` を持つテーブルのクラス群）を置く。テーブルの数を変えられるよう、型は少数の形を使い回す（同じ行の型のテーブルを複数持つ）
- 生成器は、種を固定した乱数でアセットを埋め、`AssetDatabase.CreateAsset` で一時的な場所（`Assets/` の下の、コミットしないフォルダ。`.gitignore` に足す）に保存する。アセットにするのは、アセットの読み込みの時間（テキストのアセット）も測るため
- 設定（主キーと参照）も生成器が作る
- 計測のテストは `Stopwatch` で段階ごとに測り、結果を `Assert` のメッセージに入れて返す。MCP の `tests-run` の結果で値を読める

## インターフェース

```csharp
public sealed class FixtureScale
{
    public int Tables, MaxRowsPerTable, TotalRows, IntColumns, FloatColumns, StringColumns, EnumColumns, RefColumns, DistinctStrings, Seed;
    public static FixtureScale Standard { get; }
}

public static class FixtureGenerator
{
    public static (FixtureContainer asset, DataViewerSettings settings) Generate(string assetPath, FixtureScale scale);
}
```

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| UnityEditor（`AssetDatabase`） | アセットの作成 |
| extract・index・source・window | 計測の対象 |
