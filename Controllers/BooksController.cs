using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class BooksController(BookLoansContext dbContext) : Controller
{
	public IActionResult Search(int? loanId)
	{
		var model = new SearchViewModel
		{
			LoanId = loanId
		};

		return View(model);
	}

	public IActionResult SearchResults(SearchViewModel model)
	{
		var existingBooks = SearchBooksLocally(model);
		var newBooks = SearchBooksOnline(model);
		return View(
			new SearchResultsViewModel(model)
			{
				ExistingBooks = existingBooks,
				NewBooks = [.. newBooks],
			});
	}

	private List<Book> SearchBooksLocally(SearchViewModel model)
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
			books = books.Where(b => b.ISBN != null && b.ISBN.Equals(model.ISBN));
		}
		return [.. books.AsNoTracking()];
	}

	private static IEnumerable<Book> SearchBooksOnline(SearchViewModel model)
	{
		yield break;
	}
}
