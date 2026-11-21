# Rules for the development team

Skill version: 2026-11-20.10. Every agent definition shows the same version; when yours differs, your
instructions are outdated: say so in your report, and the orchestrator regenerates them.
Product rules last changed: 2026-11-20.8 (the rules about the requirements, the architecture,
security and the tests; a skill update re-checks the existing work only when this changes).

Every team member and the orchestrator obey these rules. They come before everything else: before
your role description and before the task you were given. If a task would break an absolute rule,
do not do that part. Stop and report the conflict to the orchestrator.

The solution-type rules in `solution-types/` add to these rules or make them stricter; they never
weaken them. When a solution-type rule seems to conflict with an absolute rule, follow the
absolute rule and report the conflict.

## Absolute rules

1. **Always start by reading.** Before any other action, at the start of every run, read:
   1. this file, completely;
   2. every solution-type file in `solution-types/` (if there are any), completely: both "All
      roles" sections and your own role's section apply to you;
   3. `decisions.md`, the team overview, completely;
   4. `project.md`, your task files, the question files addressed to you, and the documents,
      decisions and requirements your task relates to (the overview links to all of them).

   Do this even when you think you know what they say. Open every team file by its path
   (`.team/project.md`, `.team/tasks/T-006.md`; the overview links every item with its path).
   Never conclude that a team file is missing because a file or text search does not find it:
   searches skip the team folder when it is git-ignored or not part of the solution. Only when
   opening this file, `decisions.md` or `project.md` by its path fails, stop and report it.
2. **The team folder is the only shared context.** Everything another member needs goes into the
   files of the team folder. Nothing is passed on any other way.
3. **Private context stays private.** `agents/<instance name>/private.md` belongs to that instance
   alone. Never open, search, copy or change another member's `private.md`. Leave the other
   members' folders under `agents/` out of every search.
4. **Never assume.** When something you need is unknown or unclear, raise a question to the role
   that owns it, or to `user` when only the customer can answer. Do not fill the gap with a
   guess, and do not continue work that depends on the answer.
5. **Stay in your role.** Write only in the files you own (see "Who writes what"). Never delete
   or rewrite another member's file or text. Work that belongs to another role is a question to
   that role.
6. **Lock before you change**, when agents run at the same time (see "Locks"). Never change a
   file someone else has locked. Never edit `decisions.md`: the script generates it.
7. **Only the orchestrator** creates tasks, changes the status of decisions and requirements,
   changes `project.md`, and compacts the team folder.
8. **Nothing irreversible or outward without the user.** Never commit, push, create pull
   requests, deploy, apply infrastructure, delete files or live resources, or create or change
   tickets or documentation pages unless the user asked for it. Commits, pushes and pull
   requests are allowed only as "Version control" in `project.md` records the user's OK, and
   then only as "Version control" below describes: never merge, never push to the base branch
   (only the orchestrator merges a pull request, after the user approved it in the chat),
   never force-push, never rewrite history.
9. **No secrets** in code, configuration, logs, team files or `private.md`.
10. **Project rules apply.** Follow the target repository's own rules (for example CLAUDE.md,
    AGENTS.md, .github/copilot-instructions.md). When they conflict with these rules, follow the
    stricter one and report the conflict.
11. **Report the truth.** Report test results, findings and outcomes as they are, including
    failures and anything you did not do.
12. **Authentication and authorization are never skipped or weakened.** Every request is
    authenticated and authorized in the backend, deny by default, following the authorization
    matrix in the architecture. Never add a public endpoint, a bypass, a broader permission or a
    shortcut for testing unless it is an accepted decision that the security officer reviewed.
13. **Requirements before design and build.** Nothing is designed or built before the user has
    approved the requirements, and nothing is built that no approved requirement asks for.
14. **Within the requirements, every agent, every task.** Before you start a task, propose a
    decision or add anything (code, tests, tools, documents, checks), check it against the
    approved requirements: it must serve one, directly (`for: REQ-002`), through a decision
    that names the requirements it serves (`requirements: REQ-002, REQ-009` in its header), or
    as a bug against one (its row in `testing.md` names the requirement). Team-internal work
    that changes nothing in the product (`for: team`, for example recording answers) is
    allowed. Anything else, however useful it looks (an extra feature, a refactoring, more
    tests, a new tool), you do not do: raise a question to `user` that says what it is, why it
    is needed and what it costs, and do it only after the user approved it (as a requirement).
    The script refuses a new task outside the requirements, and the check lists every
    unfinished task and proposed decision outside them: never work on one of those.
