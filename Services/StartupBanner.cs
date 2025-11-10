using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace library_management_system.Services;

public class StartupBanner(
	IServer server,
	IHostApplicationLifetime lifetime,
	ILogger<StartupBanner> logger) : BackgroundService
{
	private const string Banner = @"
  _    _ _         __ __
 | |  (_) |__ _ _ / /_\ \ __ _ _ _ _  _
 | |__| | '_ \ '_| / -_) / _` | '_| || |
 |____|_|_.__/_| | \___| \__,_|_|  \_, |
                  \_\ /_/          |__/

 Libr(e)ary - Library Management System

 {UrlPrompt}

 {UrlInvite}

 Press <Ctrl+C> to shut down.
";

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		lifetime.ApplicationStarted.Register(() =>
		{
			var url = GetServerUrl();

			logger.LogInformation(Banner,
				url is null ? "" : $"Open your internet browser and go to: {url}.",
				url is null ? "" : $"Ouvrez votre navigateur internet et allez sur : {url}."
			);
		});

		await Task.CompletedTask;
	}

	private string? GetServerUrl()
	{
		var addressesFeature = server.Features.Get<IServerAddressesFeature>();
		return addressesFeature?.Addresses.FirstOrDefault();
	}
}
