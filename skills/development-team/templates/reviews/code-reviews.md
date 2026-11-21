# Code reviews

_Owners: code-reviewer (the code in detail) and architect (the change as a whole), each writing
only their own reviews. Lock this file before you change it (see `RULES.md`); the lock name is
`code-reviews.md`._

Per review, newest first, starting with one bold line: the date, the pull request (item, link,
tasks), the reviewer (`code-reviewer` or `architect`) and the verdict (**Approve**, or
**Changes needed** while a Blocking finding is open), for example
`**2026-11-20, REQ-002 <link> (T-033, T-034), code-reviewer: Approve.**`. Then
`Reviewed up to: <commit>`, and per finding the file and line, the severity (Blocking,
Should fix, Nit), what is wrong, why it matters and the suggested fix. A review of fixes covers
only the commits after the previous `Reviewed up to:`; commits that only change whitespace need
no review. The user is asked to approve a pull request only when its reviewers' latest verdicts
are Approve: the code reviewer's always, the architect's for a requirement or change request
(a pull request that only fixes a bug or only changes tests: only when the code reviewer named
a finding for the architect).
Compaction keeps the latest review per pull request and reviewer in full and shortens older ones
to one line.
