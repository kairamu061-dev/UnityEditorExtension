# Principles

Cross-cutting ways of thinking that apply to every document, ticket, and line of code.

---

## Separate what was measured from what was inferred

Mark every claim as **測った** (measured — with the evidence) or **推測** (inferred — with what
would settle it). This applies to bug tickets, verification requests and reports, and dev-notes.

- Inferences are allowed. **Inferences dressed as measurements are not**
- A symptom borrowed from a different process or product is an inference, not a measurement —
  "it uses the same API" is not "it has the same symptom"
- Writing an inference in the voice of a measurement destroys the document's value as a precedent

## Write the invariant, not the history

In code comments, prefer "what breaks if you change this" over "why this was done".
History belongs in dev-notes.md and tickets; duplicating it in code lets the two drift apart.

## Apply a principle to every surface in the same session

When a rule is established ("this call must not run on the UI thread",
"this must be released in a finally"), grep for the other places it applies and fix them now.
Writing the lesson down is not applying it — this has been broken in the very session
the lesson was written.

## Keep one source for each definition

A definition, a layout, or a list lives in exactly one file. Other files link to it
instead of restating it. Two copies drift apart, and nobody can tell which one is right.
