# Libre Library - Open Source Integrated Library System (ILS)

_Libr(e)ary_ is a simple library management system (LMS) built with ASP.NET Core MVC and Entity Framework. It allows to manage book loans, including adding new loans, viewing loan details, and marking loans as returned.

As a library manager, _Libr(e)ary_ can be used to manage book loans and inventory.

As a library patron, _Libr(e)ary_ can be used to track their current loans and return dates.

## Installation instructions

Download the project from the releases page in the [GitHub repository](https://github.com/yvzn/library-management-system/releases).

Extract the downloaded archive to a directory of your choice.

Review the `appsettings.json` file to ensure the configuration is correct.

Run `library-management-system.exe`.

### Update instructions

Back up your existing database and `appsettings.json` before updating the project.

Repeat the installation instructions above to download the latest version of the project.

## Running the project locally

Requirements:
- .NET SDK 10.0 or later

To run the project, navigate to the project directory and use the following commands:

```pwsh
# restore tooling
dotnet tool restore

# restore front-end dependencies
dotnet libman restore

# run the project in watch mode
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

Optional: By default, book search is provided by [Open Library API](https://openlibrary.org/developers/api). No extra configuration is required to use the Open Library functionality.

However, you can also choose to use the [Google Books API](https://developers.google.com/books/docs/overview). An API key is required to use the Google Books functionality. You can obtain an API key by following the instructions in the [Google Books API documentation](https://developers.google.com/books/docs/v1/using#APIKey).

Store the API key in user-secrets or environment variables:

```pwsh
# If you want to use the Google Books API, set the Api key in user-secrets
dotnet user-secrets set "ConnectionStrings:BookSearchApiKey" "YOUR_API_KEY"

# Or set the API key in environment variables
$env:ConnectionStrings__BookSearchApiKey = "YOUR_API_KEY"
```

You can also set the API key in the `appsettings.json` file, but this is not recommended for production environments:

```json
{
  "ConnectionStrings": {
    "// Google Books API key": "If you want to use the Google Books API, set your API key here",
    "BookSearchApiKey": "YOUR_API_KEY"
  }
}
```

## Publishing the project

To publish the project, use the following command:

```pwsh
dotnet publish library-management-system.csproj --configuration Release -r win-x64 --output ./publish
```

## TODO

Feature requests welcomed ! Please [Open an Issue on Github](https://github.com/yvzn/library-management-system/issues).


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

Remove a migration

```pwsh
# first revert the migration from database by updating to the last valid migration
dotnet ef migrations list --project library-management-system.csproj
dotnet ef database update <<last_valid_migration>> --project library-management-system.csproj

# then remove the last migration from the project
dotnet ef migrations remove --project library-management-system.csproj

# or remove a specific migration
dotnet ef migrations remove --project library-management-system.csproj --migration <<migration_name>>
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Icon credits:

- [Brackets SVG Vector](https://www.svgrepo.com/author/wishforge.games/) by [Wishforge Games](https://wishforge.games/)
- [Book Check SVG Vector](https://www.svgrepo.com/author/Leonid%20Tsvetkov/) by [Leonid Tsvetkov](https://www.figma.com/@leonid?ref=svgrepo.com)

## Third-party APIs

Online book search is powered by the [Open Library API](https://openlibrary.org/developers/api).

Movie search is powered by [DvdFR API](https://www.dvdfr.com/api).

Music disc search is powered by [MusicBrainz API](https://musicbrainz.org/doc/MusicBrainz_API).

