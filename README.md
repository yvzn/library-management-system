# Library Management System

This is a simple library management system built with ASP.NET Core MVC and Entity Framework. It allows users to manage book loans, including adding new loans, viewing loan details, and marking loans as returned.

## Running the project

Requirements:
- .NET SDK 9.0 or later

To run the project, navigate to the project directory and use the following command:

```pwsh
dotnet watch run --launch-profile https --project library-management-system.csproj
```

Book search is provided by the [Google Books API](https://developers.google.com/books/docs/overview). An API key is required to use the search functionality. You can obtain an API key by following the instructions in the [Google Books API documentation](https://developers.google.com/books/docs/v1/using#APIKey).

Store the API key in user-secrets or environment variables:

```pwsh
# Set the API key in user-secrets
dotnet user-secrets set "ConnectionStrings:BookSearchApiKey" "YOUR_API_KEY"

# Or set the API key in environment variables
$env:ConnectionStrings__BookSearchApiKey = "YOUR_API_KEY"
```

## Tasks

- Start browser on launch
	- Log levels in Release mode
	- No console output
	- Detect browser window closure

- Packaging
	- Database connection string
	- Configure default days before return date

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
