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
			return RedirectToAction("Details", "Loans", new { id = loanBook.LoanID });
		}

		var newlyCreatedBook = await dbContext.Books.AddAsync(loanBook.Book!, HttpContext.RequestAborted);

		loanBook.BookID = newlyCreatedBook.Entity.ID;
		await dbContext.LoanBooks.AddAsync(loanBook, HttpContext.RequestAborted);

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = loanBook.LoanID });
	}
}
