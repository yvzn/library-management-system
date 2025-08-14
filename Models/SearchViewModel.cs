

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace library_management_system.Models;

public record SearchViewModel: IValidatableObject
{
	[Display(Name = "Title")]
	public string? Title { get; set; }

	[Display(Name = "Author")]
	public string? Author { get; set; }

	[Display(Name = "ISBN")]
	public string? ISBN { get; set; }

	[Display(Name = "Director")]
	public string? Director { get; set; }

	[Display(Name = "Release Year")]
	public int? ReleaseYear { get; set; }

	[Display(Name = "EAN")]
	public string? EAN { get; set; }

	public int? LoanId { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (new string?[] { Title, Author, ISBN, Director, EAN }.All(string.IsNullOrEmpty)
			&& !ReleaseYear.HasValue
		)
		{
			var stringLocalizer = validationContext.GetService(typeof(IStringLocalizer<SearchViewModel>)) as IStringLocalizer<SearchViewModel>;

			yield return new ValidationResult(
				stringLocalizer!["At least one search criterion must be specified."],
				[nameof(Title), nameof(Author), nameof(ISBN), nameof(Director), nameof(EAN), nameof(LoanId)]
			);
		}
	}
}

public record SearchResultsViewModel : SearchViewModel
{
	public SearchResultsViewModel(SearchViewModel model)
	{
		Title = model.Title;
		Author = model.Author;
		ISBN = model.ISBN;
		LoanId = model.LoanId;
		Director = model.Director;
		ReleaseYear = model.ReleaseYear;
		EAN = model.EAN;
	}

	public List<Book> Books { get; set; } = [];

	public List<Movie> Movies { get; set; } = [];
}
