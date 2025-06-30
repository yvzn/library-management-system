# Library Management System

TODO

## Running the project

```pwsh
dotnet watch run --launch-profile https --project library-management-system.csproj
```

## Tasks

- ✅ Init new ASP MVC project
    - ✅ test
    - ✅ cleanup (bootstrap, jquery, styles)

- ✅ EF context
    - ✅ entities: books, loans
    - ✅ add db context
    - ✅ add initial migration
    - ✅ add automatic migrations

- Index page
    - list of current loans

- Add a new loan

- Add a book to a loan
    - Book search (Title, Author, ISBN)

- List of all loans

- Loan details page

- Mark a loan as returned

- Branding
	- Favicon & Logo
	- Name

- Responsive

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

