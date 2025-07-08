using library_management_system;
using library_management_system.Infrastructure;
using library_management_system.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHostedService<LaunchBrowserOnStartup>();

var app = builder.Build();



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



// -- Run the application. ----------------------------------------------------

using (var scope = app.Services.CreateScope())
{
	var serviceProvider = scope.ServiceProvider;
	var dbInitializer = serviceProvider.GetRequiredService<BookLoansDbInitializer>();
	await dbInitializer.Init(CancellationToken.None);
}

await app.RunAsync();
