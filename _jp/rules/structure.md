# プロジェクト構成

## ディレクトリ構成

```
CLAUDE.md                  # Entry point — loads the always-on rules, lists the on-demand ones
rules/                     # Agent rules (English). See CLAUDE.md for when each is read

docs/
├── index.md               # Required — list of feature areas and sub-items
├── project_overview.md    # Required — overall project overview
├── glossary.md            # Required — domain-specific terminology
├── tags.md                # Required — inline tag definitions and schema
├── testing.md             # Required — the line between automated and manual tests
└── {feature-area}/        # One directory per feature area
    ├── overview.md
    ├── spec.md
    ├── design.md
    ├── tasks.md
    ├── test-cases.md
    ├── dev-notes.md
    └── {sub-item}/        # Created as needed per split rules
        └── ...

templates/                 # Document templates (all copied by /add-feature)
template_samples/          # Filled-in examples of the templates

issues/                    # Bug ticket management
├── index.md               # Ticket list, and the legend for state / rank / type
├── templates/
│   └── BUG-template.md
└── tickets/
    └── BUG-{NNN}.md        # Added sequentially as bugs are reported

tests/                     # Automated tests (see docs/testing.md for the line)

tmp/                       # Not committed. Working area
├── 確認/                   # Verification requests written by the agent
│   └── old/               #   archived after each round
└── フィードバック/           # Reports from the verification session
    └── old/               #   archived after each round

.claude/skills/            # Skills (see below)
_jp/                       # Japanese translations for humans. Agents do not read it
```

（上のコードブロック内のコメントの意味）

- `CLAUDE.md`：入口。常に読むルールを読み込み、必要なときに読むルールを一覧にする
- `rules/`：エージェント向けルール（英語）。それぞれをいつ読むかは CLAUDE.md を参照
- `docs/` の `index.md`・`project_overview.md`・`glossary.md`・`tags.md`・`testing.md`：必須。
  それぞれ機能エリアとサブ項目の一覧、プロジェクト全体の概要、ドメイン固有の用語、
  インラインタグの定義とスキーマ、自動テストと手動テストの境界
- `{feature-area}/`：機能エリアごとに1ディレクトリ。`{sub-item}/` は分割ルールに従い必要に応じて作る
- `templates/`：ドキュメントのテンプレート（`/add-feature` がすべてコピーする）
- `template_samples/`：テンプレートの記入例
- `issues/`：バグ票の管理。`index.md` は票の一覧と、ステート・ランク・タイプの凡例。
  `BUG-{NNN}.md` はバグが報告されるたびに連番で追加する
- `tests/`：自動テスト（境界は docs/testing.md を参照）
- `tmp/`：コミットしない作業領域。`確認/` はエージェントが書く確認依頼、
  `フィードバック/` は確認セッションからの報告。どちらも1回ごとに `old/` へ移す
- `.claude/skills/`：スキル（下記参照）
- `_jp/`：人間向けの日本語訳。エージェントは読まない

---

## どの文書に何を書くか

各テンプレート自身のコメントが、そこに何を書くかの正式な説明である。

| 文書 | 書くこと |
|---|---|
| `docs/project_overview.md` | プロジェクトの存在理由、範囲、技術スタック、アーキテクチャ、制約 |
| `docs/testing.md` | 対象環境なしで CI で回すもの、手動で残すもの、その理由 |
| `overview.md` | 機能が必要な理由、範囲、制約、完了の定義 |
| `spec.md` | ユーザから見て機能が何をするか。その機能一覧が、テストケースと移植時の網羅の起点になる |
| `design.md` | どう作るか：技術、コンポーネント、データ、インターフェース、依存関係 |
| `tasks.md` | タスク一覧とステータス、依存関係、その機能の自動テストと手動テストの境界 |
| `test-cases.md` | spec.md の機能一覧から参照されるテストケース。E2E の各行に対照を添える |
| `dev-notes.md` | 判断、問題、設計からの逸脱、今後の課題、ユーザへの要望、確認の方法 |
| `issues/tickets/BUG-{NNN}.md` | バグ1件、根本原因1つ。`rules/bugs.md` を参照 |

---

## スキル

| スキル | 用途 |
|---|---|
| `/add-feature <path>` | 機能エリアまたはサブ項目を作る：ディレクトリ、テンプレート、index.md の行 |
| `/verify-request <title> [--run <id>] [--workflow <name>]` | 確認依頼を生成する。**CI の成果物を自分で測る**（サイズ、SHA256）ので書き写し間違いが起きない。作業ツリーが汚れているときや、HEAD が run と異なるときは警告し、前回の未了項目を引き継ぐ。`--run` を省略すると、現在のブランチで最新の成功した run を選ぶ。`gh`・`git`・`awk` が必要。`rules/verification.md` を参照 |
