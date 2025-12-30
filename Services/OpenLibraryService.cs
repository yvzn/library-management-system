using library_management_system.Models;

namespace library_management_system.Services;

public class OpenLibraryService(
	IHttpClientFactory httpClientFactory,
	ApplicationVersionService applicationVersionService,
	ILogger<OpenLibraryService> logger)
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
		if (!string.IsNullOrWhiteSpace(isbn))
		{
			return await SearchByIsbnAsync(isbn, cancellationToken);
		}

		return await SearchByTitleAndAuthorAsync(title, author, cancellationToken);
	}

	private async Task<List<Book>> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken)
	{
		var url = $"https://openlibrary.org/isbn/{isbn}.json";

		try
		{
			var response = await HttpClient.GetFromJsonAsync<OpenLibraryIsbnResponse>(url, cancellationToken);
			if (response == null)
			{
				return [];
			}

			var authorNames = await ResolveAuthorNamesAsync(response.Authors, cancellationToken);
			var authorNamesString = authorNames.Count > 0 ? string.Join(", ", authorNames) : "Unknown";

			var book = new Book
			{
				Title = response.Title,
				Author = authorNamesString,
				ISBN_13 = response.Isbn_13.FirstOrDefault(),
				ISBN_10 = response.Isbn_10.FirstOrDefault()
			};

			// If we have ISBN-13 or ISBN-10 from the response, use those
			// Otherwise, use the search ISBN
			if (string.IsNullOrEmpty(book.ISBN_13) && string.IsNullOrEmpty(book.ISBN_10))
			{
				if (isbn.Length == 13)
				{
					book.ISBN_13 = isbn;
				}
				else if (isbn.Length == 10)
				{
					book.ISBN_10 = isbn;
				}
			}

			return [book];
		}
		catch (HttpRequestException ex) when (ex.Message.Contains("404"))
		{
			logger.LogInformation(ex, "Book not found for ISBN: {Isbn}", isbn);
			return [];
		}
	}

	private async Task<List<string>> ResolveAuthorNamesAsync(List<OpenLibraryAuthorRef> authorRefs, CancellationToken cancellationToken)
	{
		var authorNames = new List<string>();

		foreach (var authorRef in authorRefs)
		{
			if (string.IsNullOrEmpty(authorRef.Key))
				continue;

			try
			{
				var url = $"https://openlibrary.org{authorRef.Key}.json";
				var authorResponse = await HttpClient.GetFromJsonAsync<OpenLibraryAuthor>(url, cancellationToken);

				if (authorResponse?.Name != null)
				{
					authorNames.Add(authorResponse.Name);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to resolve author key: {AuthorKey}", authorRef.Key);
			}
		}

		return authorNames;
	}

	private async Task<List<Book>> SearchByTitleAndAuthorAsync(string? title, string? author, CancellationToken cancellationToken)
	{
		var uriBuilder = new UriBuilder("https://openlibrary.org/search.json");
		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

		if (!string.IsNullOrWhiteSpace(title))
		{
			query["title"] = title;
		}

		if (!string.IsNullOrWhiteSpace(author))
		{
			query["author"] = author;
		}

		query["sort"] = "title";
		query["limit"] = "20";

		uriBuilder.Query = query.ToString();

		var response = await HttpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(uriBuilder.Uri, cancellationToken);

		if (response?.Docs == null)
		{
			return [];
		}

		var books = response.Docs.Select(doc => new Book
		{
			Title = doc.Title,
			Author = doc.Author_name.Count > 0 ? string.Join(", ", doc.Author_name) : "Unknown",
			ISBN_13 = doc.Isbn.FirstOrDefault(isbn => isbn.Length == 13),
			ISBN_10 = doc.Isbn.FirstOrDefault(isbn => isbn.Length == 10)
		}).ToList();

		return books;
	}
}
