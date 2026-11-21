# Project: Property administration and maintenance system

**Last compacted:** 2026-11-21

**Last checked against the skill:** 2026-11-21 (skill 2026-11-20.10, applied by Claude Code at the user's request with -UpdateFromSkill: two new commands, team stop (finish the running tasks, commit, reviews, one approval question for the pull requests, merge) and team emergency stop (everyone stops at once, nothing committed, pushed or merged), through stop.md in the team folder that every agent checks at every step (-Stop finish / -Stop now, -Resume on team continue); prompt files /team-stop and /team-emergency-stop added. Product rules unchanged since 2026-11-20.8: no re-check tasks needed. Team setup files committed by the orchestrator in VS Code on 2026-11-21 (2a1b7d0, on feature/REQ-030-screen-regression-tests; not yet on main). Before that 2026-11-20.9, applied by Claude Code at the user's request with -UpdateFromSkill: less administration. Scope review once per sprint in the sanity agent's sprint check, ending with "(sprint Sn)", and at once for a new requirement; the architect reviews only pull requests of a requirement or change request; one task per item and role, a developer's or tester's task covers one requirement, next steps, fixes and re-reviews are rounds of the same task (-Reopen); the review note names its commit (reviewed HH:MM up to <commit> for ...), whitespace-only commits need no review; developers format their changed files before committing (dotnet format --include); titles at most 80 characters, details with -Text. Product rules unchanged since 2026-11-20.8: no re-check tasks needed. Team setup files committed by the orchestrator in VS Code on 2026-11-21 (a156f98, on feature/REQ-030-screen-regression-tests). Before that 2026-11-20.8, applied by Claude Code at the user's request with -UpdateFromSkill: new mandatory role code-reviewer (agent definitions, Team table, reviews/code-reviews.md; its web tools removed) and the architect's review of every pull request as a whole, both before the user is asked to approve; every sprint's demo has an automated end-to-end test (rule 15); as few outside libraries and tools as possible (rule 17). Still for the orchestrator: commit the team setup files, a demo test task for S2, the architect's decision on the code checks (Directory.Build.props) and on coverlet.collector, and from now on the two reviews on every ready pull request. Before that 2026-11-20.6, applied by Claude Code at the user's request with -UpdateFromSkill: commands VS Code can read (an option and its value with a space, no bare --), one command for skill updates (-UpdateFromSkill), -Lock accepts paths such as architecture/build-tasks.md, ask_user named as the question tool of VS Code agent sessions, Assisted permissions recommended; no product review tasks needed, the change is about how agents run. Before that 2026-11-20.3, applied by the orchestrator in VS Code: single quotes for PowerShell arguments with special characters. Before that 2026-11-20.2, applied by Claude Code at the user's request: documents' folders such as architecture/ checked, copied and restored; rule 16, every file stays in the repository, scratch files in .tmp/; no improvised git commands, no worktrees of your own; no product review tasks needed, the change is about how agents run. Earlier: 2026-11-12.1, short runs with heartbeat, commands that end, next-sprint refinement)

