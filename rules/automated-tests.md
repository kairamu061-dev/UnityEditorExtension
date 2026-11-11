# Automated Tests

Read this when starting a project, and whenever deciding what to test automatically.

**Draw the line on day one.** Decide, before implementation starts, which code can be
tested without the target OS or hardware, and put those tests in CI from the first commit.

- Do not let "it is all UI / platform code" postpone the decision. Pure logic
  (formatting, argument parsing, serialization round-trips, path predicates, comparators,
  tree shapes) is usually a few thousand lines and is exactly where silent breakage lives
- Put the test step **before** the publish/package step so a failure stops the artifact
- Record the line in `docs/testing.md`: what is covered, what needs the target
  environment and therefore stays in the manual verification loop, and why
