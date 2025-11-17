using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace library_management_system.Models;

public record SearchViewModel : IValidatableObject
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

	public string CacheKey => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(
		string.Join('_', SearchProperties.Where(s => !string.IsNullOrEmpty(s))))));

	public string Description => string.Join(", ", SearchProperties.Where(s => !string.IsNullOrEmpty(s)));

	private string?[] SearchProperties => [Title, Author, ISBN, Director, ReleaseYear?.ToString(), EAN];

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (SearchProperties.All(string.IsNullOrEmpty) && !ReleaseYear.HasValue)
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

	public List<MusicDisc> MusicDiscs { get; set; } = [];
}
