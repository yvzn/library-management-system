using library_management_system.Models;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Infrastructure;

public class BookLoansDbInitializer(BookLoansContext context)
{
	internal async Task Init(CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(BookLoansContext.DbDirectory);

		await context.Database.MigrateAsync(cancellationToken);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		if (context.Loans.Any())
		{
			return;
		}

		// Seed data
		var books = new List<Book>
		{
			new () { ID = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "9780743273565" },
			new () { ID = 2, Title = "1984", Author = "George Orwell", ISBN = "9780451524935" },
			new () { ID = 3, Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "9780061120084" },
			new () { ID = 4, Title = "Pride and Prejudice", Author = "Jane Austen", ISBN = "9780141040349" },
		};
		await context.Books.AddRangeAsync(books, cancellationToken);

		var activeLoan = new Loan
		{
			ID = 1,
			LoanDate = DateTime.UtcNow.AddDays(-1),
			ReturnDate = null,
		};
		var returnedLoan = new Loan
		{
			ID = 2,
			LoanDate = DateTime.UtcNow.AddDays(-10),
			ReturnDate = DateTime.UtcNow.AddDays(-5),
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
}
