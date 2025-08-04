using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace library_management_system.Controllers;

public class LoansController(BookLoansContext dbContext, IOptions<Features> features) : Controller
{
	public async Task<IActionResult> Index()
	{
		var loans = await dbContext.Loans
			.Include(l => l.LoanBooks)
			.OrderByDescending(l => l.LoanDate)
			.AsNoTracking()
			.ToListAsync(HttpContext.RequestAborted);

		return View(loans);
	}

	public async Task<IActionResult> Details(int id)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.ID == id, HttpContext.RequestAborted);

		if (loan == null)
		{
			return NotFound();
		}

		return View(loan);
	}

	public IActionResult New()
	{
		var loan = new Loan
		{
			LoanDate = DateTime.Now,
			DueDate = DateTime.Now.AddDays(features.Value.DefaultLoanDuration),
		};

		return View(loan);
	}

	public async Task<IActionResult> Edit(int loanId)
	{
		var loan = await dbContext.Loans
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan == null)
		{
			return NotFound();
		}

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

		return RedirectToAction("Details", "Loans", new { id = newlyCreatedLoan.Entity.ID });
	}

	public async Task<IActionResult> Update(Loan loan)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(Edit), loan);
		}

		var existingLoan = await dbContext.Loans
			.SingleAsync(l => l.ID == loan.ID, HttpContext.RequestAborted);

		existingLoan.LoanDate = loan.LoanDate;
		existingLoan.DueDate = loan.DueDate;
		existingLoan.ReturnDate = loan.ReturnDate;

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", new { id = existingLoan.ID });
	}

	public async Task<IActionResult> AddBook(
		int loanId,
		int bookId)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan.LoanBooks.Any(lb => lb.BookID == bookId))
		{
			return RedirectToAction("Details", new { id = loanId });
		}

		loan.LoanBooks.Add(new LoanBook
		{
			LoanID = loan.ID,
			BookID = bookId
		});
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", new { id = loanId });
	}

	public async Task<IActionResult> Return(int loanId)
	{
		var loan = await dbContext.Loans
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		loan.ReturnDate = DateTime.Now;
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", new { id = loanId });
	}

	public async Task<IActionResult> Delete(int id)
	{
		var loanBooks = await dbContext.LoanBooks
			.Where(lb => lb.LoanID == id)
			.ToListAsync(HttpContext.RequestAborted);

		dbContext.LoanBooks.RemoveRange(loanBooks);

		var loan = await dbContext.Loans
			.SingleAsync(l => l.ID == id, HttpContext.RequestAborted);

		dbContext.Loans.Remove(loan);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Index");
	}
}
