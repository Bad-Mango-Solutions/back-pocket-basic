# GitHub Project Integration

This repository uses a [GitHub Project](https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects) to plan and track emulator, `emudbg`, and agent-mode work.

## Project board

| Field | Value |
|-------|-------|
| **Organization** | [Bad-Mango-Solutions](https://github.com/Bad-Mango-Solutions) |
| **Repository** | [back-pocket-basic](https://github.com/Bad-Mango-Solutions/back-pocket-basic) |
| **Project** | [#3 — Back Pocket BASIC / Emulator](https://github.com/orgs/Bad-Mango-Solutions/projects/3) |
| **Machine config** | [`.github/project.config.json`](project.config.json) (owner, number, URL for tools and Copilot) |

Repository variables (already configured):

| Variable | Value |
|----------|-------|
| `PROJECT_NUMBER` | `3` |
| `PROJECT_OWNER` | `Bad-Mango-Solutions` |

> GitHub Actions variable names **cannot** start with `GITHUB_`; use `PROJECT_NUMBER` / `PROJECT_OWNER` instead.

## How work flows through the board

Recommended status columns (align with your existing board if already configured):

| Status | Meaning |
|--------|---------|
| **Backlog** | Accepted idea; not scheduled |
| **Ready** | Spec'd, repro exists, or acceptance criteria written |
| **In progress** | Branch open; assignee active |
| **In review** | PR open and awaiting review |
| **Done** | Merged or shipped |

### Linking issues and PRs

1. Create or update a **GitHub Issue** using the appropriate template (`emulator_*` or `basic_*`).
2. Add the issue to the project board (sidebar **Projects** on the issue, or drag on the board).
3. Reference the issue in commits and PRs: `Fixes #123` or `Relates to #456`.
4. Move the card when status changes; the [project automation workflow](workflows/project-automation.yml) auto-adds new issues and PRs to [project #3](https://github.com/orgs/Bad-Mango-Solutions/projects/3).

## Copilot and agent alignment

| Resource | Purpose |
|----------|---------|
| [`.github/copilot-instructions.md`](copilot-instructions.md) | Primary instructions for GitHub Copilot coding agent and Copilot Chat in this repo |
| [`AGENTS.md`](../AGENTS.md) | `emudbg` REPL / batch / MCP patterns for Cursor and other AI agents |
| [`PRD-emudbg-agent.md`](../PRD-emudbg-agent.md) | Product requirements for agent mode |
| [`emudbg-agent-plan.md`](../emudbg-agent-plan.md) | Technical plan for MCP / structured tools |
| [`.github/workflows/copilot-setup-steps.yml`](workflows/copilot-setup-steps.yml) | Environment setup Copilot runs before coding tasks |

When starting a Copilot task, point it at:

1. The **GitHub Issue** (acceptance criteria + repro steps).
2. The **project card** (priority and milestone).
3. **`AGENTS.md`** if the task involves boot, trace, or disk verification via `emudbg`.

### Suggested issue labels

| Label | Use |
|-------|-----|
| `emulator` | CPU, bus, devices, timing |
| `emudbg` | Debug console, commands, agent host |
| `basic` | Applesoft interpreter |
| `boot` | DOS / ProDOS / slot ROM boot path |
| `agent` | MCP, JSON output, headless |
| `good first issue` | Scoped starter tasks |

## Automation

The workflow [`.github/workflows/project-automation.yml`](workflows/project-automation.yml) adds **new issues and pull requests** to [org project #3](https://github.com/orgs/Bad-Mango-Solutions/projects/3).

### Repository variables (configured)

```text
PROJECT_NUMBER = 3
PROJECT_OWNER  = Bad-Mango-Solutions
```

Set via **Settings → Secrets and variables → Actions → Variables**, or:

```bash
gh variable set PROJECT_NUMBER --body "3" --repo Bad-Mango-Solutions/back-pocket-basic
gh variable set PROJECT_OWNER --body "Bad-Mango-Solutions" --repo Bad-Mango-Solutions/back-pocket-basic
```

### Required secret: `ADD_TO_PROJECT_PAT`

Organization-owned Projects require a PAT with **project** access. The default `GITHUB_TOKEN` in Actions **does not** have that scope.

**Projects are not covered by Issues permissions.** Fine-grained tokens expose **Organization permissions → Projects** as its own row (Read and write). Issues and Pull requests are separate repository permissions.

**One-time setup (repository maintainer):**

#### Option A — Fine-grained PAT (recommended if your org allows it)

1. Go to [Fine-grained tokens](https://github.com/settings/personal-access-tokens/new).
2. Set **Resource owner** to **`Bad-Mango-Solutions`** (the organization — **not** your personal account). This is why `gh project list` without `--owner` only shows user projects.
3. **Repository access:** select **Only select repositories** → `back-pocket-basic`.
4. **Organization permissions:** **Projects → Read and write**.
5. **Repository permissions:** **Issues → Read-only**, **Pull requests → Read-only** (per [actions/add-to-project](https://github.com/actions/add-to-project#creating-a-pat-and-adding-it-to-your-repository)).
6. If your org requires PAT approval: **Bad-Mango-Solutions → Settings → Personal access tokens → Pending requests** — an org owner must **Approve** the token before it works.

#### Option B — Classic PAT (simpler when fine-grained org UI is confusing)

1. Go to [Classic tokens](https://github.com/settings/tokens/new).
2. Scopes: **`project`** (required) and **`repo`** (required for private repos).
3. Classic tokens may work without org approval unless the org has restricted them.

#### Store the secret

Add the token as a repository secret:
   - **Settings → Secrets and variables → Actions → Secrets**
   - Name: `ADD_TO_PROJECT_PAT`
   - Value: the PAT

   ```bash
   gh secret set ADD_TO_PROJECT_PAT --repo Bad-Mango-Solutions/back-pocket-basic
   ```

3. **Merge and push** `.github/workflows/project-automation.yml` to `main` (workflow must exist on the default branch to run).

4. **Verify:** open a test issue; the **Project automation** workflow should succeed and the card should appear on [project #3](https://github.com/orgs/Bad-Mango-Solutions/projects/3).

### Optional: local `gh` CLI

Org projects require **both** the right token scopes **and** the `--owner` flag:

```powershell
# Refresh token scopes (classic PAT path)
gh auth refresh -h github.com -s read:project,project

# List org projects (not user projects)
gh project list --owner Bad-Mango-Solutions

# Open project #3 in the browser
gh project view 3 --owner Bad-Mango-Solutions --web
```

If `gh project list --owner Bad-Mango-Solutions` is empty or errors:

1. Token lacks `read:project` — run `gh auth refresh` above, or log in with a PAT that has `project` scope.
2. Fine-grained PAT not approved — check org **Pending requests** (org owner approval).
3. You are not a member of project #3 — open [project #3](https://github.com/orgs/Bad-Mango-Solutions/projects/3) in the browser and confirm you can see it; org owners can add you under project **Settings → Manage access**.
4. Resource owner was your **user** instead of **Bad-Mango-Solutions** when creating a fine-grained PAT — recreate the token with org as owner.

## Local verification commands

Copilot, agents, and humans should verify emulator changes with:

```powershell
dotnet build BackPocketBasic.slnx
dotnet test BackPocketBasic.slnx

# DOS boot smoke test
dotnet run --project src/BadMango.Emulator.Debug -- --profile pocket2e-a2-enh --headless --no-banner --exec "disk insert 6:1 library://disks/dos33-master.dsk; boot --immediate; pause; regs; trace dump 20; exit"

# ProDOS boot smoke test
dotnet run --project src/BadMango.Emulator.Debug -- --profile pocket2e-a2-enh --headless --no-banner --file diag_prodos.txt
```

## Related documentation

- [CONTRIBUTING.md](../CONTRIBUTING.md)
- [`.github/PULL_REQUEST_TEMPLATE.md`](PULL_REQUEST_TEMPLATE.md)
- [`.github/ISSUE_TEMPLATE/`](ISSUE_TEMPLATE/)