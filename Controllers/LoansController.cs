using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace library_management_system.Controllers;

public class LoansController(BookLoansContext dbContext, IOptions<Features> features) : Controller
{
	public async Task<IActionResult> Index(string? previous)
	{
		var loans = await dbContext.Loans
			.Include(l => l.LoanBooks)
			.Include(l => l.LoanMovies)
			.Include(l => l.LoanMusicDiscs)
			.OrderByDescending(l => l.LoanDate)
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync(HttpContext.RequestAborted);

		ViewData["PreviousAction"] = previous;

		return View(loans);
	}

	public async Task<IActionResult> Details(int id, string? previous)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.Include(l => l.LoanMovies)
				.ThenInclude(lm => lm.Movie)
			.Include(l => l.LoanMusicDiscs)
				.ThenInclude(lmd => lmd.MusicDisc)
			.AsSingleQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.ID == id, HttpContext.RequestAborted);

		if (loan == null)
		{
			return NotFound();
		}

		ViewData["IsNewLoan"] = (loan.LoanDate.Date >= DateTime.Now.Date).ToString().ToLowerInvariant();
		ViewData["PreviousAction"] = previous;

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

		return RedirectToAction(nameof(Details), new { id = newlyCreatedLoan.Entity.ID });
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

		return RedirectToAction(nameof(Details), new { id = existingLoan.ID, previous = nameof(Update) });
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
			return RedirectToAction(nameof(Details), new { id = loanId });
		}

		loan.LoanBooks.Add(new LoanBook
		{
			LoanID = loan.ID,
			BookID = bookId
		});
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction(nameof(Details), new { id = loanId });
	}

	public async Task<IActionResult> AddMovie(
		int loanId,
		int movieId)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanMovies)
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan.LoanMovies.Any(lm => lm.MovieID == movieId))
		{
			return RedirectToAction(nameof(Details), new { id = loanId });
		}

		loan.LoanMovies.Add(new LoanMovie
		{
			LoanID = loan.ID,
			MovieID = movieId
		});
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction(nameof(Details), new { id = loanId });
	}

	public async Task<IActionResult> AddMusicDisc(
		int loanId,
		int musicDiscId)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanMusicDiscs)
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan.LoanMusicDiscs.Any(lmd => lmd.MusicDiscID == musicDiscId))
		{
			return RedirectToAction(nameof(Details), new { id = loanId });
		}

		loan.LoanMusicDiscs.Add(new LoanMusicDisc
		{
			LoanID = loan.ID,
			MusicDiscID = musicDiscId
		});
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction(nameof(Details), new { id = loanId });
	}

	public async Task<IActionResult> Return(int loanId)
	{
		var loan = await dbContext.Loans
			.SingleAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		loan.ReturnDate = DateTime.Now;
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction(nameof(Details), new { id = loanId, previous = nameof(Return) });
	}

	public async Task<IActionResult> ConfirmDelete(int id)
	{
		var loan = await dbContext.Loans
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.ID == id, HttpContext.RequestAborted);

		if (loan == null)
		{
			return NotFound();
		}

		return View(loan);
	}

	[HttpPost]
	public async Task<IActionResult> Delete(int id)
	{
		var loanBooks = await dbContext.LoanBooks
			.Where(lb => lb.LoanID == id)
			.ToListAsync(HttpContext.RequestAborted);

		dbContext.LoanBooks.RemoveRange(loanBooks);

		var loanMovies = await dbContext.LoanMovies
			.Where(lm => lm.LoanID == id)
			.ToListAsync(HttpContext.RequestAborted);

		dbContext.LoanMovies.RemoveRange(loanMovies);

		var loanMusicDiscs = await dbContext.LoanMusicDiscs
			.Where(lmd => lmd.LoanID == id)
			.ToListAsync(HttpContext.RequestAborted);

		dbContext.LoanMusicDiscs.RemoveRange(loanMusicDiscs);

		var loan = await dbContext.Loans
			.SingleAsync(l => l.ID == id, HttpContext.RequestAborted);

		dbContext.Loans.Remove(loan);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction(nameof(Index), new { previous = nameof(Delete) });
	}

	public async Task<IActionResult> ChooseLoanItems(int loanId)
	{
		var loan = await dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.Include(l => l.LoanMovies)
				.ThenInclude(lm => lm.Movie)
			.Include(l => l.LoanMusicDiscs)
				.ThenInclude(lmd => lmd.MusicDisc)
			.AsSingleQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.ID == loanId, HttpContext.RequestAborted);

		if (loan == null)
		{
			return NotFound();
		}

		return View(nameof(ChooseLoanItems), loan);
	}

	[HttpPost]
	public async Task<IActionResult> DeleteItems(DeleteItemsViewModel model)
	{
		if (model.LoanBookIds?.Length > 0)
		{
			var loanBooksToDelete = await dbContext.LoanBooks
				.Where(lb => model.LoanBookIds.Contains(lb.ID))
				.ToListAsync(HttpContext.RequestAborted);

			dbContext.LoanBooks.RemoveRange(loanBooksToDelete);
		}

		if (model.LoanMovieIds?.Length > 0)
		{
			var loanMoviesToDelete = await dbContext.LoanMovies
				.Where(lm => model.LoanMovieIds.Contains(lm.ID))
				.ToListAsync(HttpContext.RequestAborted);

			dbContext.LoanMovies.RemoveRange(loanMoviesToDelete);
		}

		if (model.LoanMusicDiscIds?.Length > 0)
		{
			var loanMusicDiscsToDelete = await dbContext.LoanMusicDiscs
				.Where(lmd => model.LoanMusicDiscIds.Contains(lmd.ID))
				.ToListAsync(HttpContext.RequestAborted);

			dbContext.LoanMusicDiscs.RemoveRange(loanMusicDiscsToDelete);
		}

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		// Redirect back to the loan details page
		return RedirectToAction(nameof(Details), new { id = model.LoanId, previous = nameof(DeleteItems) });
	}
}
