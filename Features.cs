namespace library_management_system;

public enum OnlineSearchMode
{
	Disabled,
	Automatic,
	Fallback,
	Manual
}

public static class OnlineSearchModeParser
{
	public static OnlineSearchMode Parse(string? configuredValue, OnlineSearchMode fallback = OnlineSearchMode.Automatic)
	{
		if (string.IsNullOrWhiteSpace(configuredValue))
		{
			return fallback;
		}

		if (bool.TryParse(configuredValue, out var legacyValue))
		{
			return legacyValue ? OnlineSearchMode.Automatic : OnlineSearchMode.Disabled;
		}

		if (Enum.TryParse<OnlineSearchMode>(configuredValue, ignoreCase: true, out var mode))
		{
			return mode;
		}

		return fallback;
	}

	public static bool ShouldTriggerOnlineSearch(this OnlineSearchMode mode, bool hasLocalResults)
		=> mode switch
		{
			OnlineSearchMode.Automatic => true,
			OnlineSearchMode.Fallback => !hasLocalResults,
			OnlineSearchMode.Manual => false,
			_ => false
		};

	public static bool ShouldShowManualSearchAction(this OnlineSearchMode mode)
		=> mode == OnlineSearchMode.Manual;
}

public class Features
{
	public int DefaultLoanDuration { get; set; } = 28;
	public string DefaultBootstrapTheme { get; set; } = "dark";
	public bool LaunchBrowserOnStartup { get; set; } = false;
	public bool ShutdownIfNoActiveUsers { get; set; } = true;
	public string OnlineBookSearch { get; set; } = nameof(OnlineSearchMode.Automatic);
	public string OnlineMovieSearch { get; set; } = nameof(OnlineSearchMode.Automatic);
	public string OnlineMusicDiscSearch { get; set; } = nameof(OnlineSearchMode.Automatic);
}
