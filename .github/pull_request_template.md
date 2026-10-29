## What and why

<!-- What changed, why, and which principle it applies. Link the plan.md task. -->

## Verify

- [ ] `dotnet build --configuration Release` — 0 warnings, 0 errors
- [ ] `dotnet test --configuration Release` — all green
- [ ] `dotnet format --verify-no-changes` — clean
- [ ] `cd web && npm run lint && npm run build` — clean
- [ ] Behaviour change has a test that failed before the fix

## Clean code checklist

- [ ] Names reveal intent; no abbreviations; booleans read as questions (`IsOverdue`)
- [ ] Functions do one thing; ≤ 3 parameters (related ones grouped into a record)
- [ ] Command–query separation: commands return `void`/`Result`, queries change nothing
- [ ] No flag arguments; no `null` returns where an empty collection or `Result` fits
- [ ] No duplicated knowledge (rules, constants, formats live in one place)
- [ ] No commented-out code; comments explain *why*, not *what*
- [ ] Tests: Arrange / Act / Assert separated by blank lines; names describe behaviour

## Architecture

- [ ] Business rules only in Domain; Application orchestrates, Api endpoints stay thin
- [ ] No new project references beyond the architecture rules in `plan.md`
- [ ] No suppressed warnings, weakened tests, or secrets
