---
name: translate-to-jp
description: Translate English agent files to Japanese and save them to the _jp/ folder.
disable-model-invocation: true
allowed-tools: Read, Write, Glob
---

Translate English agent files to Japanese and save them to the `_jp/` folder.

## Source patterns

Translate all files matching the following patterns:

| Pattern | Example |
|---------|---------|
| `CLAUDE.md` | `CLAUDE.md` → `_jp/CLAUDE.md` |
| `rules/*.md` | `rules/bugs.md` → `_jp/rules/bugs.md` |
| `.claude/skills/*/SKILL.md` | `.claude/skills/add-feature/SKILL.md` → `_jp/skills/add-feature/SKILL.md` |

When new agent files are added in the future, they will be picked up automatically as long as they match these patterns.

## Translation rules

- Translate all prose and descriptions to natural Japanese
- Keep the following as-is (do not translate):
  - Code blocks and inline code
  - File paths and directory names
  - Command names and skill names
  - YAML frontmatter keys (e.g. `name:`, `description:`, `allowed-tools:`)
  - Frontmatter values for `name` and `allowed-tools`
  - `@` import lines in `CLAUDE.md` (e.g. `@rules/principles.md`)
- Translate frontmatter values for `description` and `argument-hint`

## Steps

1. Use Glob to find all files matching the source patterns
2. For each file, read the content
3. Translate to Japanese following the rules above
4. Write to the corresponding output path under `_jp/`
5. Use Glob to list `_jp/**/*.md` and find files whose source no longer exists
   (do not read them — only compare paths)
6. Report which files were translated, and list the orphaned `_jp/` files so the user can delete them
