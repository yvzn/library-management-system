using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace library_management_system.Controllers;

public class MoviesController(
	BookLoansContext dbContext,
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

		ViewData["OnlineSearchEnabled"] = features.Value.OnlineMovieSearch.ToString().ToLowerInvariant();

		return View(
			new SearchResultsViewModel(model)
			{
				Movies = result
			});
	}

	public async Task<IActionResult> SearchResultsOnline(SearchViewModel model)
	{
		var uriBuilder = new UriBuilder("http://www.dvdfr.com/api/search.php");

		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
		if (!string.IsNullOrWhiteSpace(model.Title))
		{
			query["title"] = model.Title;
		}
		if (!string.IsNullOrWhiteSpace(model.EAN))
		{
			query["gencode"] = model.EAN;
		}

		var queryString = query.ToString();
		if (string.IsNullOrEmpty(queryString)) {
			return PartialView(
				"_MovieSearchResultsOnlinePartial",
				new SearchResultsViewModel(model));
		}

		uriBuilder.Query = queryString;
		var client = httpClientFactory.CreateClient();

		using var response = await client.GetAsync(uriBuilder.Uri, HttpContext.RequestAborted);
		response.EnsureSuccessStatusCode();

		var xmlContent = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
		var movies = ParseMoviesFromXml(xmlContent);

		return PartialView(
				"_MovieSearchResultsOnlinePartial",
				new SearchResultsViewModel(model)
				{
					Movies = [..movies]
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

	private static IEnumerable<Movie> ParseMoviesFromXml(string xmlContent)
	{
		var document = XDocument.Parse(xmlContent);
		var dvdElements = document.Root?.Elements("dvd");

		if (dvdElements == null)
		{
			yield break;
		}

		foreach (var dvdElement in dvdElements)
		{
			var movie = new Movie();

			var titresElement = dvdElement.Element("titres");
			if (titresElement != null)
			{
				var firstTitle = titresElement.Elements().FirstOrDefault();
				movie.Title = firstTitle?.Value?.Trim();
			}

			var anneeElement = dvdElement.Element("annee");
			if (anneeElement != null && int.TryParse(anneeElement.Value, out var year))
			{
				movie.ReleaseYear = year;
			}

			var starsElement = dvdElement.Element("stars");
			if (starsElement != null)
			{
				var directors = starsElement.Elements("star")
					.Where(s => s.Attribute("type")?.Value == "Réalisateur")
					.Select(s => s.Value?.Trim())
					.Where(name => !string.IsNullOrEmpty(name));

				movie.Director = string.Join(", ", directors);
			}

			// EAN is not provided in the XML structure, so we leave it null
			movie.EAN = null;

			if (!string.IsNullOrEmpty(movie.Title))
			{
				yield return movie;
			}
		}
	}
}