15. **The current sprint, per requirement, working results early.** Work only on the
    requirements of the current sprint in `plan.md`, one requirement or use case at a time, as
    a team: backend, screen and tests of the same requirement together, until the user can see
    it work end to end and its tests pass. Sprint 1 ends with a working first screen. Never
    start work of a later sprint; the check lists such tasks. One exception: the architect,
    analyst, designer and sanity agent may refine the requirements of the **next** sprint
    (requirements, design, decisions, questions), so it can start at once; nobody builds code
    or tests for it before it is the current sprint.
    **Every sprint's demo has an automated end-to-end test** (the demo test): it walks the
    demo's steps in `plan.md` through the real screens, the real API and a real database, signs
    in like the user does, in every language the demo switches to, and runs with the project's
    normal test command. The tester writes it with the sprint's first requirement and extends it
    with each next one. The sprint goes to its review only when the demo test passes; it stays
    as a regression test and is never on the cut list. A manual walkthrough adds to it and
    never replaces it.
16. **Every file you create stays inside the repository folder.** Writing anywhere else
    (`%TEMP%`, `$env:TEMP`, `/tmp`, your user profile, the Downloads folder, another
    repository) makes the environment ask the user for access, and that question stalls your
    run. So:
    - Scratch files, command output, logs, test results, screenshots and downloads go in
      `.tmp/` in the repository root, also when you work in a worktree:
      `dotnet test --results-directory .tmp/TestResults`, `--output .tmp/<name>`. Never
      leave them loose in the repository root or in a project folder.
    - Worktrees go in `.worktrees/`, team files in the team folder, code where the
      architecture puts it.
    - When a tool writes outside the repository by default, give it a path in `.tmp/` with
      one of its options (`--output`, `--results-directory`, `-o`), in the same single command.
      When it has no such option, ask the orchestrator before you run it. Package caches that a
      build fills by itself (NuGet, npm) are fine.
    - Create a file there with your edit tool (it creates the folder too); a folder alone with
      `New-Item -ItemType Directory .tmp/<name>`.
    - `.tmp/` and `.worktrees/` are git-ignored: never commit them. Never delete files to tidy
      up (rule 8): what stays in `.tmp/` does no harm.
    - The only exception: a place outside the repository that the user chose for this purpose
      (for example agent definitions at user level, `~/.claude/agents/`), written by the
      orchestrator during the team setup.
17. **As few outside libraries and tools as possible, for security.** Use what the language,
    the framework, the SDK and the platform already provide (for .NET: the SDK's own analyzers,
    `dotnet format`, `dotnet list package --vulnerable`, Microsoft's own test and coverage
    packages). An outside library, package or tool is added only through a decision that says
    why the platform cannot do it, who maintains it, its licence and its known vulnerabilities;
    the security officer reviews that decision before anyone adds it. Never a paid tool, and
    never install anything for a check or a review: use what is already there.

## The team folder

| File | What it is | Who writes it |
|---|---|---|
| `decisions.md` | The overview: waiting for the user, problems, team status, open tasks and questions, decisions, requirements, documents. Generated by the script | nobody by hand |
| `report.md`, `standup.md` | The project manager overview and the daily standup (blockers, the tasks of today). Generated by the script | nobody by hand |
| `stop.md` | Only while the team is stopped: `mode: finish` (team stop) or `mode: now` (emergency stop), since when, by whom, why (see "Stop") | the script (`-Stop`, `-Resume`), for the orchestrator |
| `project.md` | Team, customer request, project boundaries, project environment, sprint length | orchestrator |
| `plan.md` | The delivery plan: the sprints with their goal, requirements, demo and status, the user's approval, the sprint reviews | orchestrator (the architect estimates) |
| `tasks/T-NNN.md` | One task: header, Task, Plan, Report | orchestrator creates it; its owner writes status, `waits-for`, Plan and Report |
| `questions/Q-NNN.md` | One question: header, Question, Answer | anyone creates one; the addressee answers and closes it (the orchestrator for `user`) |
| `decisions/D-NNN.md` | One decision: header, Decision, Why | its author; only the orchestrator changes its status |
| `requirements/REQ-NNN.md` | One requirement: header, Requirement, Acceptance criteria | architect; only the orchestrator changes its status |
| `analysis.md`, `architecture.md`, `design.md`, `testing.md` | The documents of the analyst, architect, designer and tester; a long one keeps its details in linked files in a folder of the same name (`architecture/database.md`) | that role |
| `reviews/security-assessments.md`, `reviews/sanity-reviews.md`, `reviews/code-reviews.md` | The reviews | security officer, sanity agent; code reviewer and architect (each only their own reviews of a pull request) |
| `agents/<instance>/private.md` | Your private notes | you alone |
| `ids.log` | Every id ever issued, one line each: id, time, who, title. A number is never issued twice, and a lost item is always noticed | the script (`-New`, `-Build`); you add a line only when you create an item by hand |
| `archive/` | Checked copies (`-ok`, and every 10 minutes `-auto-ok`), damaged copies kept as evidence, and in `archive/done/` the finished tasks and closed questions, plus `removed.md` with the numbers that are gone on purpose | orchestrator; read `archive/done/` when you need the history |

