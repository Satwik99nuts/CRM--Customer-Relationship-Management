# Acxiom CRM

Acxiom CRM is a web application for organizing customer relationships and sales work. It gives a team one place to track prospects, customers, opportunities, follow-ups, and the activity around each relationship.

## Why use it?

Sales work often gets spread across spreadsheets, email, and personal reminders. Acxiom CRM keeps the important context together so a team can see who owns a record, what has happened, and what needs attention next.

For example, a sales executive can enter a new lead, schedule a call, record the result, and create an opportunity when the lead is qualified. The executive can then track the opportunity through its sales stages, arrange follow-ups, and keep customer activity in the same system. Managers can review pipeline and reports, while administrators manage accounts and review audit history.

This makes the project useful as a learning app, a starting point for a small team’s internal workflow, or a base for a more tailored CRM. Before using it with real customer information in a business, configure and review deployment, backups, access policies, and privacy requirements for that environment.

## Features

- **Customer and lead management:** Keep contact details, ownership, status, lead source, and conversion history together.
- **Opportunity tracking:** Record deal value, probability, stage, expected close date, and owner.
- **Follow-ups and activities:** Plan calls or other next steps, link them to CRM records, and record completed work.
- **Reports:** Review sales pipeline and activity information.
- **Role-based access:** Admins manage users and audit records; Managers can oversee team records; Sales Executives work with records assigned to them.
- **Audit history:** Review record changes and relevant security events.
- **REST API:** Work with customers, leads, opportunities, follow-ups, and pipeline reports through authenticated API routes.
- **Local persistence:** Store application data in SQLite, with Entity Framework Core migrations applied at startup.

## Technology stack

| Area | Technology |
| --- | --- |
| Language and framework | C# and ASP.NET Core MVC on .NET 10 |
| Web interface | Razor views, HTML, CSS, Bootstrap, and jQuery validation |
| Data access | Entity Framework Core 10 |
| Database | SQLite |
| Authentication | ASP.NET Core Identity with cookie authentication and role-based authorization |
| API | ASP.NET Core controllers with JSON endpoints |

## Get started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows for the `start-crm.bat` launcher
- Google Chrome for the launcher to open Chrome directly; if Chrome is unavailable, it uses the default browser

### 1. Clone the repository

```bash
git clone https://github.com/Satwik99nuts/CRM--Customer-Relationship-Management.git
cd CRM--Customer-Relationship-Management/AcxiomCRM
```

### 2. Start the app

On Windows, double-click `start-crm.bat` or run:

```powershell
.\start-crm.bat
```

The launcher reuses the app if it is already responding, or starts it and waits for it to be ready before opening the browser. The server runs in a separate window; keep it open while using the app. Close that window or press **Ctrl+C** in it to stop the server.

You can also start the app from the project folder with:

```bash
dotnet run
```

The development profile runs at <http://localhost:5218> and opens the default browser.

### 3. Create an administrator account

The application seeds the `Admin`, `Manager`, and `Sales Executive` roles at startup. To create or promote an administrator, stop the normal app instance and run this command from the `AcxiomCRM` project folder:

```bash
dotnet run -- --provision-admin
```

Follow the prompts to assign the Admin role to an existing account or create a new administrator account. Then start the app normally and sign in. Admins can create and manage other user accounts from the Users area.

## Typical workflow

1. An administrator creates accounts and assigns each person a role.
2. A Sales Executive adds a lead and records its source and expected value.
3. The executive schedules follow-ups and records calls, meetings, or other activity.
4. When a prospect is qualified, the team tracks the potential deal as an opportunity and updates its stage and expected close date.
5. After conversion, the team maintains the customer record and its ongoing activity in the CRM.
6. Managers review sales progress; administrators manage access and inspect audit history.

## Data and configuration

By default, the app creates or uses `acxiomcrm.db` in the project folder. The database is local to the machine and excluded from Git. Entity Framework Core applies pending migrations automatically when the app starts.

The connection string is configured in `appsettings.json`. For a different environment, use ASP.NET Core configuration or environment-specific settings rather than committing credentials or private configuration to the repository.

## API

The API uses ASP.NET Core Identity cookie authentication. CRM routes require an authenticated user with an allowed role, and Sales Executives are scoped to their assigned records. See [`API_DOCUMENTATION.md`](API_DOCUMENTATION.md) for routes, authentication, request formats, and response codes.

## Project documentation

- [API documentation](API_DOCUMENTATION.md)
- [Assignment compliance notes](ASSIGNMENT_COMPLIANCE.md)
- [Change log](CHANGELOG.md)
