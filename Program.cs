using library_management_system.Infrastructure;
using library_management_system.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



// -- Add services to the container. ------------------------------------------

builder.Services.AddHttpLogging(options =>
{
	options.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.RequestMethod | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseBody;
	options.ResponseBodyLogLimit = 128;
	options.CombineLogs = true;
});
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("BookLoansDb");
if (string.IsNullOrEmpty(connectionString)) connectionString = $"Data Source={BookLoansContext.DbPath}";
builder.Services.AddDbContext<BookLoansContext>(
	options => options.UseSqlite(connectionString));
builder.Services.AddScoped<BookLoansDbInitializer>();

builder.Services.AddHostedService<LaunchBrowserOnStartup>();

var app = builder.Build();



// -- Configure the HTTP request pipeline. ------------------------------------

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