Every item file starts with a header between two `---` lines, one `key: value` per line, for
example:

```
---
id: T-006
title: Upgrade the API project to .NET 10
owner: backend-developer-1
status: Blocked
waits-for: T-005, Q-024
created: 2026-11-06
---
```

Change only the values. Keep the `---` lines, the keys and one line per key.

A title says in at most 80 characters what the item is (`-New` refuses a longer one for a task,
decision or requirement); the details go below it (`-Text`). Every agent reads the overview
with all titles at the start of every run, so a long title costs every run.

**Status words**
- Tasks: `Open`, `In progress`, `Done`, `Blocked`.
- Questions: `Open`, `Closed`.
- Decisions: `Proposed`, `Accepted`, `Rejected`, `Superseded by D-NNN`.
- Requirements: `Proposed`, `Approved`, `Won't`.

## Commands

**Use the terminal as little as possible.** In some environments (for example GitHub Copilot in
Visual Studio) every terminal command waits for the user's approval, so each one can stall you.
- Read and search files with your read and search tools, never with terminal commands
  (`Get-Content`, `Select-String`, `Get-ChildItem`, `cat`, `grep`).
- Create and change files with your edit tools, never with terminal commands (`Set-Content`,
  `Out-File`, `WriteAllText`, here-strings, redirection), and never with a script file you
  write and run: such a script once deleted eight team files at once.
- Look up web pages, documentation and package versions with your web tool, never with
  `Invoke-RestMethod`, `Invoke-WebRequest`, `curl` or `wget` in the terminal.
- Use the terminal only for the team script below, for the git commands of "Version control",
  and, for developers and the tester, to build, format (see "Commits") and test the code. Never improvise other
  commands (`git apply`, `git stash`, a worktree of your own, deleting files): each one waits
  for the user's Allow click. When a step seems to need one, ask the orchestrator instead.
- Some roles have no terminal at all (see "Without a terminal"); they need none.

**Never run a command that does not end by itself.** A command that waits (for input, for an
Allow click, or forever) stalls your run, and nobody can stop you from outside.
- Servers, `docker compose` and watchers only detached, with a wait limit:
  `docker compose up -d --wait --wait-timeout 300`; never `docker compose up` without `-d`,
  `dotnet run`, `dotnet watch` or `npm start` in the foreground. Start an app you test in the
  background and stop it when you are done.
- Every test run has a hang limit: `dotnet test --blame-hang-timeout 5m`; browser tests with
  their own timeouts (Playwright `Timeout`), headless.
- Never an interactive command (a prompt, a pager, an editor): add `--yes`, `--no-pager`,
  `-NonInteractive` and the like.
- When a command has not returned after 10 minutes, stop it, write in your task what hung, and
  end your run with the task `Blocked` and the reason.

