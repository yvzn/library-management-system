using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class LoansController(BookLoansContext dbContext) : Controller
{
	public async Task<IActionResult> Index()
	{
		var loans = await dbContext.Loans
			.Include(l => l.LoanBooks)
			.OrderByDescending(l => l.LoanDate)
			.ToListAsync(HttpContext.RequestAborted);

		return View(loans);
	}

	public async Task<IActionResult> Details(
		int id,
		bool addBooks = false)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.FirstOrDefaultAsync(l => l.ID == id, HttpContext.RequestAborted);

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

	public async Task<IActionResult> Create(Loan loan)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(New), loan);
		}

		var newlyCreatedLoan = dbContext.Loans.Add(loan);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = newlyCreatedLoan.Entity.ID, addBooks = true });
	}

	public async Task<IActionResult> AddBook(int loanId, int bookId)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan.LoanBooks.Any(lb => lb.BookID == bookId))
		{
			return RedirectToAction("Details", new { id = loanId, addBooks = true });
		}

		loan.LoanBooks.Add(new LoanBook
		{
			LoanID = loan.ID,
			BookID = bookId
		});
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", new { id = loanId, addBooks = true });
	}

	public async Task<IActionResult> Return(int loanId)
	{
		var loan = await dbContext.Loans
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		loan.ReturnDate = DateTime.Now;
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", new { id = loanId });
	}
}
