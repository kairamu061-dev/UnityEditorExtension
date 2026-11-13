# settings 設計

## データ構造

```csharp
[Serializable] public sealed class DataViewerSettings
{
    public string sourceKind;          // "asset" か、利用者のソースのアセンブリ修飾名
    public string assetPath;
    public List<PrimaryKeySetting> primaryKeys;
    public List<ReferenceSetting> references;
}
[Serializable] public sealed class PrimaryKeySetting { public string table; public string column; }
[Serializable] public sealed class ReferenceSetting
{
    public string table; public string column; public string target;
    public bool hasNullValue; public string nullValue;   // 「参照なし」とみなす値（文字列で持ち、列の型で解釈する）
}
```

- 保存は `JsonUtility.ToJson(…, prettyPrint: true)`。リストはテーブルのパスと列の名前で並べてから保存する
- 食い違いの検出は、`Snapshot` と設定から `Problem` の並びを作る純粋な関数にし、Core.Tests で確かめる（`SettingsValidator.Validate(Snapshot, DataViewerSettings)`。設定の型は Core に置く）

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| extract | 候補と食い違いの検出 |
| UnityEditor（`JsonUtility`） | 保存 |
