# development-team skill

A team of independent AI agents for software work: an orchestrator (the session you start the
skill in) with an architect, a security officer, backend and frontend developers, a sanity
agent and a tester, plus an optional analyst and designer when the task needs them. The instructions for the
AI are in [SKILL.md](SKILL.md). This file is for people: how to
install the skill.

The skill uses the open Agent Skills format (a folder with a `SKILL.md`), so it works with any
model provider in every environment that supports that format.

## Install

Copy the whole `development-team` folder (with `agents/` and `templates/`) to one of these
places.

| Environment | For all your projects | For one repository |
|---|---|---|
| Claude (desktop app, claude.ai, Claude Code) | Upload the zip in the Claude app under Customize → Skills, or copy the folder to `~/.claude/skills/` | `<repository>/.claude/skills/` |
| GitHub Copilot in VS Code | `~/.copilot/skills/` | `<repository>/.github/skills/` |
| GitHub Copilot in Visual Studio | `~/.copilot/skills/` or `~/.agents/skills/` | `<repository>/.github/skills/`, `.claude/skills/` or `.agents/skills/` |
| Other tools with Agent Skills | The tool's skills folder | The tool's skills folder (often `.agents/skills/`) |
| Tools without skills | Paste `SKILL.md` into the chat or the tool's instructions, and keep the folder next to it | |

## First run

Start the skill with its name (for example `/development-team`) or ask for a development team.
On the first run the orchestrator:

1. Asks to install the agent types (seven mandatory, two optional) in the environment's own format:
   - Claude Code: `~/.claude/agents/` or `<repository>/.claude/agents/`.
   - GitHub Copilot in VS Code and Visual Studio: `.github/agents/<agent type>.agent.md`, with the
     environment's own model names and tool names (Visual Studio also reads
     `%USERPROFILE%\.github\agents`).
2. Picks a model per role: the strongest reasoning model for the architect, the security
   officer, the code reviewer and the analyst, the main coding model for the developers, the sanity agent, the
   designer and the tester.
3. Records the environment in the team's `project.md`.

## The team folder

The team works in `.team/` in your repository. Every task, question, decision and requirement
is its own small file (`tasks/T-001.md`, `questions/Q-001.md`, `decisions/D-001.md`,
`requirements/REQ-001.md`), and each role writes its own document (`architecture.md`,
`testing.md`, `reviews/security-assessments.md`, ...). When agents run at the same time, each
locks the files it changes and releases them when done, so they never overwrite each other.
When they run one at a time, nobody locks, which saves terminal commands. `decisions.md` is the overview of everything: what waits for you, problems, the team
status, open tasks and questions, decisions and requirements. The script generates it; nobody
edits it by hand.

A team folder made with an older version of this skill (one large `decisions.md` with a Task
board) is converted once by the orchestrator, after your OK, with `-Migrate`.

## Solution types

Extra rules for a kind of solution, for example IoT, are in `solution-types/`, one file per
solution type. At kick-off the orchestrator proposes the solution types that fit, and copies the
ones you confirm into the team folder. To add a solution type, copy `solution-types/_template.md`,
fill it in, and upload the skill again. Rules for one organization or project belong in that
repository's own `CLAUDE.md` or `AGENTS.md`, not in a solution type.

## Short commands

Instead of long instructions, type a short command in the orchestrator's chat. Every team folder
gets this list as `.team/HELP.md`, and `team help` shows it in the chat.

| Command | What happens |
|---|---|
| `team help` | Shows the list of commands |
| `team start <request>` | Starts a new team task for the request |
| `team continue` | Continues the team: skill update check, repairs, status, next step |
| `team status` | Shows what is waiting for you and the status of every agent; changes nothing |
| `team next` | Starts the next role, or gives you the line for it |
| `team answer <question> <answer>` | Records your answer to a question (`Q-019` or `19`) and unblocks the agents waiting for it |
| `team check` | Checks the team folder, restores lost or damaged files, repairs errors, saves a checked copy |
| `team compact` | Moves finished tasks and closed questions to the archive and shortens the reviews |
| `team report` | Shows the project manager overview (`.team/report.md`): milestones, what waits for you, requirements, questions, work, quality and risks. While the team works: a new chat with `/team-report` (the team reporter, read-only) |

In a role's own chat, type `@<role> go`, for example `@architect go`, or `@backend-developer go 2`
for a second backend developer.

