using System.ComponentModel.DataAnnotations;

namespace library_management_system.Models;

public class Book
{
	public int ID { get; set; }
	public string? Title { get; set; }
	public string? Author { get; set; }
	public string? ISBN { get; set; }
}

public class Loan
{
	public int ID { get; set; }

	[Display(Name = "Loan Date")]
	public DateTime LoanDate { get; set; }

	[Display(Name = "Return Date")]
	public DateTime? ReturnDate { get; set; }

	public ICollection<LoanBook> LoanBooks { get; set; } = [];
}

public class LoanBook
{
	public int ID { get; set; }
	public int LoanID { get; set; }
	public int BookID { get; set; }
	public Book? Book { get; set; }
}
