# data-viewer タスク

個々のタスクはサブ項目にある。ここには順序と、サブ項目をまたぐタスクだけを置く。

## 進め方

要件はまだ固まっていない（利用者、2026-11-13）。作り直す前提で、**小さく作って、実際に見て、決める**を繰り返す。各検証で決まったことは spec.md・design.md に戻し、外れた設計はその時点で書き直す。検証のコードは捨ててよい。

### 検証 1: 見え方を確かめる

- **目的**: テーブルの一覧・行の一覧・行の詳細・参照の移動が、実際にどう見えるか。補助情報の表示の候補（annotations の `spec.md` の A〜D）を見比べて決める
- **作るもの**:
  - パッケージの骨組み（`UnityProject/Packages/com.obelune.data-viewer/`）。あわせて、埋め込みパッケージの中のテストが見つかるかを測る（**推測**: 見つからなければ `manifest.json` に `"testables"` が要る。結果を `docs/testing.md` に書く）
  - 例の形の合成データ（Character・CharacterAbility・Ability など。数千行と、スクロールの感触を見るための 2 万行のテーブル 1 つ）と、キャラクター名の CSV
  - ウィンドウ: テーブルの一覧・行の一覧（`MultiColumnListView`）・行の詳細（参照先・参照元の移動、戻る）・単純な検索・補助情報の表示の切り替え（A〜D）
  - 主キー・参照・補助情報の結び付けは、手で書いた設定ファイルで与える
- **作らないもの**: 設定の画面・問題の一覧・変更の検出・性能の作り込み（取り出しは単純なリフレクションでよい）
- **決めること**: 補助情報の表示の仕方。画面の構成（3 ペインでよいか）。次の検証で何を確かめるか
- **結果**（2026-11-13、利用者が触って判断）:
  - 補助情報の表示は **A**、名前は括弧で囲む（`1（Claude）`）。B は次点、C・D は不採用（annotations の `spec.md`）
  - 3 ペインの構成はよい。ただし行の詳細を一番よく見るので、**最初から行の一覧と半分ずつ**の広さにする
  - **グループ**を入れる: DataContainer → グループ → データクラス → `List`。テーブル一覧はグループごとに畳め、最初は全部畳む。参照はグループをまたぐ（`spec.md` のデータの決まり）
  - 「名前も検索する」は、オンかオフかが分かりにくかった。チェックボックスに変えた
  - 以上を検証 1 のウィンドウに反映済み（サンプルの型もグループの形に変えた）

### 検証 2 以降

- 検証 1 の結果で決める。候補: 実際の型での確認、設定の画面、性能の目標（fixtures の計測）

## 実装タスク一覧（目指す形。検証の結果で変わる）

- [ ] パッケージの骨組み（`design.md` のアセンブリの表）
- [ ] [fixtures](./fixtures/tasks.md)
- [ ] [extract](./extract/tasks.md)
- [ ] [settings](./settings/tasks.md)
- [ ] [index](./index/tasks.md)
- [ ] [source](./source/tasks.md)
- [ ] [annotations](./annotations/tasks.md)
- [ ] [window](./window/tasks.md)（設定の画面を含む）
- [ ] [fixtures](./fixtures/tasks.md) の計測をすべて通し、値を `dev-notes.md` に残す

## テストの線引き

全体の線は `docs/testing.md`。この機能では次のとおり。

| 対象 | 自動で書けるか | 置き場所 |
|---|---|---|
| 取り出し・index（`Obelune.DataViewer.Core`） | **書ける**（テスト用の型を直接作る） | `Obelune.DataViewer.Core.Tests`（MCP 経由） |
| たどるフィールドの判定・source・settings・性能の目標 | **書ける**（エディタの中で） | `Obelune.DataViewer.Editor.Tests`（MCP 経由） |
| 見た目・操作の手触り | 実機が要る | 各 `test-cases.md` の E 系 |

## 依存関係

- パッケージの骨組み → すべて
- fixtures の生成器 → extract の速さの計測
- extract → index → window
- settings のデータ構造 → index
- source → window
