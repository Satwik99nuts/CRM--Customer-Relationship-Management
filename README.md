# Acxiom CRM

A customer relationship management system built with ASP.NET Core MVC and Entity Framework Core. This project helps manage customers, leads, opportunities, follow-ups, activities, and audit logs in a simple, role-based CRM workflow.

## Features

- Customer management
- Lead tracking and pipeline management
- Opportunity tracking
- Follow-up scheduling and reminders
- Activity logging
- Audit log viewing
- User authentication and role-based administration
- SQLite database support
- ASP.NET Core MVC architecture

## Tech Stack

- C# / ASP.NET Core 10
- MVC + Razor Pages
- Entity Framework Core
- SQLite
- ASP.NET Core Identity

## Project Structure

- `Controllers/` - MVC controllers for CRM modules
- `Models/` - Data models
- `Views/` - Razor views for the UI
- `Data/` - EF Core database context and data access configuration
- `Services/` - Business/service layer
- `Migrations/` - Database migrations
- `wwwroot/` - Static assets
- `Program.cs` - Application startup configuration

## Getting Started

### Prerequisites

- .NET 10 SDK
- Git

### Clone the repository

```bash
git clone https://github.com/Satwik99nuts/CRM--Customer-Relationship-Management.git
cd CRM--Customer-Relationship-Management
```

### Restore dependencies

```bash
dotnet restore
```

### Run the application

```bash
dotnet run
```

The application will start using the default SQLite database configured in `appsettings.json`.

## Provisioning an Admin User

This app includes an admin provisioning workflow. To create an administrator account, run:

```bash
dotnet run -- --provision-admin
```

Then follow the prompts to enter the administrator email and password.

## Database

The project uses SQLite with EF Core migrations. The database is created automatically when the app starts and migrations are applied.

## Notes

- The repository is already configured as public.
- This project is designed as a CRM starter application and can be extended for real-world workflows, reporting, and enterprise integrations.

## License

This project is provided as-is for learning and development purposes.
