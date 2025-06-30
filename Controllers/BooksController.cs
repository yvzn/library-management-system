using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class BooksController(
	BookLoansContext dbContext,
	IHttpClientFactory httpClientFactory,
	IConfiguration configuration) : Controller
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
		var existingBooks = await SearchBooksLocally(model);
		var newBooks = await SearchBooksOnline(model);
		return View(
			new SearchResultsViewModel(model)
			{
				ExistingBooks = existingBooks,
				NewBooks = [.. newBooks],
			});
	}

	private async Task<List<Book>> SearchBooksLocally(SearchViewModel model)
	{
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
		return await books.AsNoTracking().ToListAsync(HttpContext.RequestAborted);
	}

	private async Task<IEnumerable<Book>> SearchBooksOnline(SearchViewModel model)
	{
		var apiKey = configuration.GetConnectionString("BookSearchApiKey");
		var uriBuilder = new UriBuilder("https://www.googleapis.com/books/v1/volumes");

		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
		query[nameof(apiKey)] = apiKey;

		var q = new List<string>();
		if (!string.IsNullOrEmpty(model.Author))
		{
			q.Add($"inauthor:{model.Author}");
		}
		if (!string.IsNullOrEmpty(model.Title))
		{
			q.Add($"intitle:{model.Title}");
		}
		if (!string.IsNullOrEmpty(model.ISBN))
		{
			q.Add($"isbn:{model.ISBN}");
		}
		if (q.Count > 0)
		{
			query[nameof(q)] = string.Join("+", q);
		}

		uriBuilder.Query = query.ToString();
		var client = httpClientFactory.CreateClient();

		var response = await client.GetFromJsonAsync<GoogleBooksApiResponse>(uriBuilder.Uri, HttpContext.RequestAborted);

		return response?.Items
			.Select(item => item.VolumeInfo)
			.Select(volumeInfo => new Book
			{
				Title = volumeInfo.Title,
				Author = string.Join(", ", volumeInfo.Authors),
				ISBN_13 = volumeInfo.IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier,
				ISBN_10 = volumeInfo.IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier,
			}) ?? [];
	}
}
