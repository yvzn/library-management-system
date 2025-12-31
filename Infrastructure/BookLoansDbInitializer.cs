using library_management_system.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Infrastructure;

public class BookLoansDbInitializer(BookLoansContext context)
{
	internal async Task Init(string? connectionString, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(GetDatabaseDirectory(connectionString));

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
}
