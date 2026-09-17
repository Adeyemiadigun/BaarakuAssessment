# Library API — Backend Take-Home Assessment

Small ASP.NET Core 10 “Library” API implementing **Task 2** of the take-home assessment
(borrow / return endpoints), plus the Task 3 LINQ helpers and a test suite.
Written answers for **Tasks 1, 3 and 4** live in `Assessment — Tasks 1, 3, 4.docx`
in this same folder.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` → 10.x)
- No database server needed — the API uses the EF Core InMemory provider.

## Run it

```bash
# from this folder (library-assessment/)
dotnet build LibraryApi.slnx
dotnet test LibraryApi.slnx
dotnet run --project src/Library.Api
```

Then open Swagger UI (served at the site root in Development):

- Swagger UI: `https://localhost:7056` (or `http://localhost:5025`)
- Swagger JSON: `https://localhost:7056/swagger/v1/swagger.json`

On startup the API seeds 3 books (Dune ×2, Clean Code ×1, Pragmatic Programmer ×3)
if the store is empty — see `src/Library.Api/Data/SeedData.cs`.

## Endpoints

| Method & route | Body | Success | Failures |
|---|---|---|---|
| `POST /api/books/{id}/borrow` | `{ "borrowerEmail": "alice@example.com" }` | `200` + created `Loan` | `400` missing/invalid email · `404` book not found · `409` no copies available · `409` borrower already holds an active loan |
| `POST /api/books/{id}/return` | `{ "borrowerEmail": "alice@example.com" }` | `200` + returned `Loan` | `404` book not found · `404` borrower has no active loan for this book |

Example:

```bash
curl -X POST http://localhost:5025/api/books/1/borrow \
  -H "Content-Type: application/json" \
  -d '{"borrowerEmail":"alice@example.com"}'
# {"id":1,"bookId":1,"borrowerEmail":"alice@example.com",
#  "borrowedAt":"2026-09-17T10:34:21Z","returnedAt":null}
```

Errors are RFC 9457 `ProblemDetails`, e.g. borrowing the last copy twice:

```json
{ "title": "No copies available", "status": 409,
  "detail": "Book with id 2 has no available copies (all 1 on loan)." }
```

## Project structure

```text
library-assessment/
├── LibraryApi.slnx
├── README.md                              ← this file
├── Assessment — Tasks 1, 3, 4.docx        ← written answers (Tasks 1, 3, 4)
├── src/Library.Api/
│   ├── Program.cs                         ← DI wiring, middleware pipeline, seeding
│   ├── Controllers/BooksController.cs     ← thin HTTP layer (2 actions)
│   ├── Services/IBookService, BookService ← business rules (borrow/return)
│   ├── Repositories/                      ← data access (EF Core queries live here)
│   │   ├── IBookRepository, BookRepository
│   │   └── ILoanRepository, LoanRepository
│   ├── Domain/BookLoanExceptions.cs       ← typed domain failures (each maps to a status code)
│   ├── Middleware/BookLoanExceptionHandler← global IExceptionHandler → ProblemDetails
│   ├── Data/LibraryDbContext.cs           ← EF model configuration
│   ├── Data/SeedData.cs                   ← idempotent demo seed data
│   ├── Models/Book, Loan                  ← entities (as given in the brief)
│   ├── Models/BorrowRequest.cs            ← request DTO with validation attributes
│   └── Queries/LoanQueries.cs             ← Task 3 pure-LINQ helpers
└── tests/Library.Api.Tests/
    ├── BookServiceTests.cs                ← 9 tests: success + every rejection path
    └── LoanQueriesTests.cs                ← 4 tests for the Task 3 helpers
```

## Architecture

Request flow (one direction, no shortcuts):

```text
HTTP → BooksController → IBookService → IBookRepository / ILoanRepository → LibraryDbContext
                              │                                              (EF Core InMemory)
                              └── throws LibraryDomainException on rule violation
                                            │
                              BookLoanExceptionHandler (IExceptionHandler)
                                            └── ProblemDetails with the right status code
```

- **Controller** — parameter binding, auth-agnostic delegation, nothing else. Two
  one-liner actions; no `try/catch`, no status-code logic.
- **Service (`BookService`)** — owns every business rule: book must exist, an
  available copy must exist, the borrower must not already hold an active loan,
  return targets the *most recent* active loan. It orchestrates repositories but
  contains no EF query code itself.
- **Repositories** — own all data access (`Include`, `Where`/`OrderBy`,
  `SaveChangesAsync`). This keeps queries reusable, keeps the service
  persistence-ignorant, and lets tests exercise the exact production code path.
- **Domain exceptions** — each rule violation is a typed exception carrying its
  HTTP status (`BookNotFoundException` → 404, `NoCopiesAvailableException` → 409,
  `DuplicateActiveLoanException` → 409, `ActiveLoanNotFoundException` → 404).
- **Global handler** — `BookLoanExceptionHandler` (registered via
  `AddExceptionHandler` + `UseExceptionHandler`) converts domain exceptions to
  `ProblemDetails` and lets anything else fall through to the default 500
  handling. Controllers stay free of error-mapping code.

