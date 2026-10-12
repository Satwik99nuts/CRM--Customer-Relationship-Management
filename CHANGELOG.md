# Changed-file notes

One-line purpose for each file changed for assignment completion. The existing project files retain their original names, including lowercase model/context filenames.

| File | Reason |
|---|---|
| `.gitignore` | Exclude build output, local databases, backups, and local settings from Git. |
| `API_DOCUMENTATION.md` | Document routes, authentication, request fields, and API responses. |
| `ASSIGNMENT_COMPLIANCE.md` | Map assignment topics to code, status, gaps, and manual checks. |
| `README.md` | Correct the clone URL and explain setup, migrations, API, and checklist. |
| `Program.cs` | Configure roles, secure Identity, cookie behavior, rate limiting, migration, and admin provisioning. |
| `Controllers/ActivitiesController.cs` | Add role and owner scoping, validated associations, filters, paging, and audit events. |
| `Controllers/CustomersController.cs` | Add role scoping, duplicate checks, history-safe deletion, search, paging, and audit. |
| `Controllers/FollowUpsController.cs` | Enforce scheduling and association rules; add owner scoping, filters, and history. |
| `Controllers/HomeController.cs` | Supply role-scoped and date-filtered dashboard data and KPIs. |
| `Controllers/LeadsController.cs` | Add safe deletion, status transitions, assignment, filters, and conversion workflow. |
| `Controllers/OpportunitiesController.cs` | Enforce opportunity rules, role scoping, linked follow-up safety, and audit. |
| `Controllers/AuditLogsController.cs` | Keep logs Admin-only and support action/module/user/date filters. |
| `Controllers/AuthController.cs` | Provide registration, login, logout, access denied, and auth event logging. |
| `Controllers/UsersController.cs` | Add Admin user creation, role/active management, password reset, and safeguards. |
| `Controllers/ReportsController.cs` | Implement filtered, scoped reports and safe CSV export. |
| `Controllers/ApiAuthController.cs` | Add cookie-based API login/logout, anti-forgery token, and login rate limiting. |
| `Controllers/ApiCustomersController.cs` | Add validated and role-scoped customer REST endpoints. |
| `Controllers/ApiLeadsController.cs` | Add validated and role-scoped lead REST endpoints. |
| `Controllers/ApiOpportunitiesController.cs` | Add validated and role-scoped opportunity REST endpoints. |
| `Controllers/ApiFollowUpsController.cs` | Add validated and role-scoped follow-up REST CRUD endpoints. |
| `Controllers/ApiReportsController.cs` | Expose a role-scoped pipeline reporting endpoint. |
| `Data/application_db_context.cs` | Register Identity users, unique customer/lead identifiers, and restricted relationships. |
| `Data/ApplicationDbContextFactory.cs` | Allow EF tooling to create the context with the configured SQLite database. |
| `Migrations/20261012141728_AssignmentCompliance.cs` | Add assignment fields and indexes while backfilling existing rows. |
| `Migrations/20261012141728_AssignmentCompliance.Designer.cs` | Record EF migration metadata for the assignment schema change. |
| `Migrations/20261012142425_FollowUpOpportunity.cs` | Add optional follow-up to opportunity relationship with restricted deletion. |
| `Migrations/20261012142425_FollowUpOpportunity.Designer.cs` | Record EF migration metadata for the follow-up relationship. |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | Keep EF's model snapshot aligned with current entities and relationships. |
| `Models/ApplicationUser.cs` | Add display name and activation state to Identity users. |
| `Models/activity.cs` | Add activity validation, length limits, and assignment relationship. |
| `Models/auditlog.cs` | Store outcome, old/new values, and request IP for audit details. |
| `Models/customer.cs` | Add customer code, state, owner, modification date, and history relationships. |
| `Models/followup.cs` | Add optional opportunity association for scheduling follow-ups on deals. |
| `Models/lead.cs` | Add lead code, owner, conversion link, and validated status/value fields. |
| `Models/opprtunity.cs` | Add lead/owner relationships and stage, status, amount, probability, and date validation. |
| `Services/ActiveUserSignInManager.cs` | Block inactive accounts and audit successful/failed sign-in attempts. |
| `Services/AuditService.cs` | Record result, old/new values, actor, and IP without logging credentials. |
| `Services/CrmAccessService.cs` | Centralize role-based record scoping and assignment validation. |
| `ViewModels/ApiDtos.cs` | Define API input/output shapes and server-side validation annotations. |
| `ViewModels/AuthViewModels.cs` | Validate login, registration, user edit, and password reset forms. |
| `ViewModels/DashboardViewModel.cs` | Provide typed dashboard KPIs, chart series, filters, and recent activity. |
| `ViewModels/ReportViewModel.cs` | Provide typed report filters, rows, and pagination data. |
| `Views/Activities/Index.cshtml` | Add activity search/filter/paging controls and status feedback. |
| `Views/Activities/_ActivityForm.cshtml` | Restrict activity type and assignment choices to valid values. |
| `Views/AuditLogs/Index.cshtml` | Show event results and before/after values with existing audit filters. |
| `Views/Auth/AccessDenied.cshtml` | Give unauthorized signed-in users a clear access-denied page. |
| `Views/Auth/Login.cshtml` | Provide validated login form with status messaging. |
| `Views/Auth/Register.cshtml` | Provide validated registration form and explain default role. |
| `Views/Customers/Delete.cshtml` | Explain history-preserving deactivation before customer deletion. |
| `Views/Customers/Details.cshtml` | Show linked activities, follow-ups, and opportunities as customer history. |
| `Views/Customers/Index.cshtml` | Add customer search/status filters and pagination. |
| `Views/Customers/_CustomerForm.cshtml` | Expose customer state and permitted owner assignment fields. |
| `Views/FollowUps/Create.cshtml` | Allow a follow-up to target a customer, lead, or opportunity. |
| `Views/FollowUps/Edit.cshtml` | Preserve and edit the single linked record and assignment. |
| `Views/FollowUps/Details.cshtml` | Display opportunity linkage in follow-up details. |
| `Views/FollowUps/Index.cshtml` | Add overdue messaging, search/date/status/owner filters, and paging. |
| `Views/Home/Index.cshtml` | Render typed KPIs, date filters, charts, and recent activities. |
| `Views/Leads/Delete.cshtml` | Provide the missing deletion confirmation page and explain history guards. |
| `Views/Leads/Details.cshtml` | Show lead history and the qualified-lead conversion action. |
| `Views/Leads/Index.cshtml` | Add lead search/status filters and pagination. |
| `Views/Leads/_LeadForm.cshtml` | Expose lead source, value, status, and authorized owner selection. |
| `Views/Opportunities/Create.cshtml` | Add customer/lead links and allowed owner selection. |
| `Views/Opportunities/Edit.cshtml` | Preserve editable customer/lead links and owner selection. |
| `Views/Opportunities/Delete.cshtml` | Explain why linked follow-ups prevent opportunity deletion. |
| `Views/Opportunities/Details.cshtml` | Show related lead, customer, and follow-up history. |
| `Views/Opportunities/Index.cshtml` | Add opportunity search/stage/status filters and paging. |
| `Views/Reports/Index.cshtml` | Present report selection, filters, pagination, and CSV export. |
| `Views/Shared/_Layout.cshtml` | Make navigation role-aware and use a protected POST for logout. |
| `Views/Users/Create.cshtml` | Add Admin user creation and role selection form. |
| `Views/Users/Edit.cshtml` | Add Admin user role, activation, and account controls. |
| `Views/Users/Index.cshtml` | Add Admin user list, search, pagination, and management links. |
| `Views/Users/ResetPassword.cshtml` | Provide a protected password reset form without displaying stored secrets. |
