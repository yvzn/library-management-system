# Library Management System

This is a simple library management system built with ASP.NET Core MVC and Entity Framework. It allows users to manage book loans, including adding new loans, viewing loan details, and marking loans as returned.

## Installation instructions

Download the project from the releases page in the [GitHub repository](https://github.com/yvzn/library-management-system/releases).

Extract the downloaded archive to a directory of your choice.

Review the `appsettings.json` file to ensure the configuration is correct.

Run `library-management-system.exe`.

## Running the project locally

Requirements:
- .NET SDK 9.0 or later

To run the project, navigate to the project directory and use the following command:

```pwsh
dotnet watch run --launch-profile http --project library-management-system.csproj
```

### Database

The project uses a SQLite database for storing loan information. By default, the database file is located at `%LocalAppData%` on Windows, `XDG_DATA_HOME` on Linux, and `~/Library/Application Support` on macOS.

The project will automatically create the database if it does not exist.

Change the database location by modifying the connection string in the `appsettings.json` file:

```json
{
  "ConnectionStrings": {
    "BookLoansDb": "Data Source=/path/to/your/database.db"
  }
}
```

### Configuration

Optional: Book search is provided by the [Google Books API](https://developers.google.com/books/docs/overview). An API key is required to use the search functionality. You can obtain an API key by following the instructions in the [Google Books API documentation](https://developers.google.com/books/docs/v1/using#APIKey).

Store the API key in user-secrets or environment variables:

```pwsh
# Set the API key in user-secrets
dotnet user-secrets set "ConnectionStrings:BookSearchApiKey" "YOUR_API_KEY"

# Or set the API key in environment variables
$env:ConnectionStrings__BookSearchApiKey = "YOUR_API_KEY"
```

## Publishing the project

To publish the project, use the following command:

```pwsh
dotnet publish library-management-system.csproj --configuration Release -r win-x64 --output ./publish
```

## Tasks

- Branding
	- Favicon & Logo
	- Name

- Internationalization

- Responsive
	- Spacing / UX

- Cards instead of tables

## EntityFramework migrations

<https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/intro?view=aspnetcore-9.0>

Update database

```pwsh
dotnet ef database update  --project library-management-system.csproj
```

Recreate database

```pwsh
dotnet ef database drop  --project library-management-system.csproj
```

Create migration

```pwsh
dotnet ef migrations add <<name>>  --project library-management-system.csproj
```

List all migrations

```pwsh
dotnet ef migrations list --project library-management-system.csproj
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
