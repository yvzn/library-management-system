using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace library_management_system.Controllers;

public class LoanBooksController(BookLoansContext dbContext) : Controller
{
	public async Task<IActionResult> Create(LoanBook loanBook)
	{
		if (!ModelState.IsValid)
		{
			TempData["ValidationErrorMessage"] = string.Join(" ", ModelState
				.Values
				.SelectMany(value => value.Errors)
				.Select(error => error.ErrorMessage)
				.Distinct());

			return RedirectToAction("SearchResults", "Books", new
			{
				loanId = loanBook.LoanID,
				title = loanBook.Book?.Title,
				author = loanBook.Book?.Author,
				ISBN = loanBook.Book?.ISBN_13 ?? loanBook.Book?.ISBN_10,
			});
		}

		var newlyCreatedBook = await dbContext.Books.AddAsync(loanBook.Book!, HttpContext.RequestAborted);

		loanBook.BookID = newlyCreatedBook.Entity.ID;
		var newlyCreatedLoanBook = await dbContext.LoanBooks.AddAsync(loanBook, HttpContext.RequestAborted);

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = loanBook.LoanID, previous = "AddBook", relationshipId = newlyCreatedLoanBook.Entity.ID });
	}
}
