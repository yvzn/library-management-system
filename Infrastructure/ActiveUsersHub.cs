using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace library_management_system.Infrastructure;

public class ActiveUsersHub(
	IHostApplicationLifetime appLifetime,
	IOptions<Features> features,
	ILogger<ActiveUsersHub> logger) : Hub
{
	private static readonly ConcurrentDictionary<string, byte> connectedUsers = new();

	public override async Task OnConnectedAsync()
	{
		logger.LogDebug("A user connected: {ConnectionId}", Context.ConnectionId);

		connectedUsers[Context.ConnectionId] = 1;
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		logger.LogDebug("A user disconnected: {ConnectionId}", Context.ConnectionId);

		if (connectedUsers.ContainsKey(Context.ConnectionId))
			connectedUsers.Remove(Context.ConnectionId, out _);

		if (connectedUsers.IsEmpty && features.Value.ShutdownIfNoActiveUsers)
		{
			_ = ScheduleApplicationShutdown();
		}

		await base.OnDisconnectedAsync(exception);
	}

	private async Task ScheduleApplicationShutdown()
	{
		await Task.Delay(TimeSpan.FromSeconds(30));

		// Check again before shutting down
		if (connectedUsers.IsEmpty && features.Value.ShutdownIfNoActiveUsers)
		{
			logger.LogWarning("No more connected users. Application will shutdown...");
			appLifetime.StopApplication();
		}
	}
}
