using library_management_system.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace library_management_system.Infrastructure;

public class BookLoansDbInitializer(BookLoansContext context, IConfiguration configuration, ILogger<BookLoansDbInitializer> logger)
{
	internal async Task Init(string? connectionString, CancellationToken cancellationToken = default)
	{
		var databasePath = GetDatabasePath(connectionString);
		Directory.CreateDirectory(GetDatabaseDirectory(connectionString));

		if (await ShouldCreateBackupAsync())
		{
			BackupDatabase(databasePath);
		}

		await context.Database.MigrateAsync(cancellationToken);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		if (await context.Loans.AnyAsync(cancellationToken))
		{
			return;
		}

		// Seed data
		var books = new List<Book>
		{
			new () { ID = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN_13 = "9780743273565" },
			new () { ID = 2, Title = "1984", Author = "George Orwell", ISBN_13 = "9780451524935" },
			new () { ID = 3, Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN_13 = "9780061120084" },
			new () { ID = 4, Title = "Pride and Prejudice", Author = "Jane Austen", ISBN_13 = "9780141040349" },
		};
		await context.Books.AddRangeAsync(books, cancellationToken);

		var activeLoan = new Loan
		{
			ID = 1,
			LoanDate = DateTime.UtcNow.AddDays(-1),
			DueDate = DateTime.UtcNow.AddDays(14),
			ReturnDate = null,
			CreationDate = DateTime.UtcNow.AddDays(-1),
		};
		var returnedLoan = new Loan
		{
			ID = 2,
			LoanDate = DateTime.UtcNow.AddDays(-10),
			DueDate = DateTime.UtcNow.AddDays(4),
			ReturnDate = DateTime.UtcNow.AddDays(-5),
			CreationDate = DateTime.UtcNow.AddDays(-10),
		};
		await context.Loans.AddRangeAsync([activeLoan, returnedLoan], cancellationToken);

		var loanBooks = new List<LoanBook>
		{
			new () { ID = 1, LoanID = 1, BookID = 1 },
			new () { ID = 2, LoanID = 1, BookID = 2 },
			new () { ID = 3, LoanID = 2, BookID = 3 },
			new () { ID = 4, LoanID = 2, BookID = 4 },
		};
		await context.LoanBooks.AddRangeAsync(loanBooks, cancellationToken);

		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task<bool> ShouldCreateBackupAsync()
	{
		var frequency = configuration["Backup:Frequency"];
		var normalizedFrequency = string.IsNullOrWhiteSpace(frequency)
			? "update"
			: frequency.Trim();

		if (string.Equals(normalizedFrequency, "disabled", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (string.Equals(normalizedFrequency, "startup", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!string.Equals(normalizedFrequency, "update", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		return pendingMigrations.Any();
	}

	private void BackupDatabase(string databasePath)
	{
		if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
		{
			return;
		}

		try
		{
			var backupFolder = GetBackupFolder();
			Directory.CreateDirectory(backupFolder);
			var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
			var backupPath = Path.Combine(
				backupFolder,
				$"{Path.GetFileNameWithoutExtension(databasePath)}-{timestamp}{Path.GetExtension(databasePath)}");
			File.Copy(databasePath, backupPath, overwrite: false);

			logger.LogInformation("Database backup created at {BackupPath}", backupPath);
		}
		catch (IOException ex)
		{
			logger.LogWarning(ex, "Database backup skipped: file locked or unavailable");
		}
		catch (UnauthorizedAccessException ex)
		{
			logger.LogWarning(ex, "Database backup skipped: backup folder not writable");
		}
	}

	private string GetBackupFolder()
	{
		var configuredPath = configuration["Backup:Folder"];
		if (string.IsNullOrWhiteSpace(configuredPath))
		{
			return Path.Combine(BookLoansContext.DbDirectory, "Backups");
		}

		configuredPath = Environment.ExpandEnvironmentVariables(configuredPath);
		if (Path.IsPathRooted(configuredPath))
		{
			return Path.GetFullPath(configuredPath);
		}

		return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
	}

	private static string GetDatabaseDirectory(string? connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			return BookLoansContext.DbDirectory;
		}

		var builder = new SqliteConnectionStringBuilder(connectionString);
		return Path.GetDirectoryName(builder.DataSource)
			?? throw new ArgumentException("Invalid connection string, no directory specified", nameof(connectionString));
	}

	private static string GetDatabasePath(string? connectionString)
		=> string.IsNullOrEmpty(connectionString)
			? BookLoansContext.DbPath
			: new SqliteConnectionStringBuilder(connectionString).DataSource;
}
