# data-viewer 設計

個々の設計はサブ項目に書く。ここには、サブ項目をまたいで効く決めごとだけを置く。

| サブ項目 | 役割 | 設計 |
|---|---|---|
| source | ルートを受け取る。変更を知る | [design](./source/design.md) |
| extract | ルートからテーブルを見つけ、列ごとの配列に取り出す | [design](./extract/design.md) |
| settings | 主キーと参照の設定。保存と、設定の画面 | [design](./settings/design.md) |
| index | 主キーの索引・参照の解決・参照元の逆引き・検索・検査 | [design](./index/design.md) |
| window | EditorWindow（UI Toolkit） | [design](./window/design.md) |
| fixtures | 合成データの生成と、性能の目標の計測（開発用） | [design](./fixtures/design.md) |
| annotations | データベースの外の「値 → 名前」の補助情報 | [design](./annotations/design.md) |

## 技術選定

| 技術 | 用途 | 選定理由 |
|------|------|----------|
| リフレクション＋式木（`System.Linq.Expressions`）でコンパイルした取り出し | ユーザーの型から列の配列へ | `FieldInfo.GetValue` より 20 倍ほど速い（**測った**: int の 8 列 × 10 万行で約 12 ms 対 約 270 ms）。エディタは Mono の JIT で動くので、`Compile()` が使える |
| UI Toolkit の `MultiColumnListView` | 行の一覧 | 見えている行だけを作る（仮想化） |
| `System.Threading.Tasks` | 索引の作成 | 取り出したあとの配列だけを使うので、メインスレッドの外で作れる |
| `JsonUtility` | 設定の保存 | Editor のアセンブリで使える。外部パッケージが要らない |

## アーキテクチャ

```
 source（ルート）──→ extract（メインスレッド）──→ Snapshot（列ごとの配列。ユーザーの型から切り離す）
                                                        │
                       settings（主キー・参照）──────────┤
                                                        ↓
                                 index（スレッドプール）──→ Database（読み取り専用）──→ window
```

### アセンブリ

| asmdef | 中身 | 設定 |
|---|---|---|
| `Obelune.DataViewer.Core` | 取り出しの仕組み（式木）・Snapshot・index | `noEngineReferences: true`。`includePlatforms: ["Editor"]` |
| `Obelune.DataViewer.Editor` | source・どのフィールドをたどるかの判定・settings・window | `Obelune.DataViewer.Core` を参照する |
| `Obelune.DataViewer.Core.Tests` / `Obelune.DataViewer.Editor.Tests` | テスト。fixtures は Editor.Tests に置く | `docs/project_overview.md` の asmdef の規則 |

- **どのフィールドをたどるか**（`[SerializeField]` の有無など）の判定は Editor に置く。`SerializeField` は UnityEngine の型なので、`noEngineReferences` の Core からは見えない。Editor が選んだ `FieldInfo` の並びを Core の取り出しに渡す
- すべて `overrideReferences: true`（`docs/project_overview.md` の規則）

### スレッド

- **取り出しはメインスレッドで行う。**ユーザーのオブジェクトは、インポートなどでメインスレッドから書き換わることがある。別のスレッドから読むと、途中の状態を読むおそれがある。10 万行で数十 ms の見込み（int の列で**測った**。`string`・`enum` の列は**推測**。fixtures I-02 で測る）
- 取り出したあとの Snapshot は、ユーザーのオブジェクトを参照しない（文字列はそのまま共有してよい。.NET の文字列は書き換わらない）
- **索引の作成はスレッドプールで行う。**Snapshot と設定だけを使う
- **Database は読み取り専用で、丸ごと差し替える。**差し替えはメインスレッドで、1 回の代入で行う。ロックは使わない
- 表示と検索のための派生データ（数値の表示の文字列・小文字の写し）は、取り出しか索引の作成のときに作る。Database ができたあとで書き足さない

### ドメインリロード

- コンパイルのたびに static な値は消える（`docs/testing.md`）。そのたびに取り出しと索引の作成をやり直す。キャッシュは持たない（取り出しが速いため。fixtures I-03 で、アセットの読み込みを含めて確かめる）

### メモリの持ち方

- テーブルは列ごとの配列（`long[]`・`double[]`・文字列の番号の `int[]`・値の有無のビット列）で持つ。行ごとのオブジェクトを作らない
- 文字列は Snapshot 全体で 1 つの文字列表に集め、セルは番号で持つ。検索は文字列表の側を探す

## 計測（製品自身に数字を言わせる）

- 段階ごとの所要時間（アセットの読み込み・取り出し・索引の作成・差し替え）を記録し、ウィンドウの下端に出す
- 同じ値を `Obelune.DataViewer.Editor.DataViewerDiagnostics.Last` から読めるようにする。エージェントは MCP の `script-execute` でこれを読む
- 性能の目標の計測は fixtures の EditMode テストで行い、値を `dev-notes.md` に残す

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| BCL（`System.Reflection`・`System.Linq.Expressions`・`System.Threading.Tasks`） | 取り出し・索引 |
| UnityEditor・UI Toolkit | Editor のアセンブリのみ |
| （外部パッケージなし） | — |
