using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace library_management_system.Services;

/**
 * Starts the default browser when the server starts.
 *
 * Similar to `launchBrowser` in `launchSettings.json`, but for all environments.
 */
internal class LaunchBrowserOnStartup(
	IOptions<Features> features,
	IServer server,
	IHostApplicationLifetime lifetime) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!features.Value.LaunchBrowserOnStartup)
			return;

		lifetime.ApplicationStarted.Register(() =>
		{
			var url = GetServerUrl();
			if (url is null)
				return;

			var browser = GetBrowserForCurrentOs(url);
			if (browser is null)
				return;

			TryStartBrowser(browser);
		});

		await Task.CompletedTask;
	}

	private string? GetServerUrl()
	{
		var addressesFeature = server.Features.Get<IServerAddressesFeature>();
		return addressesFeature?.Addresses.FirstOrDefault();
	}

	public static ProcessStartInfo? GetBrowserForCurrentOs(string url)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new ProcessStartInfo("cmd", $"/c start {url}");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return new ProcessStartInfo("xdg-open", url);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return new ProcessStartInfo("open", url);
		return default;
	}

	public static void TryStartBrowser(ProcessStartInfo browser)
	{
		try
		{
			_ = Process.Start(browser);
		}
		catch
		{
			// Ignore exceptions, as this is just a convenience feature.
		}
	}
}