In GitHub Copilot (Visual Studio, VS Code) the commands without a value are also prompt files:
type `/team-help`, `/team-continue`, `/team-status`, `/team-next`, `/team-check`, `/team-compact` or `/team-report`. The
orchestrator copies them from `prompts/` into the repository's `.github/prompts/` after your OK.

## Team status and checks

Every team folder gets `check-decisions.ps1`, copied from `scripts/`. Run it in the repository
root to see how the team is doing and whether a team file is damaged:

```powershell
powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Overview
```

It prints errors (damaged or empty files, wrong status words, duplicate ids, decisions or
requirements lost compared with the latest checked copy, deadlocks where tasks wait for each
other) and warnings (stale locks, tasks that can be unblocked), and with `-Overview` a table of
every agent: working, ready, blocked and by whom, or stalled because it waits for answers, plus
the questions waiting for you, the current locks, and **How to proceed**: who to run next, in
which order, with the command and what each run unblocks.

The same script is the team's tool: `-Build` rebuilds the `decisions.md` overview, `-New`
creates a task, question, decision or requirement with the next free number, `-Lock` and
`-Unlock` lock a file, `-SaveOk` saves a checked copy to `.team/archive/` when there are no
errors (`-Build` also does this by itself at most every 10 minutes), `-Restore missing`
brings back lost or damaged files from those copies (or recreates an item from the register of
issued ids, `ids.log`), `-Compact` moves finished work to `.team/archive/done/`, and `-Migrate` converts an old
team folder.

## Environments without sub-agents (for example Visual Studio)

GitHub Copilot in Visual Studio cannot start sub-agents, so the orchestrator cannot start the
team members itself. Two ways to work:

- **Recommended:** run the skill in Claude Code on the same repository, for example in Visual
  Studio's terminal (`claude`) or in the Claude desktop app. The team runs automatically, and
  you review and edit the results in Visual Studio.
- **Manual mode in Visual Studio:** the orchestrator creates the tasks in `.team/tasks/` and
  tells you which roles to run. For each, open a new chat, select that role's agent, and type
  the line it gives you, for example `@architect go`. Start them in the order the orchestrator
  gives, because the first ones unblock the others. You choose once: **one chat at a time**
  (no locks, fewest terminal commands to approve; recommended: the architect, analyst, sanity
  agent and designer then get no terminal at all) or **several at the same time** (each role
  locks the files it changes, which costs more approvals). Developers whose tasks
  touch the same code files never run at the same time. Tell the orchestrator when chats are done; it reads
  the reports in the task files and gives the next lines. Use a new chat for every run, so each
  role keeps its own context and model.
- **Keep the changes, right away.** Visual Studio asks you to Keep or Undo what an agent
  changed. Choose Keep as soon as a chat is done (also for `decisions.md`, which the script
  rebuilds), and allow the team script when asked: an open Keep or Undo can stall the next
  step, and a late Keep can write an outdated or empty copy over a file.
- **Settings that save clicks.** Visual Studio cannot keep changes by itself, but it can approve
  the team script: in Tools > Options > All Settings > GitHub > Copilot > Tools, add a terminal
  command rule with pattern `check-decisions.ps1` and approve on. In VS Code,
  `"chat.editing.autoAcceptDelay": 5` keeps agent changes after 5 seconds; for VS Code agent
  sessions, choose the Assisted permission level with the team's instructions (see
  "Recommended settings" in `SKILL.md`). Do not use a chat mode that approves everything
  (Bypass, Autopilot or allow all).
- **Skill version.** `RULES.md` and every agent definition carry a `Skill version:` line. After
  you upload a new version of this skill, run `team continue`: the orchestrator regenerates the
  agent definitions in `.github/agents/`, and the check warns while they are outdated.

## Notes

- Custom agents in Visual Studio need version 18.4 or later and are in preview; their file
  format may change.
- Tool names differ between Claude Code, VS Code and Visual Studio. The skill describes what
  each role may do (read, search, write, run commands) and lets the orchestrator map that to the
  environment's tools.

## Sources

- [Use Agent Skills in VS Code](https://code.visualstudio.com/docs/agent-customization/agent-skills)
- [Custom agent file reference (VS Code)](https://code.visualstudio.com/docs/agents/reference/custom-agent-file-reference)
- [Use subagents in VS Code](https://code.visualstudio.com/docs/copilot/agents/subagents)
- [Use GitHub Copilot agents in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-specialized-agents?view=visualstudio)
- [GitHub Copilot in Visual Studio, April 2026 update](https://github.blog/changelog/2026-04-30-github-copilot-in-visual-studio-april-update/)
- [About agent skills (GitHub Docs)](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills)
