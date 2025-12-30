namespace library_management_system.Services;

public class ApplicationVersionService(IHttpClientFactory httpClientFactory, ILogger<ApplicationVersionService> logger)
{
	public string? CurrentVersion =>
		typeof(ApplicationVersionService).Assembly.GetName().Version?.ToString();

	public async Task<(bool, Uri?)> IsNewVersionAvailable()
	{
		try
		{
			var httpClient = httpClientFactory.CreateClient();
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
				$"LibreLibrary/{CurrentVersion} (https://github.com/yvzn/library-management-system)");

			var releasesUrl = "https://github.com/yvzn/library-management-system/releases";
			var response = await httpClient.GetStringAsync(releasesUrl);

			// Parse for version numbers in h2 tags with sr-only class
			// Pattern: <h2 class="sr-only">v1.2</h2> or <h2 class="sr-only">v1.2.3</h2>
			var versionPattern = @"<h2[^>]*class=""[^""]*sr-only[^""]*""[^>]*>(v\d+\.\d+(?:\.\d+)?)</h2>";
			var matches = System.Text.RegularExpressions.Regex.Matches(response, versionPattern);

			if (matches.Count == 0)
				return (false, default);

			// Get the first (latest) version from the releases page
			var latestVersionString = matches[0].Groups[1].Value; // e.g., "v1.2.3"
			var latestVersionNumber = latestVersionString[1..]; // Remove 'v' prefix

			// Get current version
			var currentVersionString = CurrentVersion;
			if (string.IsNullOrEmpty(currentVersionString))
				return (false, default);

			// Compare versions
			var latestVersion = new Version(latestVersionNumber);
			var currentVersion = new Version(currentVersionString);

			if (latestVersion > currentVersion)
			{
				var releaseUri = new Uri($"https://github.com/yvzn/library-management-system/releases/tag/{latestVersionString}");
				return (true, releaseUri);
			}

			return (false, default);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to check for new application version.");
			return (false, default);
		}
	}
}
