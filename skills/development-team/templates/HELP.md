# Development team: commands

Type these in the chat. The orchestrator is the normal chat (Agent mode, no `@role` selected);
each role runs in its own new chat.

## In the orchestrator's chat

| Command | What happens |
|---|---|
| `team help` | Shows this list |
| `team start <request>` | Starts a new team task for the request |
| `team continue` | Continues the team: skill update check, repairs, status, then the next step |
| `team status` | Shows what is waiting for you and the status of every agent; changes nothing |
| `team next` | Starts the next role, or gives you the line to run it |
| `team answer <question> <answer>` | Records your answer to a question and unblocks the agents that wait for it |
| `team check` | Checks the team folder, restores lost or damaged files, repairs errors, saves a checked copy |
| `team compact` | Moves finished tasks and closed questions to the archive and shortens the reviews |
| `team report` | Shows the project manager overview: milestones, what waits for you, requirements, questions, work, quality and risks. While the team works, open a new chat (+) and type `/team-report`: the team reporter answers at once, without stopping the team |
| `/team-ask <question>` | Asks the team reporter in a new chat (+): an answer at once, also while the team works (how to start or sign in, where something is, how far the team is). Decisions and approvals stay in the team chat |
| `team standup` | The daily standup: the blockers, per role the tasks of today and what was done since yesterday. While the team works: a new chat (+) with `/team-standup` |
| `team stop` | Stops the team in an orderly way: nothing new starts, the running tasks are finished, committed and reviewed; then one question to you to approve the pull requests, and the approved ones are merged |
| `team emergency stop` | Stops every activity at once: no commit, push or merge; each running role stops at its next step and notes where it was. The chat's Stop button ends the current turn at once |
| `team continue` after a stop | Ends the stop and goes on from where the team was |

Examples: `team start An app to register property maintenance`, `team answer Q-019 5 years`
(`team answer 19 5 years` works too).

## In a role's own chat

| Command | What happens |
|---|---|
| `@<role> go` | The role first answers the questions addressed to it, then plans and does its open tasks |
| `@<role> go 2` | The same for the second instance of that role, for example `@backend-developer go 2` |

Roles: `architect`, `security-officer`, `backend-developer`, `frontend-developer`,
`code-reviewer`, `sanity-agent`, `tester`, and when they are in the team `analyst` and `designer`.

Start the role chats in the order the orchestrator gives, because the first ones unblock the
others. `project.md` says whether they run **one at a time** (no locks, fewest approvals;
recommended in Visual Studio) or **at the same time** (each role locks the files it changes).

**Keep the changes, right away.** Visual Studio asks you to Keep or Undo the changes an agent
made (in the editor and in the chat's list of changed files). Choose **Keep** as soon as a chat
is done, also for `decisions.md`, which the script rebuilds: an open Keep or Undo can stall the
next step, and a Keep given late, after other chats changed the same files, can write an
outdated or empty copy over a file. Choose Undo only to reject a
change, and then tell the orchestrator. When Visual Studio asks to run the team script
(`check-decisions.ps1`), allow it; the agents run it often.

**Settings that save clicks**
- VS Code: `"chat.editing.autoAcceptDelay": 5` keeps agent changes after 5 seconds.
- VS Code agent sessions: choose the **Assisted** permission level in the chat's permission
  picker and put the team's instructions in `chat.permissions.assisted.instructions` (the
  orchestrator gives you the text): routine build, test and git work then runs without a click,
  and deleting, merging and pushing to the base branch still ask you.
- Visual Studio: it cannot keep changes by itself, so choose Keep all after each chat. In Tools >
  Options > All Settings > GitHub > Copilot > Tools, add a terminal command rule with pattern
  `check-decisions.ps1` and approve on, so the team script runs without a click.
- Do not switch the chat to a mode that approves everything (Bypass, Autopilot or allow all).

## As `/` commands (GitHub Copilot in Visual Studio and VS Code)

`/team-help`, `/team-continue`, `/team-status`, `/team-next`, `/team-check`, `/team-compact`, `/team-report`, `/team-standup`, `/team-stop`, `/team-emergency-stop`, and `/team-ask <question>`.
They work once the orchestrator has copied them into `.github/prompts/` (after your OK).

## The team folder

- `decisions.md`: the overview of everything (generated; open it to see where the team is).
- `report.md`: the project manager overview: milestones, progress, quality and risks (generated).
- `standup.md`: the daily standup: blockers and the tasks of today (generated).
- `plan.md`: the delivery plan: what you get in which sprint, with your approval and the
  sprint reviews.
- `project.md`: the team, your request and the project environment.
- `tasks/`, `questions/`, `decisions/`, `requirements/`: one small file per item.
- `analysis.md`, `architecture.md`, `design.md`, `testing.md`, `reviews/`: the roles' documents.

## Check by hand

```powershell
powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Overview
```

Shows damaged or wrong files, locks, deadlocks and the status of every agent, without changing
anything. Add `-Build` to also rebuild `decisions.md`.