The script is in the team folder. Run it from the repository root (with another team folder,
use that folder's path):

| What | Command |
|---|---|
| Check and rebuild the overview | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Build` |
| Lock the files you are about to change (a document in a folder by its path or its file name: `architecture/build-tasks.md` or `build-tasks.md`) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Lock "T-006, Q-024, architecture.md" -Owner <your instance>` |
| Unlock all your files and rebuild, at the end of your run | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Unlock all -Owner <your instance> -Build` |
| New question | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -New question -From <your instance> -To <role or user> -Title "<the question>"` |
| New decision (title at most 80 characters, details with -Text) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -New decision -Owner <your instance> -Title "<what is decided>" -Text "<details>"` |
| New requirement (title at most 80 characters) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -New requirement -Title "<the requirement>" -Text "<details>"` |
| New task (orchestrator; only within the approved requirements: -For REQ-nnn, D-nnn, BUG-n or team; one task per item and role; title at most 80 characters) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -New task -Owner <instance> -Title "<the task>" -For REQ-002 -Text "<details>" [-WaitsFor "T-005, Q-024"]` |
| A new round of an existing task: the next step, a fix, a re-review (orchestrator) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Reopen T-033 -Text "<what to do now>"` |
| Team status | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Overview` |
| Daily standup: what waits for the user, the blockers, per role the tasks of today | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Standup` |
| Restore lost or damaged files (orchestrator) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Restore missing` |
| Update the rules, script and agent definitions from the skill (orchestrator, skill update) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -UpdateFromSkill "<skill folder>"` |
| Answer a question: writes the answer, closes it, unblocks the tasks that waited only for it | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Answer Q-039 -Text "<the answer>" -Owner <your instance>` |
| Change statuses (orchestrator) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -SetStatus "D-021=Superseded by D-026, D-026=Accepted"` |
| Set tasks whose waits are all finished back to Open (orchestrator) | `powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Unblock` |

For these routine edits, always use the script, never a script, loop or inline PowerShell of
your own. Each command respects the locks of others and rebuilds the overview.

`-New` takes the next free number, also when several agents create items at the same moment,
writes it to `ids.log`, and prints the file it created. Never pick a number yourself, except as
"Without a terminal" describes.

**Without a terminal.** When your agent definition has no terminal tool (in one-at-a-time mode
the architect, analyst, sanity agent and designer have none), you work only with your read,
search and edit tools:
- Create a new item by hand: take the next number after the highest one in its folder, in
  `archive/` and in `ids.log`; write the file with the same header and sections as the other
  items of its kind; then add its line at the end of `ids.log`
  (`Q-039 | 2026-11-09 16:05 | architect -> user | <title>`).
- Do not lock and do not run the check; the orchestrator runs it after your chat.
- Never ask for a terminal and never work around it.

**Run the commands exactly in this form**: one command per call, on one line. No `cd` before
it (when the terminal does not start in the repository root, use the script's full path
instead of `.team/check-decisions.ps1`), nothing chained with `;`, `&&` or `|`, no loops, no
multi-line commands and no inline scripts. Then the user can allow the team script once, and a
command never hangs in the terminal. In PowerShell, write every command so that VS Code can
read it; a command line it cannot read always waits for the user's Allow click, whatever the
approval rules say:
- An option and its value with a space, never with `=`: `--wait-timeout 300`, not
  `--wait-timeout=300`; `--format '%(trailers)'`, not `--format=%(trailers)`.
- No bare `--`: `git add PropertyAdmin.Api/a.cs PropertyAdmin.Tests/b.cs`, not
  `git add -- ...`; `git checkout -- <file>` is not allowed anyway.
- An argument with `(`, `)`, `{`, `}`, `$`, `@` or `;` in single quotes (`'%(trailers)'`):
  unquoted, PowerShell runs the part in parentheses as a separate command, which can change what
  the command does.

**A run needs few script calls:** a `-New` for each new item, one `-Lock` for the files of a step
when agents run at the same time, and one `-Unlock all ... -Build` (or only `-Build`) at the end.
Do not run the check after every single change.

When you have a terminal but a command cannot run, say so in your report; the orchestrator then
creates, locks and checks for you.

## Locks

`project.md` says under **Agents at the same time** whether several agents run at the same
time.

- **No (one agent at a time):** you are the only one changing team files. Do not lock; change
  the files and run `-Build` once at the end of your run.
- **Yes (or not recorded):** lock before you change, all files of one step in one call
  (`-Lock "T-006, Q-024, architecture.md"`). You get all of them or none.
- **Locked by someone else** (the lock command says `LOCKED` and ends with exit code 2): do not
  change those files. Work on something else and try again later. Never delete a lock file.
- **Release your locks** as soon as that step is done, and always at the end of your run:
  `-Unlock all -Owner <your instance> -Build`. Never keep a lock while you build, test or
  research.
- A lock older than 30 minutes is stale: the next lock command takes it over. The orchestrator
  releases stale locks of agents that are no longer running (`-Unlock <id> -Force`).
- Locks are for the team folder. In the code, the architect's task list keeps parallel work in
  different files.

## Version control

Only when "Version control" in `project.md` records the user's OK for commits (and for pull
requests). Every change must be traceable for a person: **what** changed, **why**, and **who**
(which agent, for which task) did it.

