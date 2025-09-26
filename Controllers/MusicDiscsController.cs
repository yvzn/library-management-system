using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace library_management_system.Controllers;

public class MusicDiscsController(
	BookLoansContext dbContext,
	IMemoryCache memoryCache,
	IHttpClientFactory httpClientFactory,
	IOptions<Features> features) : Controller
{
	public IActionResult Search(int? loanId)
	{
		var model = new SearchViewModel
		{
			LoanId = loanId
		};

		return View(model);
	}

	public async Task<IActionResult> SearchResults(SearchViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(Search), model);
		}

		model.Title = model.Title?.Trim();
		model.Author = model.Author?.Trim();
		model.EAN = model.EAN?.Trim().Replace(" ", "");

		var musicDiscs = dbContext.MusicDiscs.AsQueryable();
		if (!string.IsNullOrEmpty(model.Title))
		{
			musicDiscs = musicDiscs.Where(m => 
				m.Title != null && m.Title.ToLower().Contains(model.Title.ToLower()));
		}
		if (!string.IsNullOrEmpty(model.Author))
		{
			musicDiscs = musicDiscs.Where(m => 
				m.Artist != null && m.Artist.ToLower().Contains(model.Author.ToLower()));
		}
		if (!string.IsNullOrEmpty(model.EAN))
		{
			musicDiscs = musicDiscs.Where(m => 
				m.EAN != null && m.EAN.Equals(model.EAN));
		}

		var result = await musicDiscs.AsNoTracking().ToListAsync(HttpContext.RequestAborted);

		ViewData["OnlineSearchEnabled"] = features.Value.OnlineMusicDiscSearch.ToString().ToLowerInvariant();

		return View(
			new SearchResultsViewModel(model)
			{
				MusicDiscs = result
			});
	}

	public async Task<IActionResult> SearchResultsOnline(SearchViewModel model)
	{
		// TODO: Implement MusicBrainz API integration
		// This method will be implemented later
		return PartialView("_MusicDiscSearchResultsOnlinePartial", 
			new SearchResultsViewModel(model));
	}

	public IActionResult New(MusicDisc musicDisc, int? loanId)
	{
		ViewData["LoanId"] = loanId;

		return View(musicDisc);
	}

	public async Task<IActionResult> Create(MusicDisc musicDisc, int? loanId)
	{
		if (!ModelState.IsValid)
		{
			ViewData["LoanId"] = loanId;
			return View(nameof(New), musicDisc);
		}

		var newlyCreatedMusicDisc = await dbContext.MusicDiscs.AddAsync(musicDisc, HttpContext.RequestAborted);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		if (loanId.HasValue)
		{
			var loanMusicDisc = new LoanMusicDisc
			{
				LoanID = loanId.Value,
				MusicDiscID = newlyCreatedMusicDisc.Entity.ID
			};

			await dbContext.LoanMusicDiscs.AddAsync(loanMusicDisc, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		}

		if (loanId.HasValue)
		{
			return RedirectToAction("Details", "Loans", new { id = loanId.Value });
		}

		return RedirectToAction("Index", "Loans");
	}
}
