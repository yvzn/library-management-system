using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace library_management_system.Models;

public class Book
{
	public int ID { get; set; }

	[Required]
	[Display(Name = "Title")]

	public string? Title { get; set; }

	[Required]
	[Display(Name = "Author")]
	public string? Author { get; set; }

	[Display(Name = "ISBN-13")]
	public string? ISBN_13 { get; set; }

	[Display(Name = "ISBN-10")]
	public string? ISBN_10 { get; set; }
}

public class Loan
{
	public int ID { get; set; }

	[Required]
	[Display(Name = "Loan Date")]
	[DisplayFormat(DataFormatString = "{0:d}")]
	public DateTime LoanDate { get; set; }

	[Required]
	[Display(Name = "Due Date")]
	[DisplayFormat(DataFormatString = "{0:d}")]
	public DateTime DueDate { get; set; }

	[Display(Name = "Return Date")]
	[DisplayFormat(DataFormatString = "{0:d}")]
	public DateTime? ReturnDate { get; set; }

	public ICollection<LoanBook> LoanBooks { get; set; } = [];

	public ICollection<LoanMovie> LoanMovies { get; set; } = [];
}

public class LoanBook
{
	public int ID { get; set; }

	[Required]
	public int LoanID { get; set; }

	public int BookID { get; set; }

	public Book? Book { get; set; }
}

public class Movie
{
	public int ID { get; set; }

	/**
	 * Default title in original language
	 */
	[Required]
	[Display(Name = "Original Title")]
	public string? Title { get; set; }

	[Display(Name = "French Title")]
	public string? TitleFr { get; set; }

	[Display(Name = "Director")]
	public string? Director { get; set; }

	[Display(Name = "Release Year")]
	public int? ReleaseYear { get; set; }

	[Display(Name = "Media")]
	public string? Media { get; set; }

	[Display(Name = "EAN")]
	public string? EAN { get; set; }
}

public class LoanMovie
{
	public int ID { get; set; }

	[Required]
	public int LoanID { get; set; }

	public int MovieID { get; set; }

	public Movie? Movie { get; set; }
}
