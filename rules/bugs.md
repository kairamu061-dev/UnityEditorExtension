# Bug Tickets

Read this when a bug is reported, and before creating or updating a ticket.

The definitions of **state**, **rank**, and **type** are in the legend of `issues/index.md`.
This file covers how to use them.

---

## Workflow

1. Assign the next available ID by checking the highest existing number in `issues/tickets/`
2. Create `issues/tickets/BUG-{NNN}.md` using `issues/templates/BUG-template.md`,
   and set the **rank** from the symptom
3. Add a row to `issues/index.md` with state `Open`
4. Investigate the cause. **Do not start fixing yet**
5. Set the **type** from the cause. Revise the rank if the investigation showed the symptom
   is worse or milder than reported
6. Decide whether to fix now (see "Fix Decision")
   - Fix now → go to step 7
   - User decision → fill in 修正の影響 in the ticket, set state `Awaiting Decision`, and ask
     the user. Proceed to step 7 only if they choose to fix; otherwise set `Won't Fix` and
     record the reason
7. Fix the code. Update the ticket with the cause, fix details, and relevant commit hash;
   change state to `Fixed`
8. Commit the ticket file together with (or immediately after) the fix commit

---

## Rank

- Assigned from the **symptom** — what the user experiences, not what the cause turns out to be
- When in doubt, take the higher rank

## Type

- Assigned from the **cause**, after investigation
- If the cause is still 推測 (inferred), mark the type as 暫定 (provisional)
- **仕様**: update `spec.md` along with the code
- **外部**: the fix is a workaround on our side; record the upstream issue in the ticket
- **その他**: say in the ticket why it fits none of the other types

## Fix Decision

- **Fix now** if **any** of the following holds: rank **S**, rank **A**, or type **通常**
- **Otherwise the user decides.** Before asking, fill in 修正の影響 in the ticket:
  what the fix changes, what else it could break, other features it touches,
  rough effort, and what happens if it is left unfixed
- Do not start the fix while the ticket is `Awaiting Decision`

---

## Rules

- All bug tickets go in `issues/tickets/` — never in feature-area `tasks.md` or `dev-notes.md`
- One ticket per distinct root cause; link related tickets if they share a cause
- Do not close a ticket as `Closed` without user confirmation that the fix was verified
- Mark each of the symptom, the cause, and the impact as 測った or 推測
  (see `rules/principles.md`)
- **A step that verification could not complete is an open risk, not a closed question.**
  Record it in a ticket even when the cause is believed to be outside your code —
  the user cannot use the product if they cannot get past that step
