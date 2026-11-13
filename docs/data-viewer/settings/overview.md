# settings 概要

親: [data-viewer](../overview.md)

## 目的・背景

主キーと参照は、名前の規則で推測せず、設定だけで指定する（利用者の決定）。その設定を保存し、編集する画面を出し、データと食い違ったときに知らせる。

## スコープ

### 作るもの

- 設定のデータ（ソース・テーブルごとの主キー・参照の一覧）と保存
- 設定とデータの食い違いの検出
- 設定の画面（window の中のペイン）

### 作らないもの

- 候補の自動の決定（画面は候補を並べるだけ）

## 制約

- `ProjectSettings/Obelune.DataViewer.json` に保存し、Git で管理してチームで共有する
- 設定は、テーブルをパス（`characterData.data`）、列を名前で書く

## 完了条件

- `test-cases.md` の U-01〜U-05、E-01 が通る

## 画面イメージ

```
+-------------------------------------------------------------+
| 設定                                                          |
| ソース: [アセット ▼] [Assets/Data/GameData.asset      ] [選択] |
| 主キー                                                        |
|  Character        (characterData.data)       [character_id ▼] |
|  CharacterAbility (characterAbilityData.data)[（なし）     ▼] |
|  Ability          (abilityData.data)         [ability_id   ▼] |
| 参照                                               [+ 足す]    |
|  CharacterAbility.character_id → Character   なしの値 [0 ] [×] |
|  CharacterAbility.ability_id   → Ability     なしの値 [0 ] [×] |
| ⚠ Character.chara_id: 列がデータにない（名前が変わった？）        |
+-------------------------------------------------------------+
```
