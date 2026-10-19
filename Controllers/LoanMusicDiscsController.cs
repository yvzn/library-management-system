using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace library_management_system.Controllers;

public class LoanMusicDiscsController(BookLoansContext dbContext) : Controller
{
	public async Task<IActionResult> Create(LoanMusicDisc loanMusicDisc)
	{
		if (!ModelState.IsValid)
		{
			TempData["ValidationErrorMessage"] = string.Join(" ", ModelState
				.Values
				.SelectMany(value => value.Errors)
				.Select(error => error.ErrorMessage)
				.Distinct());

			return RedirectToAction("SearchResults", "MusicDiscs", new
			{
				loanId = loanMusicDisc.LoanID,
				title = loanMusicDisc.MusicDisc?.Title,
				author = loanMusicDisc.MusicDisc?.Artist,
				EAN = loanMusicDisc.MusicDisc?.EAN,
			});
		}

		var newlyCreatedMusicDisc = await dbContext.MusicDiscs.AddAsync(loanMusicDisc.MusicDisc!, HttpContext.RequestAborted);

		loanMusicDisc.MusicDiscID = newlyCreatedMusicDisc.Entity.ID;
		var newlyCreatedLoanMusicDisc = await dbContext.LoanMusicDiscs.AddAsync(loanMusicDisc, HttpContext.RequestAborted);

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = loanMusicDisc.LoanID, previous = "AddMusicDisc", relationshipId = newlyCreatedLoanMusicDisc.Entity.ID });
	}
}