**One branch and one pull request per item.** Every task names the item it is for in its
header (`for: REQ-002`, a requirement; `for: BUG-3`, a bug in `testing.md`; or `for: D-026`, a
decision that changes existing code). All code of that item goes on its own branch, named as
`project.md` says (for example `feature/REQ-002-manage-owners`, `bug/BUG-3-...`,
`change/D-026-...`), and into pull requests to the base branch: one open at a time, ready after
every finished task, so the user approves the item in steps.

**Commits: small, per agent, per task.**
- Before your first change to the code: switch to the item's branch (`git switch <branch>`; the
  orchestrator creates it). Never commit on the base branch.
- Commit **one logical step at a time**: a change that builds and whose tests pass, that a
  reviewer can understand on its own (as a guide, at most a few hundred changed lines). Never
  mix two tasks, or a restructuring with a change in behaviour, in one commit; put generated
  files (for example database migrations) in their own commit.
- **Format the files of the step before you commit**, so formatting never becomes a review
  round. For .NET, from the repository root, with every path counted from the repository root
  (a path counted from the project folder silently formats nothing):
  `dotnet format "<solution>" --include PropertyAdmin.Api/a.cs PropertyAdmin.Tests/b.cs`; in a
  worktree `dotnet format ".worktrees/REQ-026/<solution>" --include .worktrees/REQ-026/PropertyAdmin.Api/a.cs`.
  It changes only the files you name. For another stack, its own formatter the same way.
- Commit only your own changes (`git add` the files of this step, never `git add -A` of
  everything), never secrets, never the team folder when it is git-ignored, never another
  agent's work.
- Every commit message names what and why on its first line, and who and what for in trailers:

  ```
  T-033 Owners: GetAll and GetById as V1 use cases

  Moves the owner queries out of MasterDataControllers into V1/Owners, with
  request, response and mapping per use case. Behaviour unchanged: the
  characterization tests stay green (69 passed).

  Task: T-033
  For: REQ-002
  Ticket: PROJ-123
  Decisions: D-026
  Agent: backend-developer-1
  ```
  `Ticket:` is the ticket of the item (its `ticket:` header, or the Ticket column of the bug in
  `testing.md`); leave the line out when the project has no ticketing system.
- Push your branch after your commits (`git push -u origin <branch>`), never `--force`.
- Record the commits in your task's report: `Commits: 3f2a1c9, 8b7e0d4 on feature/REQ-002-...`.

**Pull requests** are opened and kept up to date by the orchestrator, one open per item, with
this description, so a person sees at a glance what, why and who:
- **What:** the item and what changed, per area (files, endpoints, screens).
- **Why:** the requirement, bug or request in full, its acceptance criteria, and the decisions
  with their reasons.
- **Who:** per task: the task, the agent that did it, and its commits; the reviews (code
  reviewer, architect, security officer, sanity agent, tester) with their results; the user's approvals with their dates.
- **How it was checked:** the test runs (passed, failed, skipped) and what was not tested.
- **Links:** the ticket of the item (in the title too: `[PROJ-123] [REQ-002] Manage owners`), and
  the item, task and decision files.
- **Still to come:** the item's tasks and reviews that are not in this pull request yet.

The user approves every pull request in the chat; only then the orchestrator merges it, with a
merge commit, so the item's branch can go on. Nobody else merges.

**One folder per item when items run in parallel (git worktree).** One working folder has one
branch checked out, so two developers on different items cannot share it. Then the orchestrator
gives the item its own folder with its branch checked out, a git worktree under `.worktrees/`
(for example `.worktrees/REQ-026` on `feature/REQ-026-hosting-costs`), and records it in the
item's header (`worktree: .worktrees/REQ-026`). When your task's item has a `worktree:`:
- Make every code change there: edit the files by their paths inside that folder
  (`.worktrees/REQ-026/PropertyAdmin.Api/...`), never the same files in the main folder.
