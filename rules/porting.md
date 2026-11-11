# Regression Net for Ports and Rewrites

Read this when replacing a UI framework, runtime, or any layer wholesale.

Build a coverage table that maps **every item in the old `spec.md` feature lists** to
済 / 要 / 差 (done / to-verify / intentionally different).

- **Anchor the table to the spec's feature list, not to the old test cases.**
  A feature with no test case is invisible to a table built from test cases, and will be
  dropped silently. This has happened: an "asynchronous loading" spec item was lost in a
  port and went unnoticed until a user reported slowness weeks later
- Build the table **while the old implementation still runs**. Once it is removed, the
  comparison can never be taken again
- Staged verification only checks what each stage added, so it cannot catch what the port
  forgot. The table is the only thing that can
