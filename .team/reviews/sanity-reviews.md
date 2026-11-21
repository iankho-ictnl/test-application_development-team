## Sanity reviews

_Owner: sanity-agent._ Per review: date, what was reviewed, verdict (In customer interest,
Concerns, Not in customer interest), the check against the project boundaries, and reasons.
The latest review per subject (scope review, S2 and S1 assessment, delivery plan, D-041,
first design) and the scope reviews T-174 and T-154 stay in full (restored by T-191); older
reviews are one line each, newest first, with their full
text in `archive/2026-11-20-1808-ok/reviews/sanity-reviews.md` (compacted 2026-11-20, T-187).

**2026-11-20, scope review (T-197): T-187 to T-198, BUG-14 (T-194), S-37, pull requests #24 to
#29, S3 accepted, S4 at 10.75: In customer interest, with one concern (Q-123).**

*Items:*
- T-187, T-188, T-191 (compaction of the review files, and the restore after the T-187 shrink):
  team-internal (`for: team`), no product change. T-191 was rework caused by my own T-187; small.
- T-189, T-190, T-193, T-195, T-196, T-198 (code and architect reviews of #24 to #29): the reviews
  the rules require before the user approves; within REQ-007, REQ-008, REQ-009, REQ-013, REQ-030,
  REQ-032. All Approve so far, no Blocking finding.
- T-192 (0.25 SP, in S4): within REQ-030 (Q-119, summary messages on six forms) and REQ-013
  (S-37: an Admin's 2FA reset alone ends the user's sessions, a test only). It found BUG-14.
- T-194, BUG-14 (Low, 0.25 SP): within REQ-011 (Dutch screens). The customer notices it: an empty
  invoice date showed English text on the Dutch screen. One global fix covers every date and
  number field, at no extra cost: good value. It rides on the REQ-030 branch (#27), so #27 now
  carries three items; acceptable for one small frontend file, provided the pull request says so
  (code reviewer's Should fix). Not in the plan: Q-123.
- T-163 (REQ-007 tests, now Done) with #29; #24 merged: within REQ-007 and REQ-024. T-166 (the S4
  demo test) can start now; its report step needs #25 merged first.
- Q-122: done; S4 now lists REQ-013 with the demo step "open /Profile, then /Owners".
- **Not needed:** nothing found. **Outside the requirements:** nothing found.

*Plan, checked:* rows 8 + 19.5 + 14.75 + 10.75 + 6.5 + 4.5 + 5 + 5.5 + 5.5 + 2.5 = 82.5, but the
total line says 82.25: the #22 Should fix (0.25) is in S4 but not in the total. BUG-14 (0.25) is
in neither, while BUG-13 was counted. **Concern Q-123 (orchestrator):** S4 11, total 82.75,
REQ-011 in the S4 requirements. Small, but the plan is what the user approves.

*Boundaries:*
- Time: report.md still shows about 16.6 of 40 active hours (not updated since 18:04; the
  orchestrator adds the session time). Open: S4 about 0.5 SP to build (T-166) plus approvals;
  S5 to S10 29.5 SP. About 30 SP, at 2.7 SP per hour about 11 hours, against about 22 hours left.
  Fits, plus the cut list S9 and S6 (10 SP).
- The risk is still the user's approval, not the build: #25, #26, #27, #28 and #29 all wait for
  the user. The customer sees S4 only after they are merged and the demo test passes. Advice:
  approve #28 (BUG-13, the one the user notices daily), #25, #26, #27, #29 in that order, then
  the S4 review.
- Resources: no new agent or system. Budget: no new library or tool (BUG-14 uses MVC's own
  binding messages).

Reviewed up to: T-198, D-058, REQ-032

**2026-11-20, scope review (T-186): T-175 to T-185, D-058, Q-116 to Q-120, tester-2 and the
plan total of 82: In customer interest, with one concern (Q-122).**

*Items:*
- Q-116: done; plan.md now shows S4 9 before the two additions, as the user approved.
- Q-118 b with T-180 (backend, 0.25) and T-181 (PostgreSQL test, 0.25): the user's choice;
  within REQ-007 and REQ-024 (D-056). Invoice numbers without a gap, also in the rare case of a
  delete and a create at the same moment. No new package.
- D-058 with T-182 (0.5), T-183 (0.25), T-184 (security review): within REQ-013 and REQ-001
  (BUG-13); the user said "fix now". The customer notices it directly: opening /Profile no longer
  signs them out. Cheapest fix (backend only, frontend unchanged). D-058 was accepted before the
  security officer's T-184: the pull request must not be merged before T-184 (security officer's
  field, not repeated here).
- T-175 (fixes from the #22 review), T-176, T-177, T-178, T-185 (code and architect reviews):
  reviews and rework the rules require; within REQ-030, REQ-007, REQ-032. Rework has no points.
- T-179 (CA1305, 0.25 SP): within REQ-032 and the D-054 2-point cap (1.75 + 0.25 = 2), but not
  in the plan total: Q-122.
- Q-117, Q-120: field names and an expected test shape for the owner overview; within REQ-009
  (Q-115 a), no scope change.
- Q-119 (summary-only errors on six forms enough for REQ-030 criterion 4): the architect kept
  per-field messages out (about 1.25 SP, no requirement asks for them). In customer interest:
  the user saw these forms in S1 and S2 and did not ask for it; it stays a possible later request.
- tester-2 (user, 18:00, D-055): no cost, no new tool; takes T-164, T-166, T-173, T-183, so the
  tester can finish S3 and the REQ-007 tests.
- **Not needed:** nothing found.

*Plan, checked:* rows 8 + 19.5 + 14.75 + 10.25 + 6.5 + 4.5 + 5 + 5.5 + 5.5 + 2.5 = 82; S4
9 + 0.5 (Q-118 b) + 0.75 (D-058) = 10.25. Correct. **Concern Q-122 (orchestrator):** the S4 row
lists REQ-007 to REQ-024 only, while it now carries the BUG-13 fix (REQ-013): add REQ-013 and a
demo step the user sees (open /Profile, then /Owners: still signed in); and T-179 (0.25) is not
in the total (would be 82.25).

*Boundaries:*
- Time: about 16.6 of 40 active hours used, 23.4 left. Open: S3 reviews and T-179 (0.25); S4
  about 3.25 (T-163 1, T-164 0.25, T-166 0.5, T-173 0.5, T-181 0.25, T-182 0.5, T-183 0.25);
  S5 to S10 29.5. About 33 SP, at 2.7 SP per hour about 12.2 hours. Margin about 11 hours, plus
  the cut list S9 and S6 (10 SP). Fits.
- The real risk is approval, not build: #22 waits for its re-review, REQ-007 (T-180), REQ-008
  and REQ-009 have no pull request yet, and S3 has not had its sprint review. The customer sees
  nothing new until these are approved. Advice repeated: hold the S3 review now, then S4.
- Resources: one more agent instance on the same laptop; no extra cost.
- Budget: no new library or paid tool (the advisory lock is PostgreSQL's own).

Reviewed up to: T-186, D-058, REQ-032

**2026-11-20, scope review (T-174): T-155 to T-173, D-055 to D-057, Q-115 a, the plan total and
the parallel S3/S4 work: In customer interest, with one concern (Q-116).**

*Items:*
- D-055 (two extra developers, S4 next to S3): the user asked for it. Within REQ-007, REQ-008,
  REQ-009, REQ-024. In customer interest: S3 had almost no build work left, and S4 stays on its own
  branches until the S3 review and the user's approval. Cost: more pull requests to review and
  approve (#20 and #22 are waiting now, three S4 branches follow); the user's review time becomes
  the bottleneck, not the build.
- D-056 (numbers stay MAX+1 inside the create transaction, no sequence; only the most recent open
  invoice can be deleted): within REQ-007 and REQ-024. Good for the customer: invoice numbers
  without gaps, as Dutch invoicing expects; it also takes 1 SP out (T-039 part a not built).
- D-057 (term fixed at 14 days, field kept): within REQ-007 (BUG-6). Cheapest fix, already built.
- T-155 (S4 task list), T-156, T-157, T-158, T-171 (reviews of #20, #21, #22), T-165 (formatting
  fix from a review): reviews the rules require; within REQ-007 to REQ-009, REQ-024, REQ-028,
  REQ-030, REQ-032 and BUG-12.
- T-159, T-160, T-163 (REQ-007), T-161, T-162, T-164 (REQ-008), T-166 (S4 demo test),
  T-170, T-172, T-173 (REQ-009, Q-115 a): within their requirements; each gives the customer
  something visible in the S4 demo. T-162 went to frontend-developer-1 instead of -2: uses free
  capacity, fine.
- Q-115 a (+2.5 SP, owner overview lists open invoices with due date and overdue mark): the
  user's choice; it closes three REQ-009 criteria (US-9.1, US-9.4, US-9.5) that were not met.
- **Not needed:** T-167 and T-168 (duplicates of T-170 and T-172, already closed by the
  orchestrator); **T-169** is the same piece T19 as T-173 and is still Blocked: Q-116 asks the
  orchestrator to close it. T-152 now follows Q-113 (uninvoiced lines under the current owner).

*Plan, checked:* the plan.md rows add up to 8 + 19.5 + 14.75 + 9.25 + 6.5 + 4.5 + 5 + 5.5 +
5.5 + 2.5 = 81, so the total matches its rows. **Concern Q-116 (orchestrator):** the S4 pieces in
`architecture/build-tasks.md` add up to 6.5 + 2.5 = 9, and the user's final answer to Q-115
(17:41) says "S4 becomes 9"; the 9.25 comes from the question title. The correct figures are S4 9
and a total of 80.75. A difference of 0.25 SP, but the plan should show what the user approved.

*Boundaries:*
- Time: about 16.3 of 40 active hours used, 23.7 left. Open: S3 almost done (reviews), S4 about
  8 (9 minus T-054), S5 to S10 29.5; about 37.5 SP, at 2.7 SP per hour about 14 hours with one
  build stream. Margin about 9.7 hours (about 26 SP), plus the cut list S9 and S6 (10 SP). Two
  more developers shorten the build, but each sprint still needs a code review, an architect
  review and the user's approval and sprint review, so do not count on more than 2.7 SP per hour:
  the plan fits without the extra speed. Advice: hold the S3 review now, before S4 is ready, so
  the approvals do not pile up.
- Resources: two more agent instances on the same laptop; no extra cost, no new tools.
- Budget: no new library or paid tool; D-056 even drops the sequence work.

Reviewed up to: T-174, D-057, REQ-032

**2026-11-20, scope review (T-154): T-140 to T-153, D-053, D-054, Q-112 b and the plan total
of 79.25 story points: In customer interest, with two concerns (Q-113, Q-114).**

*Items:*
- T-140 (shared ValidationMessages, 0.5 SP, D-052 condition), T-144 (indentation fix from the
  review): within REQ-003 and REQ-011. Small and unplanned; the customer does not notice them, but
  the reviews asked for them.
- T-141, T-142, T-145, T-146 (reviews of #19), T-143 (security, asset owner change), T-148,
  T-149 (reviews of #20): reviews the rules require; within REQ-003, REQ-019, REQ-009, REQ-028,
  REQ-032. **T-147 is not needed:** `code-reviews.md` already reviews d9ed60e (no finding), and
  REQ-003 shows "reviewed for T-144". Q-114 asks the orchestrator to close it.
- D-053 (the user cut T-030 and T7, 5 SP): the user's choice, in line with the agreed cut list.
  What the customer gives up: the S10 demo step "a wrong date gives the error before posting" is
  client-side, and the demo test has no browser (D-047), so only the user's own walk-through shows
  it. Stated, not a concern.
- D-054, T-150, T-151, T-153 (warning clean-up, 1.75 SP): within REQ-032 and the 2-point cap of
  Q-111. The cheapest option (no renaming of 260 tests, no global suppression, CA5350 kept on).
  In customer interest; the customer only sees a cleaner build.
- T-152 (Q-112 b, S4, 1 SP): within REQ-009; the customer notices a correct per-owner overview.
  **Concern Q-113 (architect):** the task says uninvoiced lines go to "the owner at the line
  date", but the data model has no asset ownership history, and the S3 demo (accepted behaviour)
  lets the new owner invoice the moved asset's uninvoiced lines. Q-112 b as asked put uninvoiced
  lines under the current owner. With a history it is more than 1 SP and needs the user's OK.

*Plan, checked:* the rows add up to 8 + 19.5 + 14.75 + 7.5 + 6.5 + 4.5 + 5 + 5.5 + 5.5 + 2.5 =
**79.25**, so `plan.md` is right (S3 13 + 1.75; S4 6.5 + 1; S10 7.5 − 5). The cut list still
gives S9 as 5 and S6 as 4; with their demo tests they are 5.5 and 4.5 (Q-114).

*Boundaries:*
- Time: **fits, with the margin unchanged.** S3 to S10: 51.75 SP (part of S3 is done) at about
  2.7 SP per active hour is about 19 hours; left about 24 hours (`report.md`: about 16 of 40
  used). Margin about 5 hours (about 13 SP), plus 10 SP on the cut list (S9, S6). The cut gained
  5 SP; D-054 and Q-112 b took back 2.75. Reviews and fix-ups per pull request (T-140 to T-149:
  ten tasks for two pull requests) are not in the story points and are the main risk to the
  velocity; when T-152 needs an ownership history, it grows further.
- Resources: within (laptop with Docker Desktop, D-039; parallel developer instances in
  worktrees, D-053, no new system).
- Budget: within (no new package; SDK analyzers only).

Reviewed up to: T-154, D-054, REQ-032

*Older scope reviews, one line each:*

- **2026-11-20, scope review (T-139):** T-129 to T-138, D-051, D-052, REQ-032, S2 review, plan 81.5: **In customer interest**, one concern (Q-111: tell the user 81.5, cap the B20 clean-up); time fits, thinly. Reviewed up to: T-139, D-052, REQ-032

**2026-11-20, S2 assessment and scope review (T-128): REQ-002 on feature/REQ-002-manage-owners
d6c62cb (#16) with #12 to #15, items after T-119, D-048, REQ-031: In customer interest, with
one concern (Q-109).** Evidence: `testing.md` T-109 runs 1 and 2 (main c00cc51, the app on the
laptop through https://localhost with curl), T-112 runs 1 to 3 (399 passed, 0 failed, demo test
in nl-NL and en-US on PostgreSQL 18); `code-reviews.md`: both reviewers Approve on #12 to #16.

*S2 demo (`plan.md`) and REQ-002, as the customer sees it:*
- Save with only a name gives field errors (US-2.1, US-2.1a): met on the app (T-109) and in the
  demo test. Foreign address with DE123456789 accepted, XX123 refused with a message in the
  screen's language (US-2.1b, US-2.1d): met (T-109, TC-DEMO-S2-4, -5, -9).
- Owner with assets refused, with the reason (US-2.3): met in the demo test in Dutch and English
  (TC-DEMO-S2-10, BUG-10 fixed); on the app only before the fix, so show it at the review.
- Delete without assets: gone from the list, still on its invoices, PDF with the current owner
  data (US-2.4, US-2.1c): met on the app (T-109 run 2) and in the demo test (TC-DEMO-S2-6, -8).
- `dotnet test` all green: met (399), with Docker Desktop running (D-047, within D-039).
- Not shown anywhere: a real browser (curl and no-browser demo test, D-047); so the user's own
  walk-through at the review is the first browser check of client-side validation.
- The customer will notice: owners from S1 keep their old address as street, with house number
  and country empty ("Teststraat 1 , Teststad" in the list); they must be completed at the next
  edit. Correct per D-041, but tell the user.
- Concern (Q-109, tester): BUG-3 (High) is still Open in `testing.md` although it is fixed and
  shown on the app, so `report.md` shows the user a wrong open High bug; BUG-11 is Open while
  #15 is approved; and the English maximum-length message on the Dutch owner form (T-109, not
  raised) is not covered by B18 (T-127, API only).
- S2 goal text "screen regression tests in place": REQ-030's owner screen tests (T-057) moved to
  S3 (Q-093); in S2 only the demo test is in place. Fix the wording in the plan change.

*What to show the user at the S2 review* (after #16 is merged, app from main, sign in as in
`plan.md`: `SEED_ADMIN_USERNAME` and `SEED_ADMIN_PASSWORD` in `.env`, never in the chat):
in a browser, `/Owners/Edit` with only a name (Dutch field errors); switch to English; a Berlin
owner with DE123456789 saved, the same with XX123 refused at the VAT field; delete an owner with
an asset (reason shown, Dutch and English); delete an owner without assets (gone from the list,
success message in Dutch if #15 is merged, still on `/Invoices/Details` and in the PDF with the
current address); one migrated S1 owner with the empty house number; then `dotnet test` green.

*Plan change to put to the user:* total 65.5 to 77 story points: + 1 (B16, T12; D-046), + 2
(B17, F8; D-049), + 0.5 (B18, T-127), + 2 (S2 demo test T-112), + 4.5 (demo tests S3 to S10,
0.5 each, S7 1), + 1.5 (T-114, T-119, done). S2 in `plan.md` says 9.5, `build-tasks.md` gives
about 19.5 including the above: correct the S2 row. Cut list unchanged (T-030 3, T7 2, S9 5, S6
4); rule: when the margin falls under about 8 story points, cut T-030 first (Q-106). Change the
S2 goal wording (above). Not yet in the numbers: T-111's code-check decisions (they need an
approved requirement or the user's OK, rule 14) and the two reviews per pull request.

*Scope review, items after T-119, D-048, REQ-031* (`ids.log`): T-120 (B17), T-121 (F8), D-049,
D-050: REQ-002 and REQ-011, replace D-048 with one way that does not break when a text changes;
the customer sees messages in his own language. T-122, T-123 (reviews of #15, BUG-11), T-124,
T-125, T-126 (reviews of #16): REQ-002, required by the rules. T-127 (B18): REQ-011, small (0.5),
in S2 but not needed for the demo; fine as long as it does not hold up the S2 review. T-128:
this review. Outside the requirements / not needed: none.

*Boundaries:* Time: about 15.2 of 40 hours used (`report.md`), 24.8 left. Still to do: B18 0.5
+ S3 to S10 48 + demo tests 4.5 = 53 story points, about 19.6 hours at the S1 speed (2.7 points
per hour): margin about 5 hours (14 points), more than the 8-point cut line; S2's own speed
cannot be separated in `report.md` (one day mixes S1 and S2), so keep 2.7. Resources: laptop
with Docker Desktop, now needed for every `dotnet test`; within D-039. Budget: no new library or
cost (PDF text read with PDFsharp itself, TC-DEMO-S2-8).

Reviewed up to: T-128, D-050, REQ-031

- **2026-11-20, scope review (T-118):** T-102 to T-119, D-045 to D-048, Q-100 to Q-106, BUG-10, BUG-11: **In customer interest**, one concern (Q-106: estimate the demo tests, T-114, T-119 and the reviews; cut list); time fits, margin about 7 hours. Reviewed up to: T-119, D-048, REQ-031

- **2026-11-20, scope review (T-101):** T-091 to T-101, Q-097 to Q-099: **In customer interest**, no concerns; time fits, margin about 4 hours. Reviewed up to: T-101, D-044, REQ-031

**2026-11-20, S1 assessment (T-099): main f3542bc against REQ-017, REQ-001 (US-1) and the S1
demo: In customer interest, with one concern (Q-099).** Evidence: `testing.md` runs T-071,
T-094 (69a0444) and T-097 runs 1 and 2 (f3542bc), all with curl through https://localhost on
the laptop, not in a browser.

*Acceptance criteria:*
- REQ-017 met on the running app: `up -d --build --wait` starts all 5 services healthy (T-071,
  T-094, T-097); the seed admin signs in (302); after `down` and `up` the data is kept (T-071:
  row counts of all 15 tables; T-097: invoice 2026-0004 unchanged).
- REQ-001 / US-1 ("no session, any page: the sign-in page"): **not shown** in any run (only the
  sign-in page itself, 200). Show it at the review (Q-099).

*S1 demo steps:*
- Met on f3542bc: start, sign in, activity, invoice with PDF (14-day term, VAT correct), report,
  euro amounts in English (BUG-9 fixed), date fields (BUG-8 fixed), 429 page on the 11th attempt
  in a minute, lock-out after 5 wrong passwords (proved by the correct password being refused
  and the "Geblokkeerd" badge), down/up keeps data.
- Met on the earlier commit 69a0444 (T-094), not repeated on f3542bc: add an asset, add an
  expense, the language switch itself (T-097 set the culture cookie directly).
- **Covered only by automated tests:** two-factor sign-in on and off on `/Profile` (REQ-013)
  was not checked on the running app (AuthServiceTests; ProfileTwoFactorDisableTests against a
  test double). Not checked at all: the PDF content by eye, a browser walk-through.
- Demo wording: the user sees no separate lock-out message; by design (D-036) it is the normal
  sign-in error. The plan says "lock-out message" (Q-099).

*What to show the user at the sprint review:* the full demo in a browser, adding a new asset and
expense on f3542bc; open `/Owners` without a session (sign-in page); turn two-factor on and off
with the user's own authenticator app; open one invoice PDF. Sign in as in plan.md (seed admin,
`SEED_ADMIN_USERNAME`/`SEED_ADMIN_PASSWORD` in `.env`). The test user `s1test` stays locked in
the volume. The open High bug BUG-3 (owners are deleted) is planned in S2, not an S1 blocker.

*Customer benefit:* the customer can now run the app on the laptop and walk the core workflow
(asset, cost, work, invoice, report), which is the request in `project.md`. Three bugs that
blocked that workflow (BUG-7, BUG-8, BUG-9) were found only on the running app, and are fixed.

*Boundaries and velocity:*
- Time (40 hours): about 13.7 hours used (`report.md`), 26.3 left. S1 delivered 8 planned + 1.5
  unplanned story points (T-087, T-088, Q-098), plus the unestimated BUG-7/8/9 fixes, in about
  3.5 active hours on its days (2026-11-12 to 2026-11-20): at least about 2.7 points per hour.
  Left: 57.5 points (S2 to S10) + 1 (T-079, Q-095) = 58.5, about 22 hours at that speed. It fits,
  with about 4 hours of margin; the margin is thin, because every sprint so far has found bugs on
  the running app. Keep the cut list (T-030 3, T7 2, S9 5, S6 4 = 14 points, about 5 hours) and
  check the fit again after S2. The active-time figure is an estimate from activity, not the
  user's own time.
- Calendar: S1 ran 9 calendar days instead of 1; the S2 to S10 dates (to 2026-12-03) are
  milestones, not the boundary; the boundary is the 40 hours.
- Resources: laptop with Docker Desktop only, as agreed (D-039). Budget: no new library or cost
  in S1.

- **2026-11-20, scope review (T-089):** T-082 to T-090, D-042 to D-044, BUG-7, BUG-8: **In customer interest**, two small concerns (Q-097: S1 demo 429 wording; Q-098: T-087, T-088 without story points); time fits. Reviewed up to: T-090, D-044, REQ-031

- **2026-11-20, scope review (T-081):** T-073 to T-081, D-040, D-041, the plan change of Q-093: **In customer interest**, two small concerns (Q-094: demos do not say how to sign in; Q-095: T-079, T-080 without story points); velocity still unknown. Reviewed up to: T-081, D-041, REQ-031

**2026-11-20, D-041 and the changed REQ-002 criteria (T-078; US-2.1a to US-2.1d, T-075): In
customer interest; one concern about the plan.**

*Customer benefit:* every part comes from the user's own answers (Q-087 separate address fields,
KvK and BTW; Q-088 foreign addresses and any EU VAT number; Q-089 no invoice copy). The customer
notices it at once: complete owner data on screen and on the invoice PDF, and foreign owners can
be saved. The criteria are things the customer sees (a message at the field, concrete accepted
and refused examples such as DE123456789 and NL123, the PDF owner block).

*Over-engineering:* none found. The VAT check (27 prefixes plus a length check, the Dutch pattern
for `NL`) is the cheapest check that meets "accept any EU format"; one pattern per country and a
VIES lookup were rejected. Country as free text, rename instead of a two-step migration (nothing
deployed, Q-079), and no invoice copy all keep cost down.

*Stated, not hidden (fine as is):* existing owners get an empty house number and country after
the migration, and staff must fill them in on the next save. An older invoice's PDF shows the
owner's current details; the user chose this knowing the consequence (Q-089). Small point, no
question: REQ-003 US-3.3a does not repeat "foreign addresses accepted" for assets; with only
"filled in" checks it behaves the same.

*Concern (Q-093 to orchestrator):* `plan.md` (approved 2026-11-11) still says S2 6, S3 5, total
56; the build task list now gives S2 12.5, S3 8, total 65.5. A plan change needs the user's
approval, and S2 is twice a one-day sprint. Put it to the user together with D-041 (still
Proposed, and the overview warns that nobody follows it up).

*Boundaries:*
- Time (40 hours): about 11.7 used (report.md), about 28 left. 65.5 points, 51.5 after the agreed
  cut list. S1 is still Current since 2026-11-12, so velocity is unknown and the fit check after
  S1 is overdue; the +9.5 points make it decisive.
- Resources: unchanged (laptop, Docker Desktop, migrations run there).
- Budget: no cost; no new library, no external VAT service.

- **2026-11-11, scope review (T-072):** REQ-031, Q-084, T-070 to T-072; the T-069 plan advice all taken: **In customer interest**, one small concern (Q-085: REQ-031 has no acceptance criteria). Reviewed up to: T-072, D-039, REQ-031

**2026-11-11, delivery plan (T-069: plan.md, T-068) and D-039, skill 2026-11-11.1: plan Concerns,
D-039 In customer interest.**

*D-039:* in the customer's interest. It turns the laptop with Docker Desktop (Q-078) into the
place where REQ-017, REQ-023, REQ-025 (ports, HTTPS redirect), the D-024 roles and the REQ-022
paging and query plans are really checked, at no cost, and lists honestly what cannot be
checked without a server. It rejects a pipeline (new scope, possible cost) and a second path
past the proxy. Small point: REQ-030's first criterion still cites D-038.

*Plan, what is right:* the app exists end to end, so S1 = run it on the laptop and sign in is
the soonest working screen (rule 15). BUG-3 (High, deletes data kept 7 years) is in S2. Every
sprint has a demo the user can do. The new tasks all serve approved requirements or accepted
decisions (B12: D-033; T6: D-028; T7: D-029; version in forms: D-024; paging screens: REQ-022;
anonymization: REQ-024; backup and REQ-025 checks: REQ-023, REQ-025). The two frontend
additions (S6, S8; 4 story points) were missing before and are needed, or the API changes break
or hide data on the screens.

*Concerns and recommended changes:*
1. The customer's request (assets, expenses per asset, activities as income: REQ-003 to
   REQ-008) is in no demo; the demos show fixes and hardening. Add the core flow to the S1
   demo (asset, expense, activity, invoice with PDF, report, language switch; 0 story points),
   and in S1 do the first `docker compose up` before T-044/T-046.
2. The invoice delete of Q-083 (1 story point) belongs in S4 with the invoices: retention
   applies from the first invoice; anonymization in S9 has no effect for seven years.
3. 54 (+1) story points against 32 hours left, velocity unknown. Agree the cut order with the
   user at approval and check after S1, not S3: T-030 (3), T7 (2), S9 anonymization (5), S6
   concurrency (4; first if only one person edits at a time). These are approved requirements,
   so the user decides; cutting all four gives about 40 story points.
4. S3, S4 and S10 fall on Saturday or Sunday; a part-time user reviews a sprint every day.
   The user confirms the days at approval.
5. The project ends with code ready to deploy, but the repository has no deployment guide and
   `.team/` (architecture, the built-not-verified list) is git-ignored: Q-084 to the user.

*Boundaries:*
- Time (40 hours): about 7.8 hours used (report.md), about 32 left. 54 (+1) story points
  planned in ten 1-day sprints to 2026-11-21. The fit is unknown until S1 shows a velocity;
  the cut list in 3 keeps the boundary reachable. The tighter limit stays the user's time: ten
  sprint reviews and about 15 pull requests to approve.
- Resources (the user part-time, laptop with Docker Desktop, no server): fit since D-039. The
  Let's Encrypt certificate, the server timing (Q-082) and hosting costs stay built, not
  verified, as the boundary "no go-live" implies.
- Budget (no extra budget, free and open source, REQ-027): kept. No paid tool or service added.

*Scope review* (since T-067, D-038, REQ-030): T-068 and T-069 team; D-039 within (REQ-017,
REQ-022, REQ-023, REQ-025, REQ-030), replaces D-038; Q-082 and Q-083 rightly to the user.
Outside the requirements: only the proposed deployment guide (Q-084). Not needed: none.

*Reasons for Concerns:* the plan is close to the customer and fixable with reordering, no new
scope; but as written it never shows the customer their own request working, leaves an
invoice-retention gap until the ninth day, and postpones the fit decision until a third of the
plan is spent.

Reviewed up to: T-069, D-039, REQ-030

- **2026-11-11, scope review (T-067):** T-064 to T-067, D-037, D-038, REQ-028 to REQ-030 after the user's answers Q-061 to Q-079: **Concerns** (Q-081: REQ-028 and xUnit v3, REQ-030 "every change" without a pipeline, T8 waits for dropped T-011; without Docker and a server the customer may not run what was asked, pending Q-078, Q-079). Reviewed up to: T-067, D-038, REQ-030

- **2026-11-10, scope review (T-063, rule 14):** all of T-002 to T-063, D-001 to D-036, REQ-001 to REQ-027: **Concerns** (Q-061: D-026 restructuring 11 story points outside the requirements; Q-062: xUnit v3; Q-063: BUG-1; Q-065, Q-066: T-011, T-014 not needed; Q-067: BUG-3 chained behind T-038; Q-068: REQ-026; Q-069: 12 open tasks without estimate; Q-064: resources not recorded). Reviewed up to: T-063, D-036, REQ-027

**2026-11-06, design (first architecture): In customer interest.** All requests covered
(assets, expenses per asset, activities as income) plus confirmed extras. Boundaries: 40 h is
tight for 10 Must stories; risk accepted by keeping the UI plain Bootstrap scaffolding and one
project. Excel import deferred, as agreed.

**2026-11-06, revision 2: In customer interest.** Moving the portal out keeps the 40-hour
boundary realistic.