- Run git, build and test commands on that folder without `cd`: `git -C .worktrees/REQ-026 add
  <files>`, `git -C .worktrees/REQ-026 commit ...`, `dotnet test .worktrees/REQ-026/<tests>.csproj`.
- The team folder and the team script stay in the main folder: keep using
  `.team/check-decisions.ps1` and `.team/...` paths for team files.
An item without `worktree:` works in the main folder, on the branch checked out there.

**Share the item's folder; never build or test in a copy.** Several agents may change the same
folder at once (the backend, the screen and the tests of one item). Never move your work
elsewhere to build or test it apart: no worktree of your own (only the orchestrator creates
worktrees, one per item), no `git stash`, no patch files, no `git apply`, no
`git worktree remove`, no `git checkout` or `git restore` of other files. Each of those waits for
the user's Allow click, and the copies pile up. Build and test in the item's folder. When the
build or a test fails only because of another agent's unfinished change, write under
`## Progress` which files and whose task, end your run (see "Short runs"), and run the tests
again in your next run.

## How to work

**Your task**
- Your prompt gives you the team folder, and may give your instance name and your task.
- **Short command `go`.** A prompt that is only `go` means: the team folder is `.team`, you are
  the default instance of your agent type, you **first answer the open questions addressed to
  you**, and then do your next open task (one task per run, see "Short runs"). `go 2` makes you instance 2 of your type (for example
  `backend-developer-2`).
- No instance name in the prompt: you are the default instance of your agent type (for example
  `architect` or `backend-developer-1`).
- No task in the prompt: first answer the open questions addressed to you (to your instance or
  your role), then do your `Open` tasks in order of number. A task in the prompt is also a task
  file; use that file.
- **One task per item and role, in rounds.** A developer's or the tester's task covers exactly
  one requirement (or one bug, or one change request), never several. The next step of that
  item, a fix after a review or a bug, and a re-review are new rounds of the same task
  (`### Round 2, <date>` under `## Task`, added by the orchestrator with `-Reopen`): do the
  latest round; the earlier ones are its history. Report each round under `## Report`, starting
  with `Round 2:`. Other roles may get a bigger task that covers several items of the sprint
  (for example the sanity agent's sprint check, or the architect's design of the sprint).
- **Plan first, with all your questions.** Before you start a task, work out the whole task and
  write a short plan under `## Plan` in the task file: the steps, what you need, and every
  question you have, or "no questions". Raise all those questions at once (`-New question`)
  before you start, so you do not stall halfway on a question you could have asked at the
  start. Then do the parts that do not depend on an unanswered question, and set the task to
  `Blocked` with `waits-for` when the rest depends on one.
- Set a task to `In progress` when you start it, and to `Done` or `Blocked` when you finish, or
  back to `Open` when your run ends before the task is done (see "Short runs").
- A `Blocked` task names exactly what it waits for in `waits-for`: task, question and decision
  ids, or `user`, for example `waits-for: T-005, Q-024`. Never block on something vague.
- Review `Proposed` work; never wait for a decision to be accepted before you review it.
  Acceptance comes after the review, not before.

**Stop.** At the start of your run and before every step (at least at every heartbeat line),
open `stop.md` in the team folder by its path. When it does not exist, go on. When it does:
- `mode: finish` (team stop): start no new work. Finish the task you are working on now (its
  current round) as usual, commit and push it, set it to Done, release your locks and end your
  run. Take no other task; answering a question addressed to you is still fine.
- `mode: now` (emergency stop): stop at once. No commit, push, merge or other command. Write
  one line under `## Progress` (what is done, what is not, which files are changed and not
  committed), set your task back to `Open`, release your locks
  (`-Unlock all -Owner <your instance>`), and end your run with the report
  `Stopped: emergency stop`. Leave changed files as they are.
The script prints the stop at every call and refuses `-Lock`, `-New` and `-Reopen` during an
emergency stop. Never remove `stop.md`: only the orchestrator ends a stop.

**Short runs: one task, at most 10 minutes**
- Work in this order: (1) answer the open questions addressed to you; (2) your tasks that other
  tasks wait for (your task id appears in their `waits-for`); (3) your other open tasks, in
  order of number.
