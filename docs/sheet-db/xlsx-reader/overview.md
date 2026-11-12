# xlsx-reader 概要

親: [sheet-db](../overview.md)

## 目的・背景

xlsx のシートのセルを、BCL（`System.IO.Compression`・`System.Xml`）だけで、先頭から順に読み出す。外部パッケージに依存しない制約（`docs/project_overview.md`）のための自作の読み込み処理。

## スコープ

### 作るもの

- ブックのシートの一覧（名前と、zip の中の場所）
- 共有文字列の表
- シートのセルを 1 つずつ返す、前から順に読む読み込み器

### 作らないもの

- 書き込み
- 書式（`styles.xml`）の解釈。日付も数値のまま返す
- 数式の計算。保存されている計算結果だけを返す
- 結合セル・コメント・画像・グラフ

## 制約

- `Obelune.SheetDb.Core` に入る。Unity の API を使わない。スレッドを選ばない
- 2 万行 × 10 列のシートを 300 ms 以内で読む（計測の試作で約 240 ms。`../dev-notes.md`）

## 完了条件

- `test-cases.md` の U-01〜U-12 が通る
- 実物の Excel で保存したブック（`test-cases.md` の I-01）を読める

## 画面イメージ

画面はない。
