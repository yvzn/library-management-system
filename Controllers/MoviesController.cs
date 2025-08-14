using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class MoviesController(BookLoansContext dbContext) : Controller
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
		model.Director = model.Director?.Trim();
		model.EAN = model.EAN?.Trim().Replace(" ", "");

		var movies = dbContext.Movies.AsQueryable();
		if (!string.IsNullOrEmpty(model.Title))
		{
			movies = movies.Where(m => m.Title != null && m.Title.ToLower().Contains(model.Title.ToLower()));
		}
		if (!string.IsNullOrEmpty(model.Director))
		{
			movies = movies.Where(m => m.Director != null && m.Director.ToLower().Contains(model.Director.ToLower()));
		}
		if (model.ReleaseYear.HasValue)
		{
			movies = movies.Where(m => m.ReleaseYear == model.ReleaseYear.Value);
		}
		if (!string.IsNullOrEmpty(model.EAN))
		{
			movies = movies.Where(m => m.EAN != null && m.EAN.Equals(model.EAN));
		}

		var result = await movies.AsNoTracking().ToListAsync(HttpContext.RequestAborted);

		// For demo purposes, enable online search (in real app this would be configured)
		ViewData["OnlineSearchEnabled"] = "true";

		return View(
			new SearchResultsViewModel(model)
			{
				Movies = result
			});
	}

	public async Task<IActionResult> SearchResultsOnline(SearchViewModel model)
	{
		// Dummy implementation - in a real application, this would call an external movie API
		var dummyMovies = new List<Movie>();

		// Create some dummy results based on search criteria
		if (!string.IsNullOrEmpty(model.Title) || !string.IsNullOrEmpty(model.Director) || model.ReleaseYear.HasValue || !string.IsNullOrEmpty(model.EAN))
		{
			dummyMovies.AddRange(new[]
			{
				new Movie
				{
					Title = $"Online Movie: {model.Title ?? "Sample Title"}",
					Director = model.Director ?? "Sample Director",
					ReleaseYear = model.ReleaseYear ?? 2023,
					EAN = model.EAN ?? "1234567890123"
				},
				new Movie
				{
					Title = $"Another Movie: {model.Title ?? "Another Sample"}",
					Director = model.Director ?? "Another Director",
					ReleaseYear = (model.ReleaseYear ?? 2023) - 1,
					EAN = model.EAN ?? "9876543210987"
				}
			});
		}

		// Simulate async delay for API call
		await Task.Delay(500, HttpContext.RequestAborted);

		return PartialView(
			"_MovieSearchResultsOnlinePartial",
			new SearchResultsViewModel(model)
			{
				Movies = dummyMovies
			});
	}

	public IActionResult New(Movie movie, int? loanId)
	{
		ViewData["LoanId"] = loanId;

		return View(movie);
	}

	public async Task<IActionResult> Create(Movie movie, int? loanId)
	{
		if (!ModelState.IsValid)
		{
			ViewData["LoanId"] = loanId;
			return View(nameof(New), movie);
		}

		var newlyCreatedMovie = await dbContext.Movies.AddAsync(movie, HttpContext.RequestAborted);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		if (loanId.HasValue)
		{
			var loanMovie = new LoanMovie
			{
				LoanID = loanId.Value,
				MovieID = newlyCreatedMovie.Entity.ID
			};

			await dbContext.LoanMovies.AddAsync(loanMovie, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		}

		if (loanId.HasValue)
		{
			return RedirectToAction("Details", "Loans", new { id = loanId.Value });
		}

		return RedirectToAction("Index", "Loans");
	}
}