**Time used:** not recorded before 2026-11-10 (the orchestrator adds the team's working time after every session;
the report compares it with the time boundary). 2026-11-21 22:50-23:15: 0.5 h (VS Code: compaction, skill 2026-11-20.9 setup commit, T-166 S4 demo test, merges #25, #26, #27, #29, #30; team stopped by the user)

**Skill updates:** apply (user, 2026-11-10). Apply: when the skill has a new version, the
orchestrator updates the rules files and the agent definitions without asking, and tells the
user what changed (code changes that follow from it still need the user's OK).
Applied by Claude Code only (user, 2026-11-20): Claude Code copies a new skill version into the
team folder and the agent definitions; the orchestrator in VS Code does not copy skill files, it
only checks that the versions match and commits the changed team setup files.

**Agents at the same time:** yes. Several agents or chats run at the same time, and each locks the team files it changes (user, 2026-11-09).

**Sprint length:** 1 day (user, 2026-11-11). The delivery plan (`plan.md`) delivers the functionality
sprint by sprint; every sprint ends with a sprint review with the user.

## Team

| Instance | Agent type | Model tier | Model | Owns |
|---|---|---|---|---|
| orchestrator | none (the main session running the skill) | - | GitHub Copilot session model | Customer request, Project environment, decision status, layout of this file |
| architect | architect | reasoning | session model (no per-role model choice in VS Code) | Requirements, Architecture |
| security-officer | security-officer | reasoning | session model (no per-role model choice in VS Code) | Security assessments |
| code-reviewer | code-reviewer | reasoning | session model (no per-role model choice in VS Code) | Code reviews in detail (reviews/code-reviews.md) |
| backend-developer-1 | backend-developer | standard | session model (no per-role model choice in VS Code) | Backend code and backend infrastructure |
| backend-developer-2 | backend-developer | standard | session model (no per-role model choice in VS Code) | Backend code of the next sprint in parallel (user, 2026-11-20, D-055) |
| frontend-developer-1 | frontend-developer | standard | session model (no per-role model choice in VS Code) | Frontend code and frontend infrastructure |
| frontend-developer-2 | frontend-developer | standard | session model (no per-role model choice in VS Code) | Frontend code of the next sprint in parallel (user, 2026-11-20, D-055) |
| sanity-agent | sanity-agent | standard | session model (no per-role model choice in VS Code) | Sanity reviews |
| tester | tester | standard | session model (no per-role model choice in VS Code) | Testing |
| tester-2 | tester | standard | session model (no per-role model choice in VS Code) | Tests of the next sprint in parallel (user, 2026-11-20 18:00, D-055) |
| analyst | analyst (optional) | reasoning | session model (no per-role model choice in VS Code) | Analysis |

The designer is not in the team. The orchestrator is the main session that runs the skill, not an
agent. It starts the team members, adds instances (for example `backend-developer-2`) when the
work needs them, and puts questions addressed to `user` to the customer.

## Customer request

_Owner: orchestrator._ The request in the customer's own words, plus who the customer is.

"A system for keeping administration and maintenance of houses and buildings. We need to keep track of the assets (buildings and homes). We need to keep track of the expenses per asset. We need to keep track of the activities we did for administration and maintenance of these buildings; this is our income."

**Project boundaries**

| Boundary | Value |
|---|---|
| Time (deadline, milestones) | 40 hours; the project ends with code that is ready to deploy, going live is not part of it (Q-079) |
| Resources (people, skills, systems) | The user, part-time; the user's laptop with Docker Desktop (Q-078); no production server (Q-064, Q-079) |
| Budget | No extra budget; free/open-source libraries only |

## Project environment

_Owner: orchestrator._ Filled in at kick-off from what the repository shows (git remote,
pipeline files, project documentation) and from the user. Nothing is guessed: an unknown value
is written as `unknown` and becomes an open question to `user`.

**Solution types:** none. The skill has no solution type for a business web application yet (it
has IoT only).

**AI environment**

| Item | Value |
|---|---|
| Tool (for example Claude Code, GitHub Copilot in VS Code, GitHub Copilot in Visual Studio) | GitHub Copilot in VS Code; role agents are available through this environment (earlier: Visual Studio 2026 and Claude Code). |
| Model for the reasoning tier | session model (no per-role model choice in VS Code) |
| Model for the standard tier | session model (no per-role model choice in VS Code) |
| Can start sub-agents | Yes (VS Code role agents) |
| Where the agent definitions are | `.github/agents/*.agent.md` in the repository |

**DevOps environment**

| Item | Value |
|---|---|
| Platform (for example Azure DevOps, GitHub, GitLab) | GitHub |
| Organization and project | iankho-ictnl |
| Repositories | https://github.com/iankho-ictnl/test-application_development-team |
| Base branch and branch naming | main; see Version control |
| Pipelines (build, release) | none (Dockerfile present) |
| Deployment environments (for example development, test, production) | unknown |
| Cloud and hosting (for example Azure subscription, region) | unknown |

**Version control**

| Item | Value |
|---|---|
| Commits and pushes by the agents (the user's OK, with date) | yes, 2026-11-10 |
| Pull requests by the orchestrator (the user's OK, with date) | yes, 2026-11-10 |
| Base branch | main |
| Branch per item | feature/REQ-nnn-short-title, bug/BUG-n-short-title, change/D-nnn-short-title (D-026: change/D-026-v1-layout) |
| Pull request tool | the GitHub Pull Requests extension in VS Code (the GitHub CLI is not installed) |
| Who merges | the orchestrator, after the user's approval in the chat (OK, 2026-11-10) |
| Team folder in git | committed (user, 2026-11-22): the documents, items, rules and script of `.team/`; `.team/archive/`, `.team/locks/` and `.team/agents/` stay out of git (`.gitignore`). A copy of the development-team skill is in `skills/development-team/` (not loaded automatically; the skill the agents use stays at user level) |
| Copy of the repository | https://github.com/iankho/samelshar (remote `copy`, independent copy with all branches, user 2026-11-21) |

**Ticketing system**

| Item | Value |
|---|---|
| System (for example Jira, Azure Boards) | none |
| Site and project | none |
| Tickets for this task | none |
| Conventions (ticket key in branch names and commit messages, required fields) | none |

**Documentation system**

| Item | Value |
|---|---|
| System (for example Confluence, Azure DevOps wiki, a docs folder in the repository) | none |
| Space or location | none |
| Pages for this task | none |
| Where new documentation goes | none |

## Rejected decisions
