using library_management_system.Models;

namespace library_management_system.Services;

public class GoogleBooksService(
	IHttpClientFactory httpClientFactory,
	ApplicationVersionService applicationVersionService,
	IConfiguration configuration): IBookSearchService
{
	private HttpClient HttpClient
	{
		get
		{
			var client = httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				$"LibreLibrary/{applicationVersionService.CurrentVersion} (https://github.com/yvzn/library-management-system)");
			return client;
		}
	}

	public async Task<List<Book>> SearchBooksAsync(string? title, string? author, string? isbn, CancellationToken cancellationToken = default)
	{
		var apiKey = configuration.GetConnectionString("BookSearchApiKey");
		var uriBuilder = new UriBuilder("https://www.googleapis.com/books/v1/volumes");

		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
		query[nameof(apiKey)] = apiKey;

		var q = new List<string>();
		if (!string.IsNullOrWhiteSpace(author))
		{
			q.Add($"inauthor:{author}");
		}
		if (!string.IsNullOrWhiteSpace(title))
		{
			q.Add($"intitle:{title}");
		}
		if (!string.IsNullOrWhiteSpace(isbn))
		{
			q.Add($"isbn:{isbn}");
		}
		if (q.Count > 0)
		{
			query[nameof(q)] = string.Join("+", q);
		}

		uriBuilder.Query = query.ToString();

		var response = await HttpClient.GetFromJsonAsync<GoogleBooksApiResponse>(uriBuilder.Uri, cancellationToken);

		var result = response?.Items
			.Select(item => item.VolumeInfo)
			.Select(volumeInfo => new Book
			{
				Title = volumeInfo.Title,
				Author = string.Join(", ", volumeInfo.Authors),
				ISBN_13 = volumeInfo.IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier,
				ISBN_10 = volumeInfo.IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier,
			})
			.Take(20) ?? [];

		return [.. result];
	}
}
