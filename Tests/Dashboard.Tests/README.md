# Dashboard verification (B32)

Run from the repository root against a local development SQL database with existing customers, packages and services:

```powershell
dotnet run --project Tests/Dashboard.Tests/Dashboard.Tests.csproj
```

This compares dashboard view models with database aggregates and checks all six revenue months, PAID-only revenue, booking status counts, recent ordering, employee counters and empty datasets. Payment and booking boundary fixtures are inserted inside a transaction and rolled back; no fixture records persist. As with any rolled-back SQL insert, identity sequences may advance. Do not run these fixture checks against production.

Browser verification uses Python Playwright and installed Microsoft Edge. Start the application on `http://127.0.0.1:5187`, then run:

```powershell
python -m pip install playwright
python Tests/Dashboard.Tests/verify_browser.py
```

The browser script signs in with the existing development seed accounts. It checks all 21 combinations of ADMIN/EMPLOYEE/CUSTOMER and the seven routes in B32's access matrix, real Chart.js bar/doughnut instances, six revenue labels, four booking status values, mobile horizontal overflow and JavaScript errors. Screenshots and chart data are written under ignored `obj/`. Chart.js is loaded from the requested CDN and requires browser internet access.

Verified on 2026-10-15: SQL checks passed, both charts instantiated, all 21 access checks passed, and Admin/Employee mobile layouts had no horizontal overflow. Screenshots were inspected. Dashboard links already routed to the correct controllers, so no duplicate navigation items were added. Build succeeded with zero errors; existing NU1902/NU1903 warnings remain for System.Security.Cryptography.Pkcs 9.0.11.

No database schema changes. Today's figures use application-local calendar-day boundaries. The PT preview covers scheduled sessions from now through the next 14 days. Revenue uses PaidAt within calendar-month boundaries, only for PAID payments. The six-month chart includes zero-revenue months. Recent lists and charts are read-only queries; monthly totals are aggregated in SQL.
