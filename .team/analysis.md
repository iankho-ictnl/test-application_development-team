## Analysis

_Owner: analyst (optional)._ Problem and goals, stakeholders and user groups, current situation
(as-is) and desired situation (to-be), business rules, glossary, user stories (US-n: "As a ...,
I want ..., so that ...") with Given / When / Then acceptance criteria and MoSCoW priority,
success measures, and at the end the acceptance check per user story (met or not met, with
evidence).

**Status:** Accepted: the user confirmed the problem summary and US-1 to US-10 on 2026-11-06
(question 8). US-10 was later moved to "later" by decision #2 (question 10).

## Problem and goals

The customer administers and maintains houses and buildings owned by
clients. Today there is no single system for assets, costs and the work done. Goals: one place
for assets per owner, all expenses per asset, all administration/maintenance activities (the
customer's income), invoicing them to owners, and insight into income vs expenses.

## Stakeholders and user groups
- Staff (administrator): manages owners, assets, expenses, activities, invoices, VAT rates.
- Owner (client): signs in and views only their own assets, activities, expenses and invoices.

**As-is:** unknown tooling (question 9). **To-be:** web application in Docker with PostgreSQL,
sign-in for staff (owners later, decision #2). Constraint from the customer: .NET 10 (question 18,
D-012).

**Business rules**
- BR-1 Every asset belongs to exactly one owner.
- BR-2 Every expense and activity belongs to exactly one asset.
- BR-3 An activity is priced either as hours x hourly rate or as a fixed price.
- BR-4 An expense can optionally be marked as re-invoiceable to the owner.
- BR-5 VAT rates (now 21 / 9 / 0 %) are configurable with a start and end date; the rate valid
  on the activity/expense date is applied.
- BR-6 Currency is EUR.
- BR-7 An invoice is for one owner, contains uninvoiced activities and re-invoiceable expenses,
  has status Open or Paid, and is available as PDF. An invoiced line cannot be invoiced twice.
- BR-8 An owner can only see their own data.
- BR-9 An owner is never deleted, only marked inactive (Q-026).
- BR-10 When an asset moves to another owner, its uninvoiced lines move with it; invoiced lines
  stay with the old owner (Q-027).
- BR-11 Payment term is 14 days: due date = invoice date + 14 days (Q-028).

**Glossary**
- Owner: client who owns one or more assets.
- Asset: a building or home.
- Expense: a cost made for an asset.
- Activity: administration or maintenance work done for an asset; income.
- Invoice: bill to an owner for activities and re-invoiced expenses.
- VAT rate: percentage with a validity period.
- Inactive owner: an owner that is no longer active; kept with its history, never deleted.
- Due date: the date by which an invoice must be paid (invoice date + 14 days).
- Overdue: an Open invoice whose due date has passed.
- Full address: street, house number (with addition, for example 12A), postal code, city and country; all five required for owners and assets (Q-087).
- KvK number: the owner's Dutch Chamber of Commerce (Kamer van Koophandel) registration number; optional.
- BTW number: the owner's VAT identification number; optional.

## User stories

| ID | Story | Priority |
|---|---|---|
| US-1 | As staff, I want to sign in, so that the data is protected. | Must |
| US-2 | As staff, I want to manage owners, so that assets can be assigned to them. | Must |
| US-3 | As staff, I want to manage assets (building/home, address, owner), so that I know what we maintain. | Must |
| US-4 | As staff, I want to record expenses per asset (date, description, category, amount excl. VAT, VAT rate, re-invoiceable), so that I know the costs per asset. | Must |
| US-5 | As staff, I want to record activities per asset (date, type administration/maintenance, description, hours x rate or fixed price, VAT rate), so that I know our income. | Must |
| US-6 | As staff, I want to manage VAT rates with start and end dates, so that rate changes are handled. | Must |
| US-7 | As staff, I want to create an invoice for an owner from uninvoiced activities and re-invoiceable expenses, download it as PDF and mark it paid, so that I get paid. | Must |
| US-8 | As staff, I want a report of income vs expenses per asset per period, so that I see the result per asset. | Must |
| US-9 | As staff, I want an overview per owner with open invoices, so that I can follow up payments. | Must |
| US-10 | As owner, I want to sign in and see only my assets, activities, expenses and invoices, so that I have insight. | Must; moved to later by decision #2 |

**Acceptance criteria (summary)**
- US-1: Given no session, when opening any page, then the sign-in page is shown.
- US-4/5: Given VAT rate 21 % valid on the date, when saving, then VAT and total are calculated with that rate.
- US-5: Given hours 2 and rate 50, then amount excl. VAT is 100; given fixed price 80, then 80.
- US-6: Given two rates with overlapping periods for the same name, when saving, then it is refused.
- US-7: Given an invoiced activity, when creating a new invoice, then it is not offered again; the PDF shows lines, VAT per rate and total.
- US-8: Given a period, then per asset income, expenses and result are shown, totals match the lines.
- US-10: Given owner A signed in, when requesting data of owner B (also via URL), then access is denied.
- US-2.1: Given staff signed in, when they save a new owner with name, full address (street, house number, postal code, city and country), email and phone, then the owner appears in the owner list; when one of these is missing, saving is refused with a message (Q-025, Q-087). The Chamber of Commerce (KvK) number and the VAT (BTW) number are optional, because owners can be private persons; when filled in, a number in the wrong format is refused with a message: a KvK number is exactly 8 digits; a BTW number is any EU VAT number format, one of the 27 EU member-state prefixes (Greece `EL`) followed by 2 to 12 letters or digits, and for `NL` the form NL123456789B01, spaces and dots ignored (Q-088; for example DE123456789 and BE0123456789 are accepted, XX123 and NL123 are refused). Foreign addresses are accepted: an owner with a country other than the Netherlands and a postal code in another format (for example "1000" for Brussels) can be saved; the address fields are only checked as filled in (Q-088).
- US-2.2: Given an existing owner, when staff change and save the owner's data, then the list and the owner's assets show the new data.
- US-2.3: Given an owner that still has assets, when staff try to delete the owner, then it is refused with a message, because every asset must keep exactly one owner (BR-1).
- US-2.4: Given any owner (with or without assets or invoices), when staff want to remove the owner, then the owner is never deleted, only marked inactive, and its invoices stay available (Q-026; retention and anonymization of inactive owners: REQ-024).
- US-2.5: Given an owner with a full address, and optionally a KvK and BTW number, when staff download an invoice of that owner as PDF, then the PDF shows the owner's name, street and house number, postal code and city, and country, and the KvK and BTW number only when they are filled in, always as the owner's current details: after a change of the owner, a PDF of an older invoice shows the new details too; no copy of the owner data is kept on the invoice date (Q-087, Q-089).
- US-3.1: Given an existing owner, when staff save a new asset with type building or home, an address and that owner, then the asset appears in the asset list with its owner.
- US-3.2: Given a new or changed asset without an owner, when staff save it, then it is refused (BR-1).
- US-3.3: Given an asset form, when staff choose the type, then only building and home can be chosen; when staff save an asset without the full address (street, house number, postal code, city and country, each required), then saving is refused with a message (Q-025, Q-087).
- US-3.4: Given an asset with uninvoiced and invoiced activities or expenses, when staff change its owner, then the uninvoiced lines move with the asset to the new owner (offered on the new owner's next invoice), and the invoiced lines and their invoices stay with the old owner (Q-027).
- US-9.1: Given owners with invoices that are Open and Paid, when staff open the overview, then per owner only the Open invoices are shown, with number, date and total incl. VAT.
- US-9.2: Given the overview, then the open amount per owner equals the sum of the totals of that owner's Open invoices.
- US-9.3: Given an Open invoice, when staff mark it Paid, then it no longer appears in the overview and the owner's open amount drops by its total.
- US-9.4: Given an Open invoice, then the overview shows its due date as invoice date + 14 days; given today is after the due date, then the invoice is marked overdue (Q-028).
- US-9.5: Given an owner without Open invoices, when staff open the overview, then that owner is not listed (Q-028).

## Success measures

all assets, expenses and activities in the system; invoices produced
from the system; 0 cases of owners seeing other owners' data.

**Coverage check (analyst, design review, 2026-11-06):** US-1 to US-10 all mapped to F1–F3 and
B1–B3. Covered.

## Acceptance check

Per user story: not done yet.
