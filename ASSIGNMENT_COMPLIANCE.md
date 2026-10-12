# Assignment compliance checklist

This maps the assignment requirements to the current implementation. “Implemented” means the relevant path is present and was reviewed; runtime behavior is only claimed as tested where an observed test is reported separately.

| Requirement | Code evidence | Status | Remaining work / evidence |
|---|---|---|---|
| Registration, login, logout, password policy, lockout, access denied | `AuthController`, `ActiveUserSignInManager`, `Program.cs`, `Views/Auth/*` | Implemented | Verify lockout and inactive-user behavior in the manual evaluation checklist. |
| Admin, Manager, Sales Executive authorization | `Program.cs` role seeding; `[Authorize]` on controllers; `CrmAccessService` query scoping | Implemented | Manager access is organization-wide because the data model has no team entity. |
| Dashboard totals, opportunity states, pipeline, charts | `HomeController`, `DashboardViewModel`, `Views/Home/Index.cshtml` | Implemented | Manually compare displayed totals with the isolated database when demoing. |
| Customer CRUD, search and history | `CustomersController`, `Views/Customers/*`, `Customer` | Implemented | Delete with related history deactivates the customer. |
| Lead CRUD, search/status, assignment and conversion | `LeadsController`, `Views/Leads/*`, `Lead` | Implemented | Lead deletion is blocked where history/conversion exists. |
| Follow-up scheduling, pending/status, date rules and history | `FollowUpsController`, `Views/FollowUps/*`, `FollowUp` | Implemented | Reminders are represented as overdue/pending dashboard and list status; no email/push notification service exists. |
| Call, meeting, email and task activities | `ActivitiesController`, `Views/Activities/*`, `Activity` | Implemented | Activity CRUD and association rules are enforced server-side. |
| Opportunity CRUD, stages, amount/probability/date validation | `OpportunitiesController`, `Views/Opportunities/*`, `Opportunity` | Implemented | Close-date checks apply to open opportunities. |
| User creation/listing, role assignment and activation | `UsersController`, `Views/Users/*`, `ApplicationUser` | Implemented | Admin-only; the last Admin and self-protection checks are enforced. |
| Audit of security and business events; filtering | `AuditService`, `ActiveUserSignInManager`, controllers, `AuditLogsController`, `Views/AuditLogs/Index.cshtml` | Implemented | Audit page caps results at 500. Auth failures and record changes are logged; the result and old/new values are shown. |
| Customer, lead, opportunity, follow-up, auth and reporting APIs | `Api*Controller` classes; `API_DOCUMENTATION.md` | Implemented | Cookie auth is used. API auth POSTs require the anti-forgery token. No OpenAPI/Swagger UI is configured. |
| Customer, lead, follow-up, opportunity, pipeline, user activity and audit reports | `ReportsController`, `ApiReportsController`, `ReportViewModel`, `Views/Reports/Index.cshtml` | Implemented | CSV export is provided. Reports load filtered rows before in-memory paging, so very large datasets may need query-level paging/aggregation. |
| Responsive pages, validation/messages/confirmation/search/filter/pagination | MVC views, Bootstrap layout, controller validation and TempData messages | Partial | Search/filter and pagination exist on key lists. Sort controls are not available on every list; audit is capped rather than paginated. |
| Server validation, anti-forgery, error handling, secrets | Data annotations and controller checks; `ValidateAntiForgeryToken`; production exception handler/HSTS; configuration | Implemented | No secrets are hard-coded. HTTPS redirection should be checked in the target hosting environment. |
| Preserve data and safely migrate schema | additive EF migrations in `Migrations/`; foreign-key restrictions; lead delete guard | Implemented | Migrations were applied to a backup copy of the current DB for validation. The live DB migration is applied automatically at app startup; back it up first. |

## Manual evaluation checklist

- Login with a valid account, an invalid password, and an inactive account; confirm repeated failures lock the account. Log out and confirm protected pages require a new login.
- Use Admin, Manager and Sales Executive accounts. Confirm Admin-only Users and Audit pages reject the other roles; confirm Sales Executives only see assigned records and cannot change ownership by tampering with a POST/API request.
- Create, edit, search, inspect, and delete a customer with and without history. Confirm linked records are retained when a customer is deactivated.
- Create/edit/filter a lead, change allowed statuses, convert a Qualified lead, and try deleting leads with follow-ups, activities, opportunities, or a converted customer.
- Create/edit/complete/cancel a follow-up and test a planned date in the past. Link it to each supported entity type.
- Create/edit/delete calls, meetings, emails, and tasks; test invalid associations and assignment.
- Create/edit/delete opportunities; test amount 0, probability below 0/above 100, a past close date for an open deal, and Won/Lost totals.
- Create users with each role, deactivate/reactivate a user, and verify permission changes take effect on the existing session.
- Confirm audit rows for login success/failure, logout, and business create/update/delete; filter by action, module, user, and date.
- Call every API route unauthenticated and authenticated; check validation errors, 201/200/204, 401/403/404/409 responses, role scoping, and anti-forgery on API login/logout.
- Compare dashboard totals and chart series with the database; use date/search/status/owner report filters and CSV export.
- Submit invalid and tampered form/API values and confirm server validation rejects them; test responsive layouts at a narrow viewport.
