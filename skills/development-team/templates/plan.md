# Delivery plan

_Owner: orchestrator; the architect estimates the story points, the sanity agent checks the
plan against the project boundaries. Lock this file before you change it (see `RULES.md`)._

**Approved by the user:** {{YYYY-MM-DD, or "not yet"}}

The user sees results early: the functionality is delivered in sprints (of the length in
`project.md`), per requirement or use case, the most valuable first. **Sprint 1 always ends with
a working first screen**: a thin slice through every layer that the user opens and uses (without
a user interface: the first working call or report). Every sprint ends with a sprint review in
which the user sees the working result and accepts it or asks for changes. Every demo has an
automated end-to-end test (the demo test) that passes before the review, and is never cut.
Every demo says how to sign in: the test account, and the file and setting with its password (never the
password itself). Nothing is built that is in no sprint; a change of this plan needs the
user's approval again.

## Sprints

| Sprint | Dates | Goal: what the user can see or do at the end | Requirements | Story points | Demo: how the user sees it | Status |
|---|---|---|---|---|---|---|
| S1 | {{YYYY-MM-DD to YYYY-MM-DD}} | {{A working first screen: ...}} | {{REQ-002}} | {{8}} | {{Start with ..., open https://localhost/..., sign in as `admin` (password: `SEED_ADMIN_PASSWORD` in `.env`), do ...}} | {{Current}} |

Status: `Planned`, `Current` (one sprint at a time), `Review` (waiting for the user's sprint
review), `Done`.

## Sprint reviews

Per sprint, newest first: the date, what the user saw, the user's verdict (accepted, or the
changes asked for), and what moved to a later sprint.
