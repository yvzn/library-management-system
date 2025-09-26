namespace library_management_system.Models;

public class DeleteItemsViewModel
{
	public int LoanId { get; set; }
	public int[] LoanBookIds { get; set; } = [];
	public int[] LoanMovieIds { get; set; } = [];
	public int[] LoanMusicDiscIds { get; set; } = [];
}
