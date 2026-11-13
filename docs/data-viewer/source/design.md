# source 設計

## インターフェース

```csharp
// 利用者が Editor のアセンブリで実装する
public interface IDataViewerSource
{
    string DisplayName { get; }
    object LoadRoot();                 // メインスレッドで呼ばれる
    event Action Changed;              // データが変わったら呼ぶ（任意）
}

public sealed class AssetSource : IDataViewerSource { /* 設定のアセットのパスを読む */ }

public static class DataViewerLoader
{
    public static Database Current { get; }
    public static event Action<Database> Changed;
    public static void Reload();       // ルート → extract（メインスレッド）→ index（スレッドプール）→ 差し替え（メインスレッド）
}

public static class DataViewerDiagnostics
{
    public static LoadReport Last { get; }
}

public sealed class LoadReport
{
    public int Tables, Rows;
    public double LoadRootMs, ExtractMs, IndexMs, SwapMs, TotalMs;
}
```

- 世代番号を持ち、古い読み直しの結果が新しい結果を上書きしないようにする
- 利用者のソースは、設定にアセンブリ修飾名で保存する

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| extract・index・settings | 読み直しの各段階 |
| UnityEditor（`AssetDatabase`・`AssetPostprocessor`・`TypeCache`） | アセットの読み込み・変更の検出・ソースの発見 |
