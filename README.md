# Acxiom CRM

A web-based customer relationship management app built with **ASP.NET Core MVC** and **SQLite**. Acxiom CRM brings customer records and day-to-day sales activity together in one place.

## What you can do

- Manage customers and leads
- Track opportunities and follow-ups
- Record activities and review audit logs
- View reports
- Use the REST API documented in [`API_DOCUMENTATION.md`](API_DOCUMENTATION.md)

## Get started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Google Chrome on Windows to open the app directly from the batch launcher (if Chrome is unavailable, it uses your default browser)

### Run on Windows

1. Clone the repository:

   ```bash
   git clone https://github.com/Satwik99nuts/CRM--Customer-Relationship-Management.git
   ```

2. Open the project folder:

   ```powershell
   cd CRM--Customer-Relationship-Management\AcxiomCRM
   ```

3. Double-click `start-crm.bat`, or run it from a terminal:

   ```powershell
   .\start-crm.bat
   ```

The launcher starts the app if it is not already running, waits until it responds, and opens it in Chrome. If the app is already running, it opens that instance. Keep the server window open while using the app; close it or press **Ctrl+C** there to stop the server.

### Run from the command line

From the `AcxiomCRM` project folder, run:

```bash
dotnet run
```

The development profile opens the app in your default browser at <http://localhost:5218>.

## Data storage

The app stores local data in the SQLite database `acxiomcrm.db` in the project folder. On startup, it applies any pending Entity Framework Core migrations. The database file is local and is not included in Git.

## Project documentation

- [API documentation](API_DOCUMENTATION.md)
- [Assignment compliance notes](ASSIGNMENT_COMPLIANCE.md)
- [Change log](CHANGELOG.md)
