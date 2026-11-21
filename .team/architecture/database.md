# Database architecture

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged). The entity-relationship diagram stays in architecture.md._

- Database: PostgreSQL (container with a volume); EF Core with Npgsql; migrations applied at
  start-up (decision #3).
- Entities: Owner(Id, Name, Email, Phone, Street, HouseNumber, PostalCode, City, Country,
  KvkNumber?, VatNumber?), Asset(Id, OwnerId, Type{Building,Home}, Name, Street, HouseNumber,
  PostalCode, City, Country), VatRate(Id, Name, Percentage, ValidFrom, ValidTo?),
  Expense(Id, AssetId, Date, Description, Category, AmountExclVat, VatPercentage, Reinvoiceable,
  InvoiceLineId?), Activity(Id, AssetId, Date, Kind{Administration,Maintenance}, Description,
  PricingType{Hourly,Fixed}, Hours?, HourlyRate?, FixedPrice?, VatPercentage, InvoiceId?),
  Invoice(Id, Number, OwnerId, Date, DueDate, Status{Open,Paid}, PaidDate?),
  InvoiceLine(Id, InvoiceId, Description, AmountExclVat, VatPercentage, SourceType, SourceId).
- Address, KvK and BTW (D-041, Q-087; owners in S2, assets in S3). Owner and Asset have the same
  five required address columns: `Street` varchar(200), `HouseNumber` varchar(20, with
  addition such as "12A"), `PostalCode` varchar(20), `City` varchar(100), `Country`
  varchar(100, free text); all `NOT NULL`, validated as not empty after trimming (400 per field).
  Owner also has `KvkNumber` varchar(8) and `VatNumber` varchar(14), both nullable; when given,
  KvK is exactly 8 digits and BTW is normalized (spaces and dots removed, upper case) and must
  be an EU VAT number format (Q-088): one of the 27 member-state prefixes (AT, BE, BG, CY, CZ,
  DE, DK, EE, EL, ES, FI, FR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK;
  Greece is `EL`; source Your Europe, "Check a VAT number (VIES)", checked 2026-11-20)
  followed by 2 to 12 letters or digits, so at most 14 characters; for `NL` exactly 9 digits +
  `B` + 2 digits. No check against VIES (no outbound integration, Q-051). Foreign addresses are
  accepted: the address fields are only checked as filled in, never against a Dutch format. The checks
  live in the use cases' `RequestValidation.cs` (Create, Update) of `V1/Owners` and `V1/Assets`;
  the frontend shows the 400 per field. Today the owner has `Address`, `PostalCode`, `City`
  (nullable) and the asset `Address`, `PostalCode`, `City` (required), without country.
- Invoice PDF owner block (`PropertyAdmin.Logic/V1/Invoices/GetPdf/InvoicePdfRenderer.cs`,
  D-032): name; `Street HouseNumber`; `PostalCode City`; `Country`; then `KvK: …` and
  `Btw-nr: …` (the labels of the company block above it), each line only when filled in. The
  data is read from the current owner record every time the PDF is drawn, so a PDF of an older
  invoice shows the owner's new details after a change (Q-089 a: no copy on the invoice).
- Ownership: only the API accesses the database (decision #1).
- Database identities and rights (task 2, decision #24): `postgres` superuser only for the
  container's first start and for backups; `propertyadmin_migrator` owns the schema and is used
  only to apply migrations; `propertyadmin_app` used by the API at run time with SELECT, INSERT,
  UPDATE, DELETE on the application tables and USAGE on sequences, no DDL. Passwords from
  environment variables (REQ-20). Network: the database has no published port and is reachable
  only on the internal compose network by the API.
- Main queries, volumes and indexes. Expected scale (Q-033, Q-040, REQ-022): 100 owners, 200
  assets, 10,000 activities and up to 10,000 expenses per year. With seven years retained, this
  is about 70,000 activity rows and up to 70,000 expense rows. Lists and reports must respond
  in under 2 seconds. This fits one PostgreSQL instance; the indexes below and paging support
  the expected queries.
  | Query | Index |
  |---|---|
  | Assets per owner | `Asset(OwnerId)` (FK index) |
  | Expenses / activities per asset per period (REQ-4, REQ-5, REQ-8) | `(AssetId, Date)` on both Expense and Activity (exists) |
  | Uninvoiced activities / re-invoiceable expenses per owner (REQ-7) | partial index `(AssetId) WHERE InvoiceId IS NULL` (to add) |
  | Open invoices per owner, overdue (REQ-9) | `Invoice(OwnerId, Status, DueDate)` (to add) |
  | VAT rate on a date (REQ-6) | `VatRate(Name, ValidFrom)` unique (exists) |
  | Invoice by number | `Invoice(Number)` unique (exists) |

Every list endpoint is paged (default 50, maximum 200) and sorted on a stable key.
- Transactions and concurrency: creating an invoice, marking it paid, deleting an open invoice
  and moving an asset to another owner (question 27) run in one transaction each. Optimistic
  concurrency with PostgreSQL `xmin` as row version on Owner, Asset, Expense, Activity, Invoice
  and VatRate; a conflict returns 409 Problem Details. Invoicing additionally selects its source
  rows with `InvoiceId IS NULL` inside the transaction, so a line can never be invoiced twice.
  Invoice numbers are the highest number of the year + 1, computed inside the create transaction
  and guarded by the unique index with a retry; no PostgreSQL sequence, because a sequence leaves
  gaps on a rollback (D-056, replaces the sequence clause of D-024). Only the most recent open
  invoice can be deleted, so the numbers stay without gaps (Q-083 c, D-056). Create and delete
  first take a per-year PostgreSQL transaction lock (`pg_advisory_xact_lock(7001, year)`, held
  until commit or rollback) and only then read the numbers, so a delete and a create of the same
  year at the same moment cannot leave a gap (Q-118 b, T-180; PostgreSQL test TC-REQ7-1). The
  SQLite test database has no such lock and skips it. Key 7001 is reserved for invoice numbers;
  a later advisory lock takes another first key.
- Keeping owners' data apart: one database, no tenants. Staff roles see all owners according to
  their permissions. For the later owner role, the OwnerId comes only from the authenticated
  user's claim, never from the route or body, and every owner query filters on it through one
  shared query extension; tests prove that owner A cannot read owner B (REQ-19).
- Personal data: Owner name, email, phone, address (five fields), KvK number and BTW number
  (D-041); asset address; user name, email, phone,
  password hash and 2FA secret. Owners are never deleted, only marked inactive (question 26).
  Invoices and their lines are kept at least 7 years from invoice date (Q-034, REQ-024). Seven
  years after an owner is marked inactive (Q-044), the system proposes anonymization only if all
  their invoices have also met the 7-year retention; an Admin must confirm each proposal
  (Q-041). Replace the owner's name, email, phone, address fields, KvK and BTW number, and asset
  address fields, with fixed
  placeholders; amounts, numbers and dates stay. Users are deactivated, not deleted, while they
  are referenced. Personal data is never written to logs.
- Migrations: EF Core migrations in `PropertyAdmin.Api/Data/Migrations`, reviewed in the pull
  request; every migration must stay compatible with the previous API version (add first, remove
  in a later release). Production applies migrations with an idempotent SQL script or a
  separate migration run as `propertyadmin_migrator`, not at API start-up (decision #24);
  start-up migration stays for Development only.
  Address migrations (D-041): one for owners (S2) and one for assets (S3), each in its own
  commit. Each renames `Address` to `Street` (the data stays), adds `HouseNumber` and `Country`
  as `NOT NULL DEFAULT ''`, and for owners makes `Street`, `PostalCode` and `City` `NOT NULL`
  (existing nulls become `''`) and adds nullable `KvkNumber` and `VatNumber`. Recorded
  deviation from "add first, remove later": no version is deployed (Q-079). Existing laptop
  rows show an empty house number and country until staff save them again, which then asks
  for both. Run `dotnet ef migrations has-pending-model-changes` after each.
- Backup and restore (REQ-023, D-024): RPO 24 hours and RTO 2 days (Q-037 replaces RPO 0
  from Q-033: no second machine, so no standby server and no WAL archiving). A `backup` service
  makes a daily `pg_dump` (custom format) to a backup volume separate from the database
  volume, keeps 14 daily and 12 monthly dumps, and logs a failure as an error. The
  data-protection keys volume is backed up with it. Restore is tested at least every quarter
  and before every PostgreSQL major upgrade. **Accepted risk (Q-039):** backup storage remains
  on the production host; losing the host loses the database and every backup. There is no
  off-host copy in the current scope. Hosting is capped at 1,000 euro per year including
  server, domain and backup storage (Q-042, REQ-027); maintenance has a separate cap of 100
  euro per month, paid services only, checked per calendar month (Q-046, Q-048, REQ-027). The
  current design uses no paid service (Let's Encrypt, open-source images and libraries).
