# Project Rules

This project's development process and documentation standards are defined under `rules/`.
When given an implementation task, follow them and prepare documentation before starting.

## Always

@rules/principles.md
@rules/workflow.md
@rules/structure.md

## Read when needed

Read the file **before** starting the work it covers.

| When | Read |
|---|---|
| A bug is reported, or you are about to create or update a bug ticket | `rules/bugs.md` |
| Starting a project, or deciding what to test automatically | `rules/automated-tests.md` |
| You cannot run the product yourself and need verification on real hardware | `rules/verification.md` |
| Replacing a UI framework, runtime, or any layer wholesale | `rules/porting.md` |

## Never

- Do not load or reference any files under `_jp/` — that directory is for human reference only
