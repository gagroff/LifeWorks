# AI Development Workflow for LifeWorks

How to build LifeWorks features with Claude Code CLI (in VS Code) and the small ECC subset installed by [Plan H](ImplementationPlans/PlanH-ECC-Adoption.md).

> **Status:** The `/lw-*` commands exist once Plan H Phase 1 is merged. `/lw-pr` arrives in Phase 3.

---

## The loop at a glance

```
 you describe it ──► /lw-plan ──► YOU APPROVE ──► /lw-tdd (per item) ──► /lw-verify ──► /lw-review ──► PR ──► CI ──► YOU MERGE (deploys)
                        │                              │                     │              │
                  writes PlanX.md              RED test, then GREEN     PASS/FAIL table   fresh-context
                  in docs/Implementation-      ticks the checkbox                         reviewer, findings
                  Plans/ and STOPS                                                         only, no fixes
```

There are two human gates, and they are the whole point: **approve the plan** and **merge the PR**. Everything between them is the agent's job, with evidence you can check.

---

## Which path to take

| Change | Path |
|---|---|
| Typo, copy text, one-line config | Just ask Claude directly. Run `/lw-verify` before committing. |
| Bug fix | `/lw-plan` (short) → `/lw-tdd`, starting with a **failing test that reproduces the bug** → verify → review |
| Feature, new page/entity, anything touching more than one layer | Full loop |
| Anything touching auth, uploads, Azure, migrations, or `deploy.yml` | Full loop. Read the plan's risk section carefully. |

When in doubt, plan. A rejected plan costs two minutes. A wrong implementation costs an hour.

---

## Step by step

### 0. Start clean

```powershell
cd C:\code\LifeWorks
git switch main; git pull
git switch -c feature/<short-name>
claude
```

Use one branch and one plan per task. Start a fresh `claude` session per task, so old context doesn't leak in.

### 1. Describe the work: `/lw-plan`

Give it a request in this shape. The better the request, the better the plan.

```
/lw-plan
Goal: Let me mark a maintenance task as skipped for one cycle without completing it.
Why: Seasonal tasks (e.g. pool opening) sometimes don't apply this year.
Done when: Skipping advances NextDueDate by one interval and logs a "Skipped" entry; the task list shows it.
Constraints: No new tables if avoidable. Follow existing MaintenanceService patterns.
Out of scope: Bulk skip, notifications.
```

Claude reads the relevant code first, then writes `docs/ImplementationPlans/Plan[Letter]-[Name].md` and **stops**.

**Your job:** read the plan like a PR. Check these things:
- Are the steps in the right layers? (Domain → Application → Infrastructure → Web)
- Does it need a migration? Is that called out?
- Is every behavior paired with a test step?
- Is anything in there you didn't ask for? Delete it.

Edit the file directly or tell Claude what to change. Then say **"approved"**.

### 2. Build it: `/lw-tdd`

```
/lw-tdd
```

Each run takes the next unchecked item and works RED → GREEN → REFACTOR:
1. Writes the test and **shows the failing output**. If it can't show a failure, the test isn't testing anything, so push back.
2. Implements the minimum code to pass it, and shows the passing output.
3. Refactors if needed, re-runs the tests, and ticks the checkbox.

Run it repeatedly, or say "continue through the remaining items, stop if anything fails twice."

### 3. Prove it: `/lw-verify`

```
/lw-verify
```

You get a table like this:

| Check | Result |
|---|---|
| `dotnet build -c Release` (warnings = errors) | PASS |
| Application.Tests | PASS (61) |
| Domain.Tests | PASS (8) |
| Diff scope (`git diff main...HEAD --stat`) | 7 files, all expected |
| Secrets / connection strings in diff | none |
| Stray debug files (png/html/tmp) | none |
| **Overall** | **PASS** |

Anything other than PASS means you fix it before review. If the change touched UI, also run Playwright locally. Start the app (`dotnet run --project src/LifeWorks.Web`), then in another terminal run `dotnet test tests/LifeWorks.PlaywrightTests`.

### 4. Get a second opinion: `/lw-review`

```
/lw-review
```

A **separate subagent** that didn't write the code reviews the diff against the plan, CLAUDE.md, the rules, and the security checklist. It returns findings marked **High / Medium / Low**, and it doesn't fix anything.

Then decide what to do with each finding. Tell Claude: "fix H1 and M2 with tests; M3 is intentional because ___; ignore the lows." Re-run `/lw-verify` after fixes.

### 5. Ship it

**Rung 1 (Phase 2):** you commit, push, and open the PR yourself.

```powershell
git add -A; git commit -m "feat(maintenance): allow skipping a cycle"
git push -u origin feature/<short-name>
gh pr create --fill
```

**Rung 2 (Phase 3):** `/lw-pr` pushes and opens the PR with the plan link, the verify table, and the review findings in the description. **The agent never merges.** You review on GitHub, CI must be green, and you click merge. Remember that **merging deploys to Azure.**

---

