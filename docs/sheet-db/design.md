# sheet-db 設計

個々の設計はサブ項目に書く。ここには、サブ項目をまたいで効く決めごとだけを置く。

| サブ項目 | 役割 | 設計 |
|---|---|---|
| xlsx-reader | xlsx を開き、シートのセルを順に読み出す | [design](./xlsx-reader/design.md) |
| schema | セルの並びを、型の付いたテーブルにする。型の検査 | [design](./schema/design.md) |
| index | 主キーの索引・参照の解決・参照元の逆引き・検索 | [design](./index/design.md) |
| cache | 読み込んだテーブルを `Library/` に保存し、読み戻す | [design](./cache/design.md) |
| loader | 設定・ブックの発見・裏での読み込み・変更の検出・計測 | [design](./loader/design.md) |
| window | EditorWindow（UI Toolkit） | [design](./window/design.md) |
| fixtures | 合成データの生成と、性能の目標の計測（開発用） | [design](./fixtures/design.md) |

## 技術選定

| 技術 | 用途 | 選定理由 |
|------|------|----------|
| `System.IO.Compression.ZipArchive` / `System.Xml.XmlReader` | xlsx の読み込み | 外部パッケージに依存しない制約のため。BCL だけで、2 万行 × 10 列を約 240 ms で読めることを**測った**（`dev-notes.md`） |
| UI Toolkit の `MultiColumnListView` | 行の一覧 | 見えている行だけを作る（仮想化）。`docs/project_overview.md` の「新しく作るウィンドウは UI Toolkit を優先」 |
| `System.Threading.Tasks` | 裏での読み込み | ブックごとに独立しているので、並列に読める。エディタを止めない |
| 独自のバイナリ形式 | キャッシュ | `JsonUtility` などより読み戻しが速く、列ごとの配列をそのまま書ける |

## アーキテクチャ

```
                      ┌───────────── Obelune.SheetDb.Core（noEngineReferences）─────────────┐
 *.xlsx ──→ xlsx-reader ──→ schema ──→ TableData ──┬──→ index ──→ Database（読み取り専用の一式）
                                                  │                    ↑
 Library/…/<ブック>.bin ←──── cache ─────────────┘                    │
                      └─────────────────────────────────────────────────┼───────────────────┘
                      ┌───────────── Obelune.SheetDb.Editor ──────────────┼───────────────────┐
                      │  loader: 発見・変更の検出・裏での読み込み・差し替え ─┘                   │
                      │  window: Database を表示する                                          │
                      └───────────────────────────────────────────────────────────────────────┘
```

### アセンブリ

| asmdef | 中身 | 設定 |
|---|---|---|
| `Obelune.SheetDb.Core` | xlsx-reader・schema・index・cache | `noEngineReferences: true`。`includePlatforms: ["Editor"]`。UnityEngine にも UnityEditor にも触れない |
| `Obelune.SheetDb.Editor` | loader・window | `Obelune.SheetDb.Core` を参照する |
| `Obelune.SheetDb.Core.Tests` | Core のテスト | `docs/project_overview.md` の asmdef の規則（`nunit.framework.dll` だけ） |
| `Obelune.SheetDb.Editor.Tests` | Editor のテストと fixtures | 同上。fixtures の生成器はここに置き、配布物の本体には入れない |

- すべて `overrideReferences: true`（`docs/project_overview.md` の規則）
- **Core に Unity の API を入れない。**入れると、未決の CI 案（`docs/testing.md` の `dotnet test`）で Core を回せなくなる。Core はスレッドプールの上でも動く前提なので、Unity の API を呼ぶとメインスレッド以外から呼ぶ事故にもなる

### スレッド

- **Core の処理はスレッドを選ばない。**ブック 1 冊の読み込み（reader → schema → TableData）は、スレッドプールで並列に動かす
- **Database は読み取り専用で、丸ごと差し替える。**ブックが変わったら、新しい TableData を混ぜて新しい Database を作り、メインスレッドで 1 回の代入で差し替える。UI は、手元の Database を最後まで使う。ロックは使わない
- UnityEditor の API（`EditorApplication`・`AssetDatabase`・UI）を呼ぶのは loader と window だけで、メインスレッドに限る

### ドメインリロード

- スクリプトをコンパイルするたびに、static な値はすべて消える（**測った**: `docs/testing.md` のリロードの振る舞い）。メモリ上の Database も消える
- だから「ウィンドウを開いて 0.5 秒」の目標は、**コンパイルのあとに毎回キャッシュから読み戻す**時間を含む。キャッシュの読み戻しを速くすることが、体感の速さの中心になる

### メモリの持ち方

- テーブルは**列ごとの配列**で持つ（`int[]`・`double[]`・文字列の番号の `int[]`・値の有無のビット列）。行ごとのオブジェクトを作らない。10 万行 × 20 列でも、オブジェクトの数は列の数で済む
- 文字列は Database 全体で 1 つの**文字列表**に集め、セルは番号で持つ。同じ文字列（`Common`・`Weapon` など）は 1 つにまとまり、検索は文字列表の側を探せば済む（index の設計）

## 計測（製品自身に数字を言わせる）

- loader は、読み込みの段階ごとの所要時間（キャッシュの読み戻し・xlsx の読み込み・索引の作成・差し替え）を記録し、ウィンドウの下端に出す
- 同じ値を `Obelune.SheetDb.Editor.SheetDbDiagnostics.Last` から読めるようにする。エージェントは MCP の `script-execute` でこれを読み、`spec.md` の性能の目標と比べる
- 性能の目標の計測は fixtures の EditMode テストで行い、値を `dev-notes.md` に残す

## 依存関係

| ライブラリ / サービス | 用途 |
|-----------------------|------|
| BCL（`System.IO.Compression`・`System.Xml`・`System.Threading.Tasks`） | 読み込み・並列化 |
| UnityEditor・UI Toolkit | loader・window のみ |
| （外部パッケージなし） | — |
