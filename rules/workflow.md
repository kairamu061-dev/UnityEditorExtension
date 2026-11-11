# Workflow

The feature development process. Create documentation before implementation.

---

## Starting a project

1. Create `docs/project_overview.md`, `docs/glossary.md`, and `docs/tags.md` if they do not exist
2. Fill in `docs/testing.md` before the first implementation (see `rules/automated-tests.md`)

## Starting a new feature

1. Run `/add-feature <feature-area>` to set up the directory, templates, and index.md at once
2. Write content in order: overview.md → spec.md → design.md
3. After writing spec.md, evaluate whether subdivision is needed (see "Split Rules")
4. If subdivision is needed, run `/add-feature <sub-item-path>`
5. Write content in tasks.md and begin implementation

## During implementation

- Record decisions, issues, and changes in dev-notes.md as they occur.
  Write it as a record of decisions, not a work log
- If requirements change, update the relevant documents immediately
- If skills, permissions, or information are lacking, record the request in dev-notes.md

## After implementation

1. Update the status in tasks.md
2. If the implementation deviates from the design, update design.md and record the diff in dev-notes.md

## Committing

- Commit after each logical unit of work (e.g. per sub-item completed, per doc section written)
- Do not batch multiple unrelated changes into a single commit
- Push to GitHub at natural stopping points

---

## Split Rules

**Default is to split. Extract any independently implementable unit into a sub-item immediately.**

### Criteria for splitting

Split if any of the following apply:

- Has its own screen, view, or UI surface
- Has its own data definition or schema
- Can be implemented and tested without the other units existing
- A separate developer could work on it in parallel

### Exception — integration is only allowed when BOTH conditions are met

- Neither half can be verified in isolation after splitting
- AND the code and responsibility are extremely small (equivalent to 1 class / 1 file)

### Procedure

1. Evaluate splitting immediately after writing spec.md
2. When a split target is identified, run `/add-feature <sub-item-path>` without hesitation
3. Parent-level design.md / tasks.md should contain only links to sub-items and cross-cutting concerns
4. After splitting, keep the parent's overview.md and spec.md as-is (do not delete them)