## Why each implementation choice

- **Exceptions over `Result<T>` wrappers** — keeps service signatures honest
  (`Task<Loan>`, no nulls, no out-params) while still producing precise HTTP
  mapping in exactly one place. Either pattern is defensible; this matches the
  ASP.NET Core 8+ `IExceptionHandler` guidance.
- **`409 Conflict` for no-copies and duplicate loans** — the request is
  well-formed but conflicts with current resource state; `400` is reserved for
  malformed input, `404` for missing resources.
- **`200 OK` on borrow** — the brief says “returns the created loan” without
  specifying a code. `201 Created` would also be defensible; `200` was chosen
  for simplicity and stated as a judgment call in the docx.
- **`BorrowRequest` DTO + `[ApiController]`** — `[Required]`/`[EmailAddress]`
  turn missing/invalid emails into automatic `400 ProblemDetails` responses;
  the service never sees unvalidated input.
- **Case-insensitive email comparison** — `a@x.com` and `A@X.COM` are treated
  as the same borrower for the duplicate-loan check.
- **`DateTime.UtcNow` everywhere** — no local-time ambiguity in
  `BorrowedAt`/`ReturnedAt` or the 14-day overdue calculation.
- **`CancellationToken` throughout** — controller → service → repository →
  EF async calls, so abandoned requests stop doing work.
- **`AsNoTracking` on read-only queries** (`GetAllAsync`) and indexes on
  `Loan(BookId)` / `Loan(BookId, BorrowerEmail)` — avoids change-tracker
  overhead and keeps lookups indexed if this moves to a relational provider.
- **EF Core InMemory** — zero-setup review (no Docker/SQL needed). Swapping to
  SQL Server/Postgres is a one-line change
  (`UseInMemoryDatabase` → `UseSqlServer`/`UseNpgsql`); no service or
  repository code changes.
- **Swashbuckle only for docs** — the built-in `Microsoft.AspNetCore.OpenApi`
  package was deliberately removed: it ships a conflicting `Microsoft.OpenApi`
  major version that breaks Swashbuckle 9 at runtime
  (`TypeLoadException` on `SwaggerGenerator.GetSwagger`). Swagger UI is served
  at the site root in Development.
- **`SeedData` as a separate static class** — keeps `Program.cs` to composition
  only (services, middleware, seed call). Seeding is idempotent (skips when
  books exist) and deliberately **synchronous**: it runs once at startup,
  before `app.Run()`, when there are no requests to overlap with — so `await`
  would add ceremony with zero benefit, and the InMemory provider completes
  synchronously under the hood anyway. The request path (controller → service
  → repositories) stays fully async, which is where async actually matters.
  If the provider ever moves to SQL Server/Postgres, this is the one call
  that would gain an async twin.
- **Scoped lifetimes** for `DbContext`, repositories and service — one instance
  per request; avoids the captive-dependency bug of sharing scoped state from
  a singleton, and lets the container dispose the context per request.
- **`LoanQueries` as pure static functions** — Task 3 operates on an already
  in-memory `List<Loan>`, so there is nothing to translate to SQL. Purity plus
  an injectable `now` parameter makes the overdue logic deterministic in tests.

## Assumptions

1. `/borrow` and `/return` both accept `{ "borrowerEmail": "…" }`. The brief
   only names the body for borrow, but “the borrower’s most recent unreturned
   loan” requires identifying the borrower, so the same DTO is reused.
2. `/return` with no matching active loan → `404` (“nothing to return” reads
   as not-found rather than a state conflict).
3. Books with zero active loans are absent from `GetActiveLoanCountsByBook`
   (rather than present with `0`).
4. “Top borrowers” counts *all* loans (active + returned); ties break
   deterministically by email.
5. Overdue means *strictly* older than 14 days; exactly-14-days is not overdue.
6. No authentication — `BorrowerEmail` is self-asserted. A production system
   would take the borrower identity from the auth token instead of the body.

## Known limitation

The availability check and the insert are not serialized: two concurrent
borrows could both observe “one copy left” and over-allocate. For this exercise
that is acceptable and documented; in production it needs a serializable
transaction, a row-level lock (`SELECT … FOR UPDATE`), or a database-level
constraint on the active-loan count.

## Tests

xUnit + EF Core InMemory, one isolated database per test class instance
(`Guid` database name, disposed afterwards). The service is constructed with
the **real** repositories, so tests run the same code path as production:

- `BookServiceTests` (9) — borrow success, book-not-found (borrow + return),
  no-copies-available, duplicate active loan, multi-copy/multi-borrower,
  case-insensitive email, return-marks-most-recent, return-with-no-active-loan.
- `LoanQueriesTests` (4) — overdue boundary (15d / 14d / returned / recent),
  top-borrower ordering, `topN` limit, active-counts-per-book map.

```bash
dotnet test LibraryApi.slnx
# Passed! - Failed: 0, Passed: 13, Total: 13
```
