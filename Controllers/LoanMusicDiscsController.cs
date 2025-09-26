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
			return RedirectToAction("Details", "Loans", new { id = loanMusicDisc.LoanID });
		}

		var newlyCreatedMusicDisc = await dbContext.MusicDiscs.AddAsync(loanMusicDisc.MusicDisc!, HttpContext.RequestAborted);

		loanMusicDisc.MusicDiscID = newlyCreatedMusicDisc.Entity.ID;
		await dbContext.LoanMusicDiscs.AddAsync(loanMusicDisc, HttpContext.RequestAborted);

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = loanMusicDisc.LoanID });
	}
}
