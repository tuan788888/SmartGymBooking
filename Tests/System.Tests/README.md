# B33 system hardening verification

Run from the repository root against the local development SQL Server database. The workflow checks intentionally create labelled `B33QA...` records, simulate confirmation of test payments and preserve their history. They do not contact a bank or OpenAI. Never run these fixture scripts against production.

```powershell
dotnet build
dotnet run --project Tests/System.Tests/System.Tests.csproj
dotnet run --no-build --no-launch-profile --urls http://127.0.0.1:5187
```

In another terminal (Python 3.12, Playwright and installed Microsoft Edge):

```powershell
python Tests/System.Tests/verify_workflow.py
python Tests/System.Tests/verify_operations.py
python Tests/System.Tests/verify_security.py
python Tests/System.Tests/verify_browser.py
```

`verify_operations.py` uses the QA prefix written under ignored `obj/` by the preceding workflow run. It deliberately changes only that fixture's schedule timestamps to represent ended sessions for completion tests. The `age` helper requires a specific B33QA prefix. The `retire B33QA` helper disables test accounts/PTs/packages/services while retaining all transaction history. Development seed credentials are used only for the local test accounts.

## Completed checks — 2026-10-15

- Clean, restore, build and publish succeeded. Final build: **0 warnings, 0 errors**. Pkcs is pinned to 10.0.13, resolving the earlier NuGet audit warnings.
- Anonymous `/health` returns Healthy/application/UTC time. Missing routes return a friendly 404 with status 404; access-denied pages retain 403; the server-error page returns generic content with 500. Production exception handling targets `/Error/ServerError`; Development retains its diagnostic exception page.
- All 41 tested unsafe actions rejected missing antiforgery tokens. Customer mutation actions rejected GET with 404/405. Reflection checked explicit POST antiforgery attributes; a global AutoValidateAntiforgeryToken filter also covers future unsafe MVC actions.
- Registration ignored an injected ADMIN role. Future birth dates were rejected and displayed next to the field. Locked login used the same generic message as invalid credentials. Locking the QA customer revoked an already-issued cookie on its next request. Browser cookies were HttpOnly and SameSite=Lax; SecurePolicy follows the request scheme.
- DatabaseTest pages are now Admin-only. Direct role URL checks covered Customer/Employee access to restricted administration, customer edit/delete and database diagnostic pages.
- Customer search/details/profile edit; Employee create/edit/lock/delete; PT create/edit/image upload; Package/Service create/edit/inactive; Exercise create/edit/video/delete all ran with isolated QA records.
- Customer purchase → PENDING/QR → Admin PAID confirmation → exactly one ACTIVE membership; repeat confirmation created no duplicate membership.
- Booking creation, partial overlap, past time, inactive service, invalid duration and outside-membership rejection; confirm, customer/admin cancellation, check-in and duplicate prevention; future completion rejection and ended booking completion.
- PT scheduling within rolling 14 days; PT/customer overlap, locked customer, IncludesPT entitlement, invalid duration and beyond-14-day rejection; own customer PT list, cancel and ended-session completion.
- Customer with history, PT with schedule, package with payment/membership, service with booking and exercise used in a workout resisted deletion. GET Delete only displayed confirmation. No cascade or schema changes were introduced.
- ONNX produced a RandomForest/1.0 workout and exercises; the daily limit blocked a second plan. DEMO chat saved USER/ASSISTANT history and used that workout context. Cross-customer payment/workout/chat reads and payment/booking/chat writes were rejected; PT lists were owner-filtered. Existing AI mock-provider and dashboard SQL suites also passed after the dependency update.
- **152 page/viewport checks** passed at 1920, 1366, 768 and 390 pixels. Business tables were wrapped, pages did not overflow horizontally, and missing PT images fell back to accessible initials. Browser JavaScript checks passed. Empty states, status badges and the project footer were polished.
- SQL integrity: duplicate emails/customer users/membership payments/check-in bookings and invalid booking/PT time ranges all returned zero groups/rows. Current-source API-key-pattern scan found no matches; `.env`, development settings and build/cache outputs remain ignored. AI remains DEMO.

The QA accounts/resources were retired and history retained. Real paid OpenAI requests were not made. `main` is reserved for the later deployment release; B33 merges only into `develop`.
