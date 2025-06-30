using library_management_system.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<BookLoansContext>(
	options => options.UseSqlite($"Data Source={BookLoansContext.DbPath}"));
builder.Services.AddScoped<BookLoansDbInitializer>();

builder.Services.AddHttpLogging(options =>
{
	options.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.RequestMethod | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseBody;
	options.ResponseBodyLogLimit = 128;
	options.CombineLogs = true;
});
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.UseHttpLogging();

app.MapStaticAssets();

app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}")
		.WithStaticAssets();

using(var scope = app.Services.CreateScope()) {
	var serviceProvider = scope.ServiceProvider;
	var dbInitializer = serviceProvider.GetRequiredService<BookLoansDbInitializer>();
	await dbInitializer.Init(CancellationToken.None);
}

await app.RunAsync();