## Picking work back up

The plan doc *is* the memory. In a new session:

```
Read docs/ImplementationPlans/PlanX-Name.md and continue from the first unchecked item using /lw-tdd.
```

There's no need for transcripts or session files. If you stop mid-item, ask Claude to add a one-line note under that item before you close the session.

---

## Reviewing agent output: a 2-minute checklist

- [ ] Did it do **only** what the plan says? Scope creep is the most common failure.
- [ ] Are the tests asserting behavior, or just that "it ran"? Read at least one test closely.
- [ ] No layer leaks: Web doesn't touch `AppDbContext`, and Application doesn't reference Infrastructure.
- [ ] No suppressed warnings (`#pragma warning disable`, `<NoWarn>`) without a stated reason.
- [ ] Migrations were generated by `dotnet ef`, not hand-written.
- [ ] Nothing changed in `deploy.yml`, `appsettings*.json`, or seeded credentials.
- [ ] Debug files or temporary test hacks are gone.

---

## Command reference

These specs are what each `.claude/commands/lw-*.md` file must do. If a command drifts from its spec, fix the command file.

### `/lw-plan <request>`
- Read the relevant existing code first (ECC `search-first` skill). Name the files it read.
- Restate the goal, list assumptions and **risks** (migration? auth? breaking UI?), and estimate complexity.
- Write `docs/ImplementationPlans/Plan[NextLetter]-[Name].md` in the house format: Context, Key Decisions, Phases with checkbox items, and a Verification block per phase.
- Every behavior item gets a paired test item.
- **STOP. Make no code changes until the user says approved.**

### `/lw-tdd`
- Find the active plan (most recently modified `Plan*.md` with unchecked items, or ask).
- Take the next unchecked item and follow the ECC `tdd-workflow` and `csharp-testing` skills, under `lifeworks-overrides.md`.
- Show RED output, implement, show GREEN output, and refactor while keeping tests green.
- Run tests with the narrowest filter first (`dotnet test tests/LifeWorks.Application.Tests --filter <Class>`), then the full unit suite.
- Tick the checkbox. Stop and report if the same failure happens twice.

### `/lw-verify`
- `dotnet build -c Release`. Any warning is a failure.
- `dotnet test` for `LifeWorks.Application.Tests` and `LifeWorks.Domain.Tests`.
- `git diff main...HEAD --stat`, flagging files outside the plan's scope.
- Grep the diff for secrets, connection strings, keys, and `password` literals.
- Flag stray `*.png`, `*.html`, `*.tmp` and similar files outside `wwwroot`.
- Print the PASS/FAIL table. Don't fix anything, only report.

### `/lw-review`
- Launch a **fresh-context subagent** with only: the diff (`git diff main...HEAD`), the active plan, CLAUDE.md, `.claude/rules/`, and the `security-review` skill.
- Review for correctness against the plan, Clean Architecture violations, missing or weak tests, security (auth/authorization on pages, upload validation, open redirects, secrets), and EF Core issues (N+1 queries, missing `AsNoTracking` on reads, migration correctness).
- Output findings as `H1/M1/L1 — file:line — problem — suggested fix`.
- **No edits.**

### `/lw-pr` (Phase 3)
- Refuse unless the latest `/lw-verify` is PASS and no High findings are open.
- Push the branch. Run `gh pr create` with: the summary, a link to the plan doc, the verify table, the review findings with resolutions, and manual checks left for the human (for example, "run Playwright: UI changed").
- **Never** run `gh pr merge`, push to `main`, or force-push.

---

## Guardrails (always)

- **Never on `main`.** Agents work on feature branches only. Branch protection enforces this, and so do the deny rules in `.claude/settings.json` (Phase 3).
- **Merging deploys.** Treat the merge button as a production release.
- **Repo rules win.** `.claude/rules/lifeworks-overrides.md` beats the ECC rules. Classes rather than records for entities, xUnit + NSubstitute + `Assert`, no coverage-% gate.
- **Ask before:** new NuGet packages, new tables, auth changes, anything in `.github/`.
- **Don't add more ECC** (hooks, plugin, more skills) mid-task. Changes to the tooling get their own plan and PR.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Claude starts coding before you approve the plan | Say "stop, plan only". If it keeps happening, strengthen the STOP line in `lw-plan.md`. |
| It wants to convert entities to records or add FluentAssertions | The ECC rules are leaking through. Point it at `lifeworks-overrides.md`, and make that rule more explicit. |
| Tests pass but don't test the behavior | Ask: "show me the RED output for this test." If there isn't one, the test is redone. |
| Build fails only in CI | CI runs `-c Release` on Windows. Run the same locally: `dotnet build -c Release`. |
| Playwright tests fail | They need the app running on port 5053 and aren't part of CI. Start the app first. |
| Session got long and sloppy | `/clear` or start a new session, then resume from the plan doc. |
| Superpowers skills show up again | Check `/plugin`, and confirm it's disabled in `.claude/settings.json`. |
