using System.ComponentModel.DataAnnotations;

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
	public DateTime LoanDate { get; set; }

	[Required]
	[Display(Name = "Due Date")]
	public DateTime DueDate { get; set; }

	[Display(Name = "Return Date")]
	public DateTime? ReturnDate { get; set; }

	public ICollection<LoanBook> LoanBooks { get; set; } = [];
}

public class LoanBook
{
	public int ID { get; set; }

	[Required]
	public int LoanID { get; set; }

	public int BookID { get; set; }

	public Book? Book { get; set; }
}
