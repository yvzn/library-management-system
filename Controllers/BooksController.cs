using library_management_system.Infrastructure;
using library_management_system.Models;
using library_management_system.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace library_management_system.Controllers;

public class BooksController(
	BookLoansContext dbContext,
	IMemoryCache memoryCache,
	IOptions<Features> features,
	IBookSearchService bookSearchService,
	ILogger<BooksController> logger) : Controller
{
	public IActionResult Search(int? loanId, string? title, string? author, string? ISBN)
	{
		var model = new SearchViewModel
		{
			LoanId = loanId,
			Title = title,
			Author = author,
			ISBN = ISBN,
		};

		return View(model);
	}

	public async Task<IActionResult> SearchResults(SearchViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(Search), model);
		}

		model.Author = model.Author?.Trim();
		model.Title = model.Title?.Trim();
		model.ISBN = model.ISBN?.Trim().Replace("-", "");

		var books = dbContext.Books.AsQueryable();
		if (!string.IsNullOrEmpty(model.Author))
		{
			books = books.Where(b => b.Author != null && b.Author.ToLower().Contains(model.Author.ToLower()));
		}
		if (!string.IsNullOrEmpty(model.Title))
		{
			books = books.Where(b => b.Title != null && b.Title.ToLower().Contains(model.Title.ToLower()));
		}
		if (!string.IsNullOrEmpty(model.ISBN))
		{
			books = books.Where(
				b => b.ISBN_13 != null && b.ISBN_13.Equals(model.ISBN)
				|| b.ISBN_10 != null && b.ISBN_10.Equals(model.ISBN));
		}

		var result = await books.AsNoTracking().ToListAsync(HttpContext.RequestAborted);
		var onlineSearchMode = OnlineSearchModeParser.Parse(
			features.Value.OnlineBookSearch.ToString(),
			OnlineSearchMode.Automatic);

		ViewData["OnlineSearchMode"] = onlineSearchMode.ToString();
		ViewData["OnlineSearchEnabled"] = onlineSearchMode.ShouldTriggerOnlineSearch(result.Count > 0).ToString().ToLowerInvariant();
		ViewData["ShowManualOnlineSearch"] = onlineSearchMode.ShouldShowManualSearchAction().ToString().ToLowerInvariant();

		return View(
			new SearchResultsViewModel(model)
			{
				Books = result
			});
	}

	public async Task<IActionResult> SearchResultsOnline(SearchViewModel model)
	{
		var cacheKey = $"SearchResultsOnline_Books_{model.CacheKey}";
		if (memoryCache.TryGetValue(cacheKey, out SearchResultsViewModel? cachedResults))
		{
			return PartialView("_BookSearchResultsOnlinePartial", cachedResults);
		}

		var title = model.Title?.Trim();
		var author = model.Author?.Trim();
		var isbn = model.ISBN?.Trim().Replace("-", "");

		try
		{
			var books = await bookSearchService.SearchBooksAsync(title, author, isbn, HttpContext.RequestAborted);

			var searchResults = new SearchResultsViewModel(model)
			{
				Books = books
			};

			memoryCache.Set(cacheKey, searchResults, TimeSpan.FromMinutes(5));

			return PartialView("_BookSearchResultsOnlinePartial", searchResults);
		}
		catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
		{
			logger.LogError(ex, "Online book search failed for {Description}", model.Description);
			return PartialView("_BookSearchResultsOnlinePartial", new SearchResultsViewModel(model)
			{
				OnlineSearchFailed = true
			});
		}
	}

	public async Task<IActionResult> Edit(int id)
	{
		var book = await dbContext.Books.AsNoTracking().FirstOrDefaultAsync(b => b.ID == id, HttpContext.RequestAborted);
		if (book == null)
		{
			return NotFound();
		}

		ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineBookSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
		return View(book);
	}

	[HttpPost]
	public async Task<IActionResult> Update(Book book)
	{
		if (!ModelState.IsValid)
		{
			ViewData["OnlineSearchEnabled"] = (OnlineSearchModeParser.Parse(features.Value.OnlineBookSearch, OnlineSearchMode.Automatic) != OnlineSearchMode.Disabled).ToString().ToLowerInvariant();
			return View(nameof(Edit), book);
		}

		var existingBook = await dbContext.Books.FindAsync(book.ID);
		if (existingBook == null)
		{
			return NotFound();
		}

		existingBook.Title = book.Title;
		existingBook.Author = book.Author;
		existingBook.ISBN_13 = book.ISBN_13;
		existingBook.ISBN_10 = book.ISBN_10;

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		return RedirectToAction(nameof(Search));
	}

	[HttpPost]
	public async Task<IActionResult> SearchMetadata(int id)
	{
		var existingBook = await dbContext.Books.FirstOrDefaultAsync(b => b.ID == id, HttpContext.RequestAborted);
		if (existingBook == null)
		{
			return NotFound();
		}

		var onlineSearchMode = OnlineSearchModeParser.Parse(features.Value.OnlineBookSearch, OnlineSearchMode.Automatic);
		if (onlineSearchMode == OnlineSearchMode.Disabled)
		{
			ViewData["OnlineSearchEnabled"] = "false";
			return View(nameof(Edit), existingBook);
		}

		var searchResults = await bookSearchService.SearchBooksAsync(existingBook.Title, existingBook.Author, existingBook.ISBN_13 ?? existingBook.ISBN_10, HttpContext.RequestAborted);
		var match = searchResults.FirstOrDefault(result =>
			string.Equals(result.Title, existingBook.Title, StringComparison.OrdinalIgnoreCase)
			|| (result.ISBN_13 != null && existingBook.ISBN_13 != null && result.ISBN_13.Equals(existingBook.ISBN_13, StringComparison.OrdinalIgnoreCase))
			|| (result.ISBN_10 != null && existingBook.ISBN_10 != null && result.ISBN_10.Equals(existingBook.ISBN_10, StringComparison.OrdinalIgnoreCase)))
			?? searchResults.FirstOrDefault();

		if (match != null)
		{
			existingBook.Title ??= match.Title;
			existingBook.Author ??= match.Author;
			existingBook.ISBN_13 ??= match.ISBN_13;
			existingBook.ISBN_10 ??= match.ISBN_10;
		}

		ViewData["OnlineSearchEnabled"] = "true";
		return View(nameof(Edit), existingBook);
	}

	public IActionResult New(Book book, int? loanId)
	{
		ViewData["LoanId"] = loanId;

		return View(book);
	}

	public async Task<IActionResult> Create(Book book, int? loanId)
	{
		if (!ModelState.IsValid)
		{
			ViewData["LoanId"] = loanId;
			return View(nameof(New), book);
		}

		var newlyCreatedBook = await dbContext.Books.AddAsync(book, HttpContext.RequestAborted);
		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		if (loanId.HasValue)
		{
			var loanBook = new LoanBook
			{
				LoanID = loanId.Value,
				BookID = newlyCreatedBook.Entity.ID
			};

			var newlyCreatedLoanBook = await dbContext.LoanBooks.AddAsync(loanBook, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

			return RedirectToAction("Details", "Loans", new { id = loanId.Value, previous = "AddBook", relationshipId = newlyCreatedLoanBook.Entity.ID });
		}

		return RedirectToAction("Index");
	}
}
