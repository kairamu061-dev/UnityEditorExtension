# BUG-{NNN}: {short title}

- **State**: Open
- **Reported**: {YYYY-MM-DD}（{who found it — user / verification session / code review / self}）
- **Area**: {affected feature area / file}
- **Rank**: {S / A / B / C}（set from the symptom when the ticket is created）
- **Type**: {通常 / 例外 / 競合 / 仕様 / 外部 / その他}（set from the cause after investigation; add「暫定」if the cause is still inferred）

## Summary

What is wrong, in one or two sentences.

## Reproduction

1. Step
2. Step
3. Observed result

<!-- ★ If this has not actually been reproduced, say so here and label the symptom
     as inferred below. A symptom borrowed from a different process or product is an
     inference, not a measurement — "it uses the same API" is not "it has the same
     symptom". Getting this wrong leaves a broken precedent behind. -->

## Expected

What should happen instead.

## Measured / Inferred

<!-- ★ Do not delete this section. Mark each of symptom, cause and impact. -->

| | 測った（根拠） | 推測（何が分かれば決まるか） |
|---|---|---|
| 症状 | | |
| 原因 | | |
| 影響 | | |

## Cause

Root cause, once investigated.

## Fix Decision

<!-- ★ Fill this in after setting the type, before touching the code (rules/bugs.md「Fix Decision」).
     Fix now if rank is S or A, or type is 通常. Otherwise fill in 修正の影響 and ask the user. -->

- **判定**: {即時修正 / ユーザー判断}
- **ユーザー判断**: {修正する / 修正しない — 日付と理由。即時修正なら「不要」}

### 修正の影響

<!-- Required when the decision goes to the user. Delete only for an immediate fix. -->

| 観点 | 内容 |
|---|---|
| 何を変えるか | |
| 壊しうるもの | |
| 触れる他の機能 | |
| 工数の目安 | |
| 直さない場合 | |

## Fix

What was changed and why.

- **Fix commit**: {hash}

### Other surfaces of the same principle

<!-- ★ Fill this in the same session as the fix, not later.
     grep for the other places the rule applies and list them with their state.
     Writing "next time I will grep" has failed before — in the very session it was written. -->

| Surface | State |
|---|---|
| | |

## Notes

Related tickets, follow-ups, or remaining risk.

<!-- If verification could not complete a step, record it here rather than closing the
     question — the user cannot use the product if they cannot get past that step. -->
