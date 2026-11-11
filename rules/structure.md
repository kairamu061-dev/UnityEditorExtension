# Project Structure

## Directory Layout

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

---

## Which document holds what

Each template's own comments are the authoritative description of what goes in it.

| Document | Holds |
|---|---|
| `docs/project_overview.md` | Why the project exists, scope, tech stack, architecture, constraints |
| `docs/testing.md` | What runs in CI without the target environment, what stays manual, and why |
| `overview.md` | Why the feature is needed, scope, constraints, definition of done |
| `spec.md` | What the feature does, as the user sees it. Its feature list is the anchor for test cases and port coverage |
| `design.md` | How it is built: tech, components, data, interfaces, dependencies |
| `tasks.md` | Task list and status, dependencies, and the feature's automated/manual test line |
| `test-cases.md` | Test cases referenced from spec.md's feature list, each E2E row with its control |
| `dev-notes.md` | Decisions, issues, deviations, future work, requests to the user, and how we verify |
| `issues/tickets/BUG-{NNN}.md` | One bug, one root cause. See `rules/bugs.md` |

---

## Skills

| Skill | Use |
|---|---|
| `/add-feature <path>` | Create a feature area or sub-item: directory, templates, index.md row |
| `/verify-request <title> [--run <id>] [--workflow <name>]` | Generate a verification request. **Measures the CI artifacts itself** (sizes, SHA256) so they cannot be mis-transcribed, warns on a dirty tree or a HEAD that differs from the run, and carries over the previous round's open items. Without `--run`, picks the latest green run on the current branch. Requires `gh`, `git`, `awk`. See `rules/verification.md` |
