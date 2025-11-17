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
	IOpenLibraryService openLibraryService) : Controller
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

		ViewData["OnlineSearchEnabled"] = features.Value.OnlineBookSearch.ToString().ToLowerInvariant();

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

		// Prepare search parameters
		var title = model.Title?.Trim();
		var author = model.Author?.Trim();
		var isbn = model.ISBN?.Trim().Replace("-", "");

		// Use OpenLibrary service to search for books
		var books = await openLibraryService.SearchBooksAsync(title, author, isbn, HttpContext.RequestAborted);

		var searchResults = new SearchResultsViewModel(model)
		{
			Books = books
		};

		memoryCache.Set(cacheKey, searchResults, TimeSpan.FromMinutes(5));

		return PartialView("_BookSearchResultsOnlinePartial", searchResults);
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

			await dbContext.LoanBooks.AddAsync(loanBook, HttpContext.RequestAborted);
			await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
		}

		if (loanId.HasValue)
		{
			return RedirectToAction("Details", "Loans", new { id = loanId.Value });
		}

		return RedirectToAction("Index");
	}
}
