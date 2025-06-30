using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class LoansController(BookLoansContext dbContext) : Controller
{
	public IActionResult Details(
		int id,
		bool addBooks = false)
	{
		var loan = dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.FirstOrDefault(l => l.ID == id);

		if (loan == null)
		{
			return NotFound();
		}

		if (addBooks)
		{
			ViewData["AddBooks"] = "true";
		}

		return View(loan);
	}

	public IActionResult New()
	{
		var loan = new Loan
		{
			LoanDate = DateTime.Now,
			DueDate = DateTime.Now.AddDays(30),
		};

		return View(loan);
	}

	public IActionResult Create(Loan loan)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(New), loan);
		}

		var newlyCreatedLoan = dbContext.Loans.Add(loan);
		dbContext.SaveChanges();

		return RedirectToAction("Details", "Loans", new { id = newlyCreatedLoan.Entity.ID, addBooks = true });
	}

	public IActionResult AddBook(int loanId, int bookId)
	{
		var loan = dbContext.Loans
			.Include(l => l.LoanBooks)
			.Single(l => l.ID == loanId);

		if (loan.LoanBooks.Any(lb => lb.BookID == bookId))
		{
			return RedirectToAction("Details", new { id = loanId, addBooks = true });
		}

		loan.LoanBooks.Add(new LoanBook
		{
			LoanID = loan.ID,
			BookID = bookId
		});
		dbContext.SaveChanges();

		return RedirectToAction("Details", new { id = loanId, addBooks = true });
	}
}
