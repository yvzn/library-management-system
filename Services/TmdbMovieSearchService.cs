using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using library_management_system.Models;

namespace library_management_system.Services;

public interface IMovieSearchService
{
	Task<List<Movie>> SearchMoviesAsync(string? title, string? ean, CancellationToken cancellationToken = default);
}

public static class MovieSearchConfiguration
{
	public static string? GetTmdbApiKey(IConfiguration configuration)
		=> configuration.GetConnectionString("MovieSearchApiKey")
			?? configuration["MovieSearchApiKey"];

	public static bool IsEnabled(IConfiguration configuration, Features features, bool hasLocalResults)
	{
		var apiKey = GetTmdbApiKey(configuration);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return false;
		}

		var mode = OnlineSearchModeParser.Parse(features.OnlineMovieSearch.ToString(), OnlineSearchMode.Automatic);
		return mode.ShouldTriggerOnlineSearch(hasLocalResults);
	}
}

public class TmdbMovieSearchService(
	IHttpClientFactory httpClientFactory,
	ApplicationVersionService applicationVersionService,
	IConfiguration configuration,
	ILogger<TmdbMovieSearchService> logger) : IMovieSearchService
{
	public async Task<List<Movie>> SearchMoviesAsync(string? title, string? ean, CancellationToken cancellationToken = default)
	{
		var apiKey = MovieSearchConfiguration.GetTmdbApiKey(configuration);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return [];
		}

		var queryText = !string.IsNullOrWhiteSpace(title) ? title : ean;
		if (string.IsNullOrWhiteSpace(queryText))
		{
			return [];
		}

		var uriBuilder = new UriBuilder("https://api.themoviedb.org/3/search/movie");
		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
		query["query"] = queryText;
		uriBuilder.Query = query.ToString();

		var client = httpClientFactory.CreateClient();
		client.Timeout = TimeSpan.FromSeconds(60);
		client.DefaultRequestHeaders.UserAgent.ParseAdd(
			$"LibreLibrary/{applicationVersionService.CurrentVersion} (https://github.com/yvzn/library-management-system)");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

		using var response = await client.GetAsync(uriBuilder.Uri, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			logger.LogWarning(
				"TMDb movie search failed with status {StatusCode}: {ReasonPhrase}",
				(int)response.StatusCode,
				response.ReasonPhrase);
			throw new HttpRequestException($"TMDb movie search failed with status code {(int)response.StatusCode}: {response.ReasonPhrase}");
		}

		var content = await response.Content.ReadAsStringAsync(cancellationToken);
		var searchResults = JsonSerializer.Deserialize<TmdbMovieSearchResponse>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		});

		var movies = searchResults?.Results?
			.Where(result => !string.IsNullOrWhiteSpace(result.Title) || !string.IsNullOrWhiteSpace(result.OriginalTitle))
			.Select(result => new Movie
			{
				Title = !string.IsNullOrWhiteSpace(result.OriginalTitle) ? result.OriginalTitle : result.Title,
				TitleFr = result.Title,
				ReleaseYear = TryParseYear(result.ReleaseDate),
				Media = "Movie"
			})
			.DistinctBy(movie => movie.Title + movie.TitleFr + movie.ReleaseYear + movie.Media)
			.Take(20)
			.ToList() ?? [];

		if (!string.IsNullOrWhiteSpace(ean))
		{
			movies = [.. movies.Select(movie => { movie.EAN = ean; return movie; })];
		}

		return movies;
	}

	private static int? TryParseYear(string? releaseDate)
	{
		if (string.IsNullOrWhiteSpace(releaseDate))
		{
			return null;
		}

		if (DateOnly.TryParse(releaseDate, out var parsedDate))
		{
			return parsedDate.Year;
		}

		return null;
	}
}

public sealed class TmdbMovieSearchResponse
{
	[JsonPropertyName("results")]
	public List<TmdbMovieResult> Results { get; set; } = [];
}

public sealed class TmdbMovieResult
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("original_title")]
	public string? OriginalTitle { get; set; }

	[JsonPropertyName("release_date")]
	public string? ReleaseDate { get; set; }
}
