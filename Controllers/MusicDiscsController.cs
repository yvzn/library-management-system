using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Reflection;

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
		var cacheKey = $"SearchResultsOnline_MusicDiscs_{model.CacheKey}";
		if (memoryCache.TryGetValue(cacheKey, out SearchResultsViewModel? cachedResults))
		{
			return PartialView("_MusicDiscSearchResultsOnlinePartial", cachedResults);
		}

		// build MusicBrainz query
		var queryParts = new List<string>();
		if (!string.IsNullOrWhiteSpace(model.Author))
		{
			queryParts.Add($"artist:{model.Author}");
		}
		if (!string.IsNullOrWhiteSpace(model.Title))
		{
			// use recording to match track/release title
			queryParts.Add($"recording:{model.Title}");
		}
		if (!string.IsNullOrWhiteSpace(model.EAN))
		{
			queryParts.Add($"barcode:{model.EAN}");
		}

		if (queryParts.Count == 0)
		{
			// nothing to search for
			var emptyResults = new SearchResultsViewModel(model);
			memoryCache.Set(cacheKey, emptyResults, TimeSpan.FromMinutes(5));
			return PartialView("_MusicDiscSearchResultsOnlinePartial", emptyResults);
		}

		queryParts.Add($"format:cd");

		var uriBuilder = new UriBuilder("https://musicbrainz.org/ws/2/release/");
		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
		query["query"] = string.Join(" AND ", queryParts);
		query["fmt"] = "json";
		uriBuilder.Query = query.ToString();

		var client = httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd(
			$"LibreLibrary/{Assembly.GetExecutingAssembly().GetName().Version} (https://github.com/yvzn/library-management-system)");

		var response = await client.GetFromJsonAsync<MusicBrainzApiResponse>(uriBuilder.Uri, HttpContext.RequestAborted);

		var results = response?.Releases?
			.Select(r => new MusicDisc
			{
				Title = r.Title,
				Artist = string.Join(", ", r.ArtistCredit.Select(a => a.Name).Distinct()),
				Version = r.Version,
				EAN = string.IsNullOrEmpty(r.BarCode) ? r.Asin : r.BarCode
			})
			.Take(20) ?? [];

		var searchResults = new SearchResultsViewModel(model)
		{
			MusicDiscs = [..results]
		};

		memoryCache.Set(cacheKey, searchResults, TimeSpan.FromMinutes(5));

		return PartialView("_MusicDiscSearchResultsOnlinePartial", searchResults);
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
