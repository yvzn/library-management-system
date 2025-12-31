using library_management_system.Models;

namespace library_management_system.Services;

public interface IBookSearchService
{
	public Task<List<Book>> SearchBooksAsync(string? title, string? author, string? isbn, CancellationToken cancellationToken = default);
}
