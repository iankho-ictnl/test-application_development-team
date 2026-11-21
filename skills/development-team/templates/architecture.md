# Architecture

_Owner: architect. Lock this file before you change it (see `RULES.md`)._ The requirements
themselves are files in `requirements/`; the choices are decisions in `decisions/`. This file
describes the design and links to both.

## Architecture diagram

Mermaid, context and container level: users and other actors, components, external systems,
data flows, trust boundaries, and where authentication and authorization take place.

## Layers and components

By default: clients use the API, the API holds the business logic and retrieves the data, the
database stores the data. Each exception is a decision. Then the components, the data flows,
the interface contract between frontend and backend, and the infrastructure.

## API structure

The folders per version, feature and use case (by default), which project holds what, where
the business logic lives (the feature services), and the models with their mapping at every
boundary:

| Boundary | From | To | Where |
|---|---|---|---|
| Request in | API request | Query or command | API project, `<UseCase>/Mapping.cs` |
| Data in | Database entity or external data | Business model | Logic project, `<UseCase>/<Source>Mapping.cs` |
| Response out | Result | API response | API project, `<UseCase>/Mapping.cs` |
| Frontend | API response | View model | Frontend, API client layer |

Every exception to the default structure is a decision.

## Error handling

One central error handler for every endpoint; Problem Details responses
(`application/problem+json`) with a safe message and the trace id; no internal details in any
response. A request the client cancelled is not an error.

| Situation | Status | Logged as |
|---|---|---|
| Invalid request (validation) | 400, with the errors per field | information |
| Not signed in, or a missing, invalid or expired token | 401 | information |
| Signed in, but not allowed | 403 | warning |
| Not found, or not visible to this user | 404 | information |
| Conflict with the current state | 409 | information |
| Another system failed, or did not answer in time | 502 or 504 | error |
| Unexpected failure | 500, with a general message only | error, once, with the trace id |
| The client cancelled the request | 499, no error body | not an error |

## Technology versions

For every technology, the newest stable version whose official support ends latest (usually the
newest long-term support release), checked on the vendor's lifecycle page when decided. An end
of support within the expected life of the solution is a risk.

| Technology | Version | Release type (for example LTS) | End of support | Source (lifecycle page) | Decision |
|---|---|---|---|---|---|

## Database architecture

When the solution stores data. In plain words, with an entity-relationship diagram (Mermaid
`erDiagram`): which databases and why, the data model, which component owns which data, who
connects with which rights, the main queries with their volumes and indexes, transactions and
concurrency, how organizations' data is kept apart, personal data, schema migrations, and backup
and restore.

## Authentication and authorization

Who and what signs in, the identity provider and the sign-in flow per client, the tokens
(issuer, audience, scopes or roles, lifetime, validation), where each check is enforced, and the
authorization matrix (deny by default):

| Role | Action | Resource or data | Allowed | Condition (for example own organization only) |
|---|---|---|---|---|

## Build task list

The pieces of work, per sprint of `plan.md` and per requirement or use case: for each
requirement the backend piece, the screen and the tests together, so the team finishes it end to
end. Each piece with its owner role, its sprint, the item it is for, the requirements it covers,
the files or areas it touches, and an estimate in story points. Pieces that can run in parallel
touch different files. The orchestrator creates the task files of the current sprint.

| Piece | Owner role | Sprint | For (item) | Requirements | Files or area | Story points |
|---|---|---|---|---|---|---|
