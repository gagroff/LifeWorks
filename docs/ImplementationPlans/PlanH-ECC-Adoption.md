# Plan H — Adopting ECC for AI-Assisted Development

**Context:** LifeWorks is developed with Claude Code CLI in VS Code. This plan adopts a small, hand-picked subset of [ECC](https://github.com/affaan-m/ECC) (pinned to release **2.2.3**, commit `b92f181`) to make that work faster and more reliable. It replaces the Superpowers plugin for this repo. It deliberately climbs the autonomy ladder one rung at a time:

| Rung | What the agent does | What you do | Phase |
|---|---|---|---|
| 0 | Nothing new | Put guardrails in place | Phase 0 |
| 1 | Plans, writes tests first, implements, verifies, reviews | Approve every plan, commit, open PRs, merge | Phases 1–2 |
| 2 | Does the whole loop on a branch and **opens the PR** | Approve the plan, review the PR, merge | Phase 3 |
| 3 | Runs unattended loops | — | **Out of scope** for this plan |

**How to use:** Work top to bottom. Each phase ends with a Verification step, and the next phase doesn't start until it passes. Use one PR per phase, targeting `main`, and CI must pass before merge. Day-to-day usage is documented in [`docs/AI-Dev-Workflow.md`](../AI-Dev-Workflow.md).

---

## Key Decisions

- **Hand-copy files instead of using the ECC plugin or installer.** Even ECC's "selective" installer pulls dependency clusters. Asking for 6 skills installs **77**, including HIPAA, DeFi and EVM-token skills, and the `csharp` language pack installs **170**. The plugin advertises the full ~311-skill catalog to the model. Copying ~10 files by hand keeps the context small, every file reviewable, and the setup committed to the repo and versioned.
- **Write LifeWorks commands instead of using ECC's commands.** ECC's `/plan`, `/build-fix`, `/quality-gate` and the `verification-loop` skill are written around npm/TypeScript (`npm run build`, `npx tsc`), and `/plan` writes to `.claude/plans/`. Thin `/lw-*` commands reuse ECC's *method* but run `dotnet`, and write plans to `docs/ImplementationPlans/` per CLAUDE.md.
- **Repo conventions override ECC's.** ECC's C# rules push `record` types, AwesomeAssertions, `WebApplicationFactory` and an 80% coverage gate. LifeWorks binds Blazor forms to mutable entity classes, tests with xUnit + NSubstitute + `Assert`, and has no coverage gate. A `lifeworks-overrides.md` rule file says which side wins.
- **The plan doc is the memory.** Session state lives in the checkboxes of the active `Plan*.md`, not in ECC's session files under `~/.claude/session-data/`. Any new session can resume from the plan doc, and the history is in git.
- **No hooks, no MCP, no continuous learning, no `multi-*` commands** during this plan. Each adds moving parts before the basics are proven.
- **`main` is production.** `deploy.yml` deploys to Azure on every push to `main`, so no agent ever pushes to or merges into `main`.

---

## Phase 0 — Guardrails (before any ECC files)

> **Goal:** CI is trustworthy, `main` is protected, and Superpowers is off for this repo. No AI tooling changes yet.

### 0.1 — Make CI green and meaningful

The Playwright tests call `http://localhost:5053`, but `ci.yml` runs `dotnet test` on the whole solution with no app running. That likely fails or hangs. `deploy.yml` already runs only the two unit-test projects.

- [ ] Check the last few runs on the Actions tab. Confirm whether CI is currently red, and why.
- [ ] Change `ci.yml` to run only the unit-test projects (match `deploy.yml`):
  ```yaml
  - name: Test
    run: |
      dotnet test tests/LifeWorks.Application.Tests --no-build --configuration Release --verbosity normal
      dotnet test tests/LifeWorks.Domain.Tests --no-build --configuration Release --verbosity normal
  ```
- [ ] Note in CLAUDE.md that Playwright tests are local-only and need the app running on port 5053.

### 0.2 — Protect `main`

GitHub → Settings → Branches (or Rules → Rulesets) → add a rule for `main`:

- [ ] Require a pull request before merging
- [ ] Require status checks to pass, and select **Build & Test**
- [ ] Block force pushes and deletions
- [ ] **Do not allow bypassing** (applies to you too, which is the point)

### 0.3 — Turn off Superpowers in this repo

- [ ] In Claude Code, run `/plugin` to find the exact Superpowers plugin ID and its install scope.
- [ ] Disable it for this project only. In `.claude/settings.json`, add `"enabledPlugins": { "<superpowers-id>": false }`, or use the `/plugin` UI.
- [ ] Remove the two `Skill(superpowers:...)` entries from `.claude/settings.local.json`.
- [ ] Fix `.claude/settings.json`. `additionalDirectories` points at a hard-coded `c:\code\LifeWorks\...` path *inside* the repo, which is redundant. Remove it unless it solves a real problem.
- [ ] Decide later, after Phase 2, whether to uninstall Superpowers globally. That affects your other repos.

### 0.4 — Record a baseline

- [ ] Write down how long a typical small change takes you today, and how many CI runs it usually needs. Phase 2 is judged against this.

### Phase 0 Verification

- [ ] A throwaway PR shows **Build & Test** passing, and the merge button is blocked until it does.
- [ ] A direct `git push origin main` is rejected.
- [ ] A new `claude` session in the repo does not list any `superpowers:` skills.
- [ ] Commit on branch `chore/plan-h-phase-0`, PR passes CI, merge.

---

## Phase 1 — Install the Minimal ECC Subset (Rung 1 tooling)

> **Goal:** About 10 ECC files plus 4 LifeWorks commands live in `.claude/`, are committed, and are loaded by Claude Code. Nothing runs automatically.

### 1.1 — Get the pinned ECC source (outside the repo)

```powershell
cd C:\code
git clone https://github.com/affaan-m/ECC.git ecc-2.2.3
cd ecc-2.2.3
git checkout v2.2.3   # or commit b92f181 if the tag name differs
```

- [ ] Skim each file below before copying it. You're adding instructions your agent will follow, so read them like code you're merging.

### 1.2 — Copy skills (loaded only when relevant)

Copy each folder into `LifeWorks/.claude/skills/`:

| ECC skill | Why |
|---|---|
| `skills/tdd-workflow/` | RED → GREEN → REFACTOR discipline with evidence |
| `skills/csharp-testing/` | xUnit/NSubstitute patterns (overrides apply) |
| `skills/dotnet-patterns/` | DI, async, and .NET conventions |
| `skills/security-review/` | Security checklist for review (auth, uploads, secrets) |
| `skills/search-first/` | Read existing code before writing new code |

- [ ] Copied, and `.claude/skills/` contains exactly these 5 folders.

### 1.3 — Copy rules (always loaded, so keep them few)

Into `LifeWorks/.claude/rules/ecc/`:

- [ ] `rules/csharp/` (whole folder: coding-style, patterns, security, testing, hooks)
- [ ] From `rules/common/`, only: `testing.md`, `security.md`, `code-review.md`, `git-workflow.md`

Skip the rest of `common/` (agents, hooks, performance, patterns, development-workflow). It's about the ECC harness, not this app.

### 1.4 — Add the LifeWorks override rule

Create `.claude/rules/lifeworks-overrides.md`. This file **wins over** anything in `rules/ecc/`:

- [ ] Domain entities are mutable classes bound directly by Blazor forms. Don't convert them to `record`s.
- [ ] Tests use xUnit + NSubstitute + `Assert` (unit) and NUnit + Playwright (E2E). Don't add assertion libraries (AwesomeAssertions/FluentAssertions) or Testcontainers without asking.
- [ ] No coverage-percentage gate. Every Application-service change gets tests, and so does every bug fix (a failing test first).
- [ ] Respect Clean Architecture boundaries in CLAUDE.md. Web → Application → (interfaces) ← Infrastructure, and timestamps are set in services.
- [ ] Migrations are only created with `dotnet ef migrations add`. Never hand-edit migration or snapshot files.
- [ ] `TreatWarningsAsErrors` is on. A warning is a failure, so never suppress one without saying why.
- [ ] Never edit `.github/workflows/deploy.yml`, `appsettings*.json` secrets, or `IdentitySeeder` credentials without explicit approval.
- [ ] Never commit to, push to, or merge into `main`.

### 1.5 — Write the four LifeWorks commands

In `.claude/commands/`. Each is a short Markdown prompt. Details of what each must do are in [`AI-Dev-Workflow.md`](../AI-Dev-Workflow.md#command-reference).

- [ ] `lw-plan.md`: restate the request, read the relevant code first (`search-first`), list risks, and write `docs/ImplementationPlans/Plan[Letter]-[Name].md` in this repo's checkbox format. **STOP and wait for approval.**
- [ ] `lw-tdd.md`: work the next unchecked plan item using `tdd-workflow`. Show the failing test output (RED) before implementing, then the passing output (GREEN). Tick the box.
- [ ] `lw-verify.md`: `dotnet build -c Release` (zero warnings), both unit-test projects, `git diff main...HEAD --stat`, a grep of the diff for secrets/connection strings, and a scan for leftover debug files. Output a PASS/FAIL table.
- [ ] `lw-review.md`: run a **fresh-context subagent** that sees only the diff, the plan, CLAUDE.md and the rules. It reviews for correctness, layer violations, missing tests, and `security-review` items, and reports findings by severity. It does not fix anything.

### 1.6 — Record provenance

- [ ] Create `.claude/ECC-VERSION.md` with the ECC version, the commit, the date copied, and the list of copied files. To upgrade later, diff the new ECC release against these files only.

### Phase 1 Verification

- [ ] A new `claude` session shows `/lw-plan`, `/lw-tdd`, `/lw-verify`, `/lw-review` in the `/` menu.
- [ ] Ask Claude: *"Which rules apply when you edit a .cs file here, and which wins on records vs classes?"* The answer cites `lifeworks-overrides.md`.
- [ ] Run `/context`. Rules plus skills add a modest amount, not thousands of lines.
- [ ] `/lw-verify` on a clean branch reports PASS.
- [ ] Commit on branch `chore/plan-h-phase-1`, PR passes CI, merge.

---

## Phase 2 — Supervised Loop (Rung 1, about 2 weeks)

> **Goal:** Prove that plan → TDD → verify → review is faster and safer than your baseline, using real but low-risk LifeWorks work. You approve every plan and do every commit, push, and merge yourself.

### 2.1 — Practice tasks (easiest first, one PR each)

- [ ] **Clean stray debug artifacts:** remove `tests/*.png` and `tests/*.html`, and add patterns to `.gitignore`.
- [ ] **Align test package versions:** `Microsoft.NET.Test.Sdk` (17.14.0 vs 18.5.1) and `coverlet.collector` (6.0.4 vs 10.0.0) differ across test projects.
- [ ] **Fix doc drift:** CLAUDE.md says MudBlazor 9.3.0 (the csproj has 9.4.0), and its Domain entity list is out of date. Also reconcile stale checkboxes in older plan docs, or mark those plans complete.
- [ ] **A small bug fix**, from your own notes, done test-first.
- [ ] **One real feature slice**, from PRD §4 (Future Phases) or the remaining items in `PlanDEF`. Keep it to one entity or page.

### 2.2 — Run every task through the loop

`/lw-plan` → you edit or approve → `/lw-tdd` (repeat per item) → `/lw-verify` → `/lw-review` → fix findings → **you** commit, push, and open the PR → CI → you merge.

### 2.3 — Keep a scorecard

For each task, note in the PR description: time spent, how many plan edits you made, how many `/lw-review` findings were real, and whether CI passed first try.

### Phase 2 Verification (decision gate)

- [ ] At least 5 tasks merged through the loop.
- [ ] At least 4 of 5 merged without major rework after review.
- [ ] `/lw-review` caught at least one real issue you'd have missed.
- [ ] **Decision:** keep this setup and go to Phase 3, adjust the commands and repeat Phase 2, or roll back (see the end of this plan).
- [ ] Decide about uninstalling Superpowers globally.

---

## Phase 3 — Agent Works on a Branch and Opens the PR (Rung 2)

> **Goal:** After you approve a plan, the agent runs the whole loop on its own branch and opens a PR. Your job becomes reviewing PRs, not watching sessions. **The agent never merges.**

### 3.1 — Prerequisites

- [ ] Install `gh` (GitHub CLI) and authenticate it on your machine (`gh auth status`).
- [ ] Use a separate working folder per agent task so it never touches your checkout:
  ```powershell
  git worktree add ..\LifeWorks-agent -b feature/<task-name>
  ```
- [ ] Add permission rules to `.claude/settings.json`. **Allow:** `dotnet build`, `dotnet test`, `dotnet ef migrations add`, `git` add/commit/push on non-main branches, `gh pr create`. **Deny:** pushes to `main`, `git push --force`, `gh pr merge`, edits to `.github/workflows/**`, and `appsettings*.json`. Check the rule syntax against the current Claude Code permissions docs, then test each deny rule by asking Claude to do it.

### 3.2 — Add `/lw-pr`

- [ ] `lw-pr.md`: requires a PASS from `/lw-verify` and a `/lw-review` with no unresolved high-severity findings. Then it pushes the branch and runs `gh pr create` with a body that includes a link to the plan doc, the verify table, review findings and how each was resolved, and anything left for you to check by hand (for example, "run Playwright locally"). It **never** runs `gh pr merge`.

### 3.3 — The Rung 2 flow

You write the request → `/lw-plan` → you approve (your only mid-task check-in) → the agent runs `/lw-tdd` through all items → `/lw-verify` → `/lw-review` → fixes → `/lw-pr` → you review the PR on GitHub and run Playwright locally if UI changed → you merge (which deploys).

- [ ] Optional second opinion: ECC's `/code-review <pr-number>` PR mode, if you copy `commands/code-review.md`.

### Phase 3 Verification

- [ ] 5 agent-opened PRs merged.
- [ ] Every deny rule was tested and held.
- [ ] Your review time per PR is less than the time the work would have taken you directly.
- [ ] **Decision:** stay at Rung 2 (likely the right steady state for a side project), or write a new plan to explore Rung 3.

---

## Out of Scope (revisit in a later plan)

ECC plugin/marketplace install · the hooks runtime · continuous learning / instincts · the memory vault · `multi-*` commands (they need the third-party ccg-workflow runtime) · `/astra-review` (it sends diffs to OpenAI) · autonomous loops · MCP servers · parallel agents.

## Rollback

Delete `.claude/skills/`, `.claude/rules/`, `.claude/commands/lw-*.md` and `.claude/ECC-VERSION.md`, then re-enable Superpowers in `.claude/settings.json`. Because nothing was installed globally, this repo is the only thing affected.
