using library_management_system.Infrastructure;
using library_management_system.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

namespace library_management_system;

internal class Program
{
	private static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();

		var supportedCultures = new[] { "en", "fr" };



		// -- Add services to the container. ------------------------------------------

		builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

		builder.Services.AddControllersWithViews()
			.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
			.AddDataAnnotationsLocalization();

		builder.Services.AddHttpLogging(options =>
		{
			options.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.RequestMethod | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseBody;
			options.ResponseBodyLogLimit = 128;
			options.CombineLogs = true;
		});
		builder.Services.AddHttpClient();

		builder.Services.Configure<Features>(builder.Configuration.GetSection(nameof(Features)));

		var connectionString = builder.Configuration.GetConnectionString("BookLoansDb");
		if (string.IsNullOrEmpty(connectionString)) connectionString = $"Data Source={BookLoansContext.DbPath}";
		builder.Services.AddDbContext<BookLoansContext>(
			options => options.UseSqlite(connectionString));
		builder.Services.AddScoped<BookLoansDbInitializer>();

		builder.Services.AddHealthChecks()
			.AddDbContextCheck<BookLoansContext>();

		builder.Services.AddHostedService<LaunchBrowserOnStartup>();
		builder.Services.AddHostedService<StartupBanner>();

		var urls = builder.Configuration.GetSection("urls").Get<string>()
			?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			?? [];
		var healthCheckTimeout = builder.Configuration.GetSection("Technical")?.GetValue<int?>("HealthCheckTimeout")
			?? 500;

		var app = builder.Build();



		// -- Check if the application is already running. ----------------------------
		var (isAlreadyRunning, url) = await IsAlreadyRunning(urls, healthCheckTimeout, app);
		if (isAlreadyRunning && url is not null)
		{
			Console.WriteLine("Application is already running.");
			Console.WriteLine();
			Console.WriteLine($"Open your browser and visit: {url}.");

			var browser = LaunchBrowserOnStartup.GetBrowserForCurrentOs(url);
			if (browser is null)
				return;

			LaunchBrowserOnStartup.TryStartBrowser(browser);
			return;
		}



		// -- Configure the HTTP request pipeline. ------------------------------------

		app.UseRequestLocalization(options =>
		{
			options.SetDefaultCulture(supportedCultures[0])
				.AddSupportedCultures(supportedCultures)
				.AddSupportedUICultures(supportedCultures);
			options.ApplyCurrentCultureToResponseHeaders = true;
		});

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
		}

		app.UseRouting();

		app.UseAuthorization();

		app.UseHttpLogging();

		app.MapStaticAssets();

		app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}")
				.WithStaticAssets();

		app.MapHealthChecks("/healthz");



		// -- Run the application. ----------------------------------------------------

		using (var scope = app.Services.CreateScope())
		{
			var serviceProvider = scope.ServiceProvider;
			var dbInitializer = serviceProvider.GetRequiredService<BookLoansDbInitializer>();
			await dbInitializer.Init(CancellationToken.None);
		}

		await app.RunAsync();
	}

	private static async Task<(bool, string?)> IsAlreadyRunning(string[] urls, int timeoutInMilliseconds, WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var serviceProvider = scope.ServiceProvider;

		var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
		using var httpClient = httpClientFactory.CreateClient();
		httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

		foreach (var url in urls)
		{
			try
			{
				var response = await httpClient.GetAsync(url + "/healthz");
				if (response.IsSuccessStatusCode)
				{
					return (true, url);
				}
			}
			catch (Exception ex)
			{
				serviceProvider
					.GetRequiredService<ILogger<Program>>()
					.LogDebug(ex, "IsAlreadyRunning could not reach {Url}", url);
			}
		}
		return (false, default);
	}

	protected Program() { }
}