- A run is **one task, and at most about 10 minutes**. Then end it: the orchestrator starts you
  again at once, and sees in between what you did. Questions to you count as part of the run.
- **Heartbeat.** While a task is `In progress`, add a line under `## Progress` in its file after
  every step, at least every 5 minutes: `- 13:42 built the owner endpoint, tests run next`.
  Only you write the Progress section of your own task, so it needs no lock. `In progress`
  means that you run on it now: a task file without a change for 5 minutes shows the team that
  your run hangs.
- **End of a run without finishing the task:** write under `## Progress` what is done and where
  to continue (`- 13:50 next: the validation tests`), set the task back to `Open` (or `Blocked`
  with what it waits for), release your locks and end with your report. The next run continues
  from there and from your `private.md`.
- **Unblock others first.** When another agent waits for you, spend your time on that before
  your own work. When you cannot finish it, give what lets them continue: an answer, the
  interface contract, the part they need. In your report, name what you unblocked
  (`Unblocks: T-006, T-008`).

**Prevent deadlocks**
- Do not block on something you can find out yourself (in the code, the documentation or the
  team folder): find it out.
- Before you set a task to `Blocked`, follow what you wait for. When it waits, directly or
  through others, for you, that is a deadlock: do not block. Do the part that breaks the circle
  (often the smallest step of one side), or raise a question to the orchestrator with the
  circle (`T-006 -> T-008 -> T-006`), and go on with your other work.
- Never wait for a task that is `Done` or a question that is `Closed`: take that wait out.

**Questions**
- Ask with `-New question`: one question per file, the question in one line as the title. Add
  details under `## Question` (nobody else knows the new file yet, so no lock is needed).
- `-To` is a role (`architect`), an instance (`backend-developer-2`) or `user`.
- To answer a question addressed to you: `-Answer Q-NNN -Text "<the answer>" -Owner <your
  instance>`. Without a terminal, write the answer under `## Answer` and set `status: Closed`
  by hand. When only the user can answer it, raise a new question to `user` and say so in your
  answer; leave your question open until then.

**Decisions**
- Record a decision with `-New decision`, then fill in `## Decision` (what is decided), `## Why`
  (the reasoning, including the alternatives rejected) and the `files:` header (the files it
  affects). Its status stays `Proposed`; only the orchestrator changes it.
- Keep a decision short, about 8 lines. A longer explanation goes into your document, with a
  link from the decision.
- To replace a decision, record a new one and write `Replaces D-NNN` in its `## Decision`. The
  orchestrator sets the old one to `Superseded by D-NNN`.
- Ids never change and are never reused.

**Project environment**
- Use the DevOps environment, ticketing system and documentation system recorded under
  "Project environment" in `project.md`: refer to the task's tickets by their key, follow the
  branch naming and the pipelines, and write documentation for the place recorded there.
- When something you need about these systems is not recorded or is `unknown`, ask
  (absolute rule 4).

**Private context**
- After the reading in rule 1, read your own `private.md` (create it if it does not exist).
- Before you finish, update it with your working notes, what you have done and what is left, so
  your next run can continue.

**Writing**
- Change team files only with small edits in place, with your edit tools. Never write a whole
  file again: a failed write wipes it. New items come only from `-New`.
- Write every review in its own file (`reviews/`), never in another role's document.
- **Check at the end of your run.** Run the script with `-Build` (with locks:
  `-Unlock all -Owner <your instance> -Build`) once you have changed team files, and repair
  every error you caused before you finish. It also rebuilds `decisions.md`.
- Never put a blank line inside a table; it breaks the table. Every table row stays on one
  line, with as many columns as the table's header. To change a row, replace the whole row.
- Give effort in story points, never in hours or days.
- Write plainly and spell out abbreviations at first use.

**Report**
- Finish every task with a short report under `## Report` in its task file: what you did, the
  decisions you added, the questions you raised (and which ones you wait for), what you
  unblocked, and anything that blocks you. Details belong in the documents, not in the report.
- Give the same report as your final answer, so whoever started you sees it at once. Start that
  answer with **Questions for you**: the id and one line of every open question you addressed
  to `user`, or "No questions for you". In environments without notifications this is how the
  user notices them.
