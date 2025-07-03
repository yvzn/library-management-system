

namespace library_management_system.Models;

public record SearchViewModel
{
	public string? Title { get; set; }
	public string? Author { get; set; }
	public string? ISBN { get; set; }
	public int? LoanId { get; set; }
}

public record SearchResultsViewModel : SearchViewModel
{
	public SearchResultsViewModel(SearchViewModel model)
	{
		Title = model.Title;
		Author = model.Author;
		ISBN = model.ISBN;
		LoanId = model.LoanId;
	}

	public List<Book> Books { get; set; } = [];
}
