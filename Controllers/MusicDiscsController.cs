using library_management_system.Infrastructure;
using library_management_system.Models;
using library_management_system.Services;
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
	ApplicationVersionService applicationVersionService,
	IOptions<Features> features,
	ILogger<MusicDiscsController> logger) : Controller
{
	public IActionResult Search(int? loanId, string? title, string? author, string? EAN)
	{
		var model = new SearchViewModel
		{
			LoanId = loanId,
			Title = title,
			Author = author,
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
		var onlineSearchMode = OnlineSearchModeParser.Parse(
			features.Value.OnlineMusicDiscSearch.ToString(),
			OnlineSearchMode.Automatic);

		ViewData["PreviousAction"] = previous;
		ViewData["OnlineSearchMode"] = onlineSearchMode.ToString();
		var loanContext = model.LoanId.HasValue;
		ViewData["OnlineSearchEnabled"] = loanContext && onlineSearchMode.ShouldTriggerOnlineSearch(result.Count > 0) ? "true" : "false";
		ViewData["ShowManualOnlineSearch"] = loanContext && onlineSearchMode.ShouldShowManualSearchAction() ? "true" : "false";

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

		try
		{
			var client = httpClientFactory.CreateClient();
			client.Timeout = TimeSpan.FromSeconds(60);
			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				$"LibreLibrary/{applicationVersionService.CurrentVersion} (https://github.com/yvzn/library-management-system)");

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
		catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
		{
			logger.LogError(ex, "Online music disc search failed for {Description}", model.Description);
			return PartialView("_MusicDiscSearchResultsOnlinePartial", new SearchResultsViewModel(model)
			{
				OnlineSearchFailed = true
			});
		}
	}

	public async Task<IActionResult> Edit(int id)
	{
		var musicDisc = await dbContext.MusicDiscs.AsNoTracking().FirstOrDefaultAsync(m => m.ID == id, HttpContext.RequestAborted);
		if (musicDisc == null)
		{
			return NotFound();
		}

		ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineMusicDiscSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
		return View(musicDisc);
	}

	[HttpPost]
	public async Task<IActionResult> Update(MusicDisc musicDisc)
	{
		if (!ModelState.IsValid)
		{
			ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineMusicDiscSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
			return View(nameof(Edit), musicDisc);
		}

		var existingMusicDisc = await dbContext.MusicDiscs.FindAsync(musicDisc.ID);
		if (existingMusicDisc == null)
		{
			return NotFound();
		}

		existingMusicDisc.Title = musicDisc.Title;
		existingMusicDisc.Artist = musicDisc.Artist;
		existingMusicDisc.Version = musicDisc.Version;
		existingMusicDisc.EAN = musicDisc.EAN;

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		return RedirectToAction(nameof(SearchResults), new
		{
			title = existingMusicDisc.Title,
			author = existingMusicDisc.Artist,
			EAN = existingMusicDisc.EAN,
			previous = nameof(Update)
		});
	}

	[HttpPost]
	public async Task<IActionResult> SearchMetadata(int id)
	{
		var existingMusicDisc = await dbContext.MusicDiscs.FirstOrDefaultAsync(m => m.ID == id, HttpContext.RequestAborted);
		if (existingMusicDisc == null)
		{
			return NotFound();
		}

		var onlineSearchMode = OnlineSearchModeParser.Parse(features.Value.OnlineMusicDiscSearch, OnlineSearchMode.Automatic);
		if (onlineSearchMode == OnlineSearchMode.Disabled)
		{
			ViewData["OnlineSearchEnabled"] = "false";
			return View(nameof(Edit), existingMusicDisc);
		}

		var queryParts = new List<string>();
		if (!string.IsNullOrWhiteSpace(existingMusicDisc.Artist)) queryParts.Add($"artist:{existingMusicDisc.Artist}");
		if (!string.IsNullOrWhiteSpace(existingMusicDisc.Title)) queryParts.Add($"recording:{existingMusicDisc.Title}");
		if (!string.IsNullOrWhiteSpace(existingMusicDisc.EAN)) queryParts.Add($"barcode:{existingMusicDisc.EAN}");
		if (queryParts.Count > 0)
		{
			queryParts.Add("format:cd");
			var uriBuilder = new UriBuilder("https://musicbrainz.org/ws/2/release/");
			var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
			query["query"] = string.Join(" AND ", queryParts);
			query["fmt"] = "json";
			uriBuilder.Query = query.ToString();
			var client = httpClientFactory.CreateClient();
			client.Timeout = TimeSpan.FromSeconds(60);
			client.DefaultRequestHeaders.UserAgent.ParseAdd($"LibreLibrary/{applicationVersionService.CurrentVersion} (https://github.com/yvzn/library-management-system)");
			var response = await client.GetFromJsonAsync<MusicBrainzApiResponse>(uriBuilder.Uri, HttpContext.RequestAborted);
			var match = response?.Releases?.FirstOrDefault();
			if (match != null)
			{
				existingMusicDisc.Title ??= match.Title;
				existingMusicDisc.Artist ??= string.Join(", ", match.ArtistCredit.Select(a => a.Name).Distinct());
				existingMusicDisc.Version ??= match.Version;
				existingMusicDisc.EAN ??= string.IsNullOrEmpty(match.BarCode) ? match.Asin : match.BarCode;
			}
		}

		ViewData["OnlineSearchEnabled"] = "true";
		return View(nameof(Edit), existingMusicDisc);
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

			var newlyCreatedLoanMusicDisc = await dbContext.LoanMusicDiscs.AddAsync(loanMusicDisc, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

			return RedirectToAction("Details", "Loans", new { id = loanId.Value, previous = "AddMusicDisc", relationshipId = newlyCreatedLoanMusicDisc.Entity.ID });
		}

		return RedirectToAction("Index", "Loans");
	}
}
