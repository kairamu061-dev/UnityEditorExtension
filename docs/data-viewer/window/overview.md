# window 概要

親: [data-viewer](../overview.md)

## 目的・背景

Database を参照・検索・検査するための EditorWindow。画面の構成は親の [overview の画面イメージ](../overview.md#画面イメージ) と [spec の各画面](../spec.md#各画面--状態の詳細) にある。

## スコープ

### 作るもの

- テーブルの一覧・行の一覧・行の詳細・問題の一覧・検索欄・状態の表示
- 参照先・参照元への移動と、戻る・進む
- 設定の画面（中身は [settings](../settings/spec.md)）
- ソースの選択と「再読込」

### 作らないもの

- 値の編集

## 制約

- UI Toolkit。行の一覧は `MultiColumnListView` で、見えている行だけを作る
- 色は独自に決めず、エディタの標準のスタイルに従う（`docs/project_overview.md` の UI デザイン方針）
- メニューは `Tools/Obelune/Data Viewer`

## 完了条件

- `test-cases.md` の I-01・E-01〜E-06 が通る

## 画面イメージ

親の [overview](../overview.md#画面イメージ) を参照。
