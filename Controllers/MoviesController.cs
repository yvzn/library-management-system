using library_management_system.Infrastructure;
using library_management_system.Models;
using library_management_system.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace library_management_system.Controllers;

public class MoviesController(
	BookLoansContext dbContext,
	IMemoryCache memoryCache,
	IConfiguration configuration,
	IOptions<Features> features,
	IMovieSearchService movieSearchService,
	ILogger<MoviesController> logger) : Controller
{
	public IActionResult Search(int? loanId, string? title, string? director, int? releaseYear, string? EAN)
	{
		var model = new SearchViewModel
		{
			LoanId = loanId,
			Title = title,
			Director = director,
			ReleaseYear = releaseYear,
			EAN = EAN
		};

		return View(model);
	}

	public async Task<IActionResult> SearchResults(SearchViewModel model, string? previous = null)
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
			movies = movies.Where(m =>
				m.Title != null && m.Title.ToLower().Contains(model.Title.ToLower())
				||
				m.TitleFr != null && m.TitleFr.ToLower().Contains(model.Title.ToLower()));
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
		var onlineSearchMode = OnlineSearchModeParser.Parse(
			features.Value.OnlineMovieSearch.ToString(),
			OnlineSearchMode.Automatic);
		var onlineSearchEnabled = MovieSearchConfiguration.IsEnabled(configuration, features.Value, result.Count > 0);

		ViewData["PreviousAction"] = previous;
		ViewData["OnlineSearchMode"] = onlineSearchMode.ToString();
		var loanContext = model.LoanId.HasValue;
		ViewData["OnlineSearchEnabled"] = (loanContext && onlineSearchEnabled && onlineSearchMode.ShouldTriggerOnlineSearch(result.Count > 0)) ? "true" : "false";
		ViewData["ShowManualOnlineSearch"] = (loanContext && onlineSearchEnabled && onlineSearchMode.ShouldShowManualSearchAction()) ? "true" : "false";

		return View(
			new SearchResultsViewModel(model)
			{
				Movies = result
			});
	}

	public async Task<IActionResult> SearchResultsOnline(SearchViewModel model)
	{
		if (string.IsNullOrWhiteSpace(MovieSearchConfiguration.GetTmdbApiKey(configuration)))
		{
			return PartialView("_MovieSearchResultsOnlinePartial", new SearchResultsViewModel(model));
		}

		var cacheKey = $"SearchResultsOnline_Movies_{model.CacheKey}";
		if (memoryCache.TryGetValue(cacheKey, out SearchResultsViewModel? cachedResults))
		{
			return PartialView("_MovieSearchResultsOnlinePartial", cachedResults);
		}

		try
		{
			var movies = await movieSearchService.SearchMoviesAsync(model.Title, model.EAN, HttpContext.RequestAborted);
			var searchResults = new SearchResultsViewModel(model)
			{
				Movies = [.. movies]
			};

			memoryCache.Set(cacheKey, searchResults, TimeSpan.FromMinutes(5));

			return PartialView("_MovieSearchResultsOnlinePartial", searchResults);
		}
		catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
		{
			logger.LogError(ex, "Online movie search failed for {Description}", model.Description);
			return PartialView("_MovieSearchResultsOnlinePartial", new SearchResultsViewModel(model)
			{
				OnlineSearchFailed = true
			});
		}
	}

	public async Task<IActionResult> Edit(int id)
	{
		var movie = await dbContext.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.ID == id, HttpContext.RequestAborted);
		if (movie == null)
		{
			return NotFound();
		}

		ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineMovieSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
		return View(movie);
	}

	[HttpPost]
	public async Task<IActionResult> Update(Movie movie)
	{
		if (!ModelState.IsValid)
		{
			ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineMovieSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
			return View(nameof(Edit), movie);
		}

		var existingMovie = await dbContext.Movies.FindAsync(movie.ID);
		if (existingMovie == null)
		{
			return NotFound();
		}

		existingMovie.Title = movie.Title;
		existingMovie.TitleFr = movie.TitleFr;
		existingMovie.Director = movie.Director;
		existingMovie.ReleaseYear = movie.ReleaseYear;
		existingMovie.Media = movie.Media;
		existingMovie.EAN = movie.EAN;

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		return RedirectToAction(nameof(SearchResults), new
		{
			title = existingMovie.Title,
			director = existingMovie.Director,
			releaseYear = existingMovie.ReleaseYear,
			EAN = existingMovie.EAN,
			previous = nameof(Update)
		});
	}

	[HttpPost]
	public async Task<IActionResult> SearchMetadata(int id)
	{
		var existingMovie = await dbContext.Movies.FirstOrDefaultAsync(m => m.ID == id, HttpContext.RequestAborted);
		if (existingMovie == null)
		{
			return NotFound();
		}

		var onlineSearchMode = OnlineSearchModeParser.Parse(features.Value.OnlineMovieSearch, OnlineSearchMode.Automatic);
		if (onlineSearchMode == OnlineSearchMode.Disabled)
		{
			ViewData["OnlineSearchEnabled"] = "false";
			return View(nameof(Edit), existingMovie);
		}

		try
		{
			var candidateMovies = await movieSearchService.SearchMoviesAsync(
				existingMovie.Title,
				existingMovie.EAN,
				HttpContext.RequestAborted);
			var match = candidateMovies.FirstOrDefault(result =>
				string.Equals(result.Title, existingMovie.Title, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(result.TitleFr, existingMovie.Title, StringComparison.OrdinalIgnoreCase))
				?? candidateMovies.FirstOrDefault(result =>
					!string.IsNullOrWhiteSpace(existingMovie.EAN)
					&& !string.IsNullOrWhiteSpace(result.EAN)
					&& string.Equals(result.EAN, existingMovie.EAN, StringComparison.OrdinalIgnoreCase))
				?? candidateMovies.FirstOrDefault();

			if (match != null)
			{
				existingMovie.Title = string.IsNullOrWhiteSpace(existingMovie.Title) ? match.Title : existingMovie.Title;
				existingMovie.TitleFr ??= match.TitleFr;
				existingMovie.ReleaseYear ??= match.ReleaseYear;
				existingMovie.Media ??= match.Media;
				existingMovie.EAN ??= match.EAN;
			}
		}
		catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
		{
			logger.LogError(ex, "Online movie metadata lookup failed for {MovieId}", existingMovie.ID);
		}

		ViewData["OnlineSearchEnabled"] = "true";
		return View(nameof(Edit), existingMovie);
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

			var newlyCreatedLoanMovie = await dbContext.LoanMovies.AddAsync(loanMovie, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

			return RedirectToAction("Details", "Loans", new { id = loanId.Value, previous = "AddMovie", relationshipId = newlyCreatedLoanMovie.Entity.ID });
		}

		return RedirectToAction("Index", "Loans");
	}
}
