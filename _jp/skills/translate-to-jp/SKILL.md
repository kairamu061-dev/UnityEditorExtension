---
name: translate-to-jp
description: 英語のエージェント用ファイルを日本語に翻訳し、_jp/ フォルダに保存する。
disable-model-invocation: true
allowed-tools: Read, Write, Glob
---

英語のエージェント用ファイルを日本語に翻訳し、`_jp/` フォルダに保存します。

## 翻訳元のパターン

次のパターンに一致するすべてのファイルを翻訳します。

| パターン | 例 |
|---------|---------|
| `CLAUDE.md` | `CLAUDE.md` → `_jp/CLAUDE.md` |
| `rules/*.md` | `rules/bugs.md` → `_jp/rules/bugs.md` |
| `.claude/skills/*/SKILL.md` | `.claude/skills/add-feature/SKILL.md` → `_jp/skills/add-feature/SKILL.md` |

今後エージェント用ファイルが追加されても、これらのパターンに一致していれば自動的に対象になります。

## 翻訳のルール

- 本文と説明はすべて自然な日本語に翻訳する
- 次のものはそのまま残す（翻訳しない）：
  - コードブロックとインラインコード
  - ファイルパスとディレクトリ名
  - コマンド名とスキル名
  - YAML フロントマターのキー（例：`name:`、`description:`、`allowed-tools:`）
  - フロントマターの `name` と `allowed-tools` の値
  - `CLAUDE.md` の `@` インポート行（例：`@rules/principles.md`）
- フロントマターの `description` と `argument-hint` の値は翻訳する

## 手順

1. Glob で翻訳元のパターンに一致するファイルをすべて探す
2. 各ファイルの内容を読む
3. 上のルールに従って日本語に翻訳する
4. `_jp/` 配下の対応する出力パスに書き込む
5. Glob で `_jp/**/*.md` を一覧し、翻訳元がもう存在しないファイルを探す
   （中身は読まない — パスを比べるだけ）
6. 翻訳したファイルを報告し、ユーザが削除できるよう、翻訳元を失った `_jp/` のファイルを一覧にする
