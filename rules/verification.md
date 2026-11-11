# Verification Loop

Read this when you cannot run the product yourself (no target OS, no hardware, CI-only builds).
In that case all behaviour is confirmed by a **separate verification session on real hardware**.

Requests go in `tmp/確認/`, reports come back in `tmp/フィードバック/`, and both are archived
to `old/` after each round (layout: `rules/structure.md`).

---

## Each round

1. Push the change, wait for CI to go green
2. Run `/verify-request <title>` to generate the request document
   — it measures the artifacts itself so the numbers cannot be mis-transcribed
3. Fill in the items, naming the negative control for each
4. When feedback arrives: fix what is reported, update tickets and test-cases,
   archive both documents to `old/`, then start the next round. The verification session
   replies in the 報告の書式 embedded at the end of each request; its 未了・判定できなかった項目
   section is carried into the next request automatically

## Rules for request documents

These four rules each exist because breaking them cost a round or shipped a defect.

- **Re-derive every fact at the time of writing.** Sizes, hashes, paths, registry keys —
  take them from the artifact and from the product, never from memory or from an
  earlier document. A path or number written in a request document is itself a claim
- **When the expected value is `0`, supply the control alongside it.** Either a case where
  it becomes `1`, or evidence that the string being counted exists in the shipped binary.
  `0` otherwise means both "stable" and "never executed", and they are indistinguishable
- **Confirm the control exists before writing the item.** "The old build will fail this"
  is a prediction. Verify that the old build actually fails before asking anyone to
  compare against it; an item whose control does not exist cannot be judged
- **Mark every claim as 測った or 推測** (see `rules/principles.md`). The request template
  has a section for this

## Reading feedback

- **"Could not determine" is a result, not a gap.** It is often more informative than a
  pass — treat it as a lead, and never round it to "not a problem"
- When the verification session uses a stronger method than you asked for, **move that
  method into the test-case steps** so the next round does not regress to the weaker one
- A workaround that works does not identify a cause. Record both separately
