# Kickoff Runbook: PLC Emulator

Self-contained setup steps for starting this project from the
`=TEMPLATE=` repo. Assumes GitHub CLI (`gh`) is installed and
authenticated, and that a Claude subscription (Pro/Max/Team) is
available.

The fields below are filled out to provide baseline information about the project.
This file lives at the root of the project which was cloned from AGENT_TEMPLATE version 1.0


---

## Fill in before starting

| Field | Value |
| --- | --- |
| Project Name | |
| Client Name | |
| Preferred Software Programming Language | |
| Description of deliverable(s) | |
| General budget (time/tokens) | |

**Notes on specific fields:**

- **Client Name** — for an internal or self-directed project (no
  external client), your own name or "N/A" is fine. Solutions
  Architect and the Product Manager's interview still function the
  same way either way; "client" just means whoever's answering.
- **Description of deliverable(s)** — keep this to what's actually
  known and fixed, not everything you can guess at. In particular,
  leave target platform, IDE, and deployment environment out unless
  one is a genuine hard constraint — let the kickoff interview surface
  whether one exists, rather than assuming. Getting this wrong on an
  earlier project (assuming a Windows/Visual Studio requirement needed
  verifying on every single feature, rather than once at the end) cost
  real time and complexity; leaving it open by default is the
  corrected lesson, not an oversight.
- **General budget (time/tokens)** — this now has a concrete use: it's
  what decides the credential choice in step 2a below (time pressure
  vs. funding constraint). Beyond that one decision, it's not yet
  consumed automatically anywhere else in the pipeline — no role
  throttles work against it or queries it programmatically. If you
  want it to inform the project further, mention it directly during
  the kickoff interview in step 7; Product Manager can record it in
  memory as context even though it can't yet act on it algorithmically
  beyond credential choice. That gap is expected to close once
  usage-query and level-of-effort estimation are built into the
  Product Manager role — this field is placed here now specifically so
  nothing needs to be retrofitted when that happens.

---

## 0. Naming — PLC Emulator

The intended repo name doesn't collide with an existing local
folder or GitHub repo. The name was chosen based on the fundamental value of the target product.

---

## 1. Create the repository from `=TEMPLATE=`

On GitHub: open the `=TEMPLATE=` repo → **Use this template** →
**Create a new repository** → name it per the **Project Name** field
above → create.

Clone it locally:

```bash
cd C:\_Dev\GIT
git clone https://github.com/<your-username>/<Project Name>.git
cd <Project Name>
```

---

## 2. Secrets — none of these carry over from the template automatically

Go to the new repo's **Settings → Secrets and variables → Actions**.

### 2a. Choose a credential — decide this from your General Budget entry above

This is a genuine time-vs-money tradeoff, not just a convenience
choice, and the **General budget** field you filled in above is what
should decide it:

- **Ample funding, time pressure** → **`ANTHROPIC_API_KEY`** (separate
  API billing). Nothing throttles concurrent throughput the way a
  subscription's shared usage window does — many agents can run in
  parallel without competing with each other or with your own
  interactive use, which gets the project done faster. You pay for
  that speed directly.
- **Funding-constrained, time is more flexible** → **subscription**
  (`CLAUDE_CODE_OAUTH_TOKEN`, the default). Work proceeds within your
  existing subscription's usage window rather than incurring new
  cost — a deliberately slower burn, and the right choice when the
  budget itself is the binding constraint rather than the calendar.

Also worth weighing: the subscription avoids the budget-management
overhead (expiration dates, a separate balance to track and top up)
that caused real friction on an earlier project — a genuine point in
its favor even setting the time/money tradeoff aside. Reconsider
either default specifically if the project needs queryable,
programmatic usage tracking — that capability currently exists more
reliably for API-key billing than for subscription usage, and remains
an open question for how Product Manager's future throttling work
will actually query remaining capacity.

**If using the subscription (default):**

```
claude setup-token
```

Opens a browser, logs in with your subscription account, and prints a
token starting `sk-ant-oat01-...`. Copy it immediately — shown once.

Store as **New repository secret**: name exactly
`CLAUDE_CODE_OAUTH_TOKEN`.

**If using separate API billing instead:** generate a key at
console.anthropic.com → Settings → API keys, and store it as
`ANTHROPIC_API_KEY`. Then in step 6 below, make that the active
(uncommented) line instead.

Don't add both unless you specifically want a fallback — an unused
extra credential is harmless, but the active one should be
deliberate, not whichever happens to load first.

### 2b. `RELAY_TOKEN` — a fine-grained personal access token

GitHub → profile picture → **Settings** → **Developer settings** →
**Personal access tokens** → **Fine-grained tokens** → **Generate new
token**.

- Token name: `agent-relay-token` (or similar)
- Expiration: choose a long window, or no expiration — a short window
  caused a full, hard-to-diagnose account-wide outage on an earlier
  project when it silently expired mid-project
- Repository access: **Only select repositories** → this repo only
- Permissions: **Contents** (Read and write), **Issues** (Read and
  write), **Pull requests** (Read and write)
- Generate, copy the value immediately (`github_pat_...`, shown once)

Store as **New repository secret**: name exactly `RELAY_TOKEN`.

---

## 3. Confirm the GitHub App covers this repo

Settings → **GitHub Apps** → find the Claude app → **Configure**.
Confirm the new repo is included in its repository access.

---

## 4. Confirm Actions are enabled

Settings → **Actions → General** → confirm "Allow all actions and
reusable workflows" (or an equivalent allow-list) is selected.

---

## 5. Create the labels

```bash
gh auth login   # one-time, if not already authenticated
./scripts/setup-labels.sh
```

Confirm with `gh label list` — expect 22 labels, including
`agent:product-manager`.

---

## 6. Verify the credential line in `agent-relay.yml`

Open `.github/workflows/agent-relay.yml`, check the `with:` block
under the "Run Claude Code as..." step, and confirm it matches
whichever credential you chose in step 2a — exactly one of
`claude_code_oauth_token` or `anthropic_api_key` active
(uncommented), the other commented out. If it doesn't match:

```bash
git add .github/workflows/agent-relay.yml
git commit -m "Set active credential to match this project's choice"
git push
```

---

## 7. Submit the kickoff issue

On the new repo: **Issues → New issue → Project Kickoff**. This
auto-applies `agent:product-manager` the moment it's submitted.

In the "What are we building?" field, use the **Description of
deliverable(s)** value from the table above, expanded to full
sentences if it was kept brief there. If you're tracking a budget and
want it recorded as project context, mention it explicitly here too.

---

## 8. Where the interview happens, and how to answer it

The Product Manager's questions appear as a comment on the kickoff
issue — check the **Issues** tab, open the issue, scroll to comments.

To reply: **just write a plain comment** — no special mention syntax
needed. Comments don't trigger anything on their own; only labels do.
After posting your reply:

1. Open the issue's **Labels** section
2. Remove `agent:product-manager`
3. Immediately re-add `agent:product-manager`

That relabel is what wakes the agent back up to read what you wrote.
Repeat for as many rounds as the interview takes.

If a run seems to be taking a while, check the **Actions** tab for
current status before assuming something's wrong.

---

## 9. What "done" with kickoff looks like

Once scope is fully defined and confirmed, the Product Manager closes
the kickoff issue and opens a new one titled **"RTVM"**, labeled
`agent:systems-engineer` — expected behavior, not an error. That's
where requirements decomposition begins, and it proceeds on its own
from there.
