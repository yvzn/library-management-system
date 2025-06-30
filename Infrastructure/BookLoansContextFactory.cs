using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace library_management_system.Infrastructure;

public class BookLoansContextFactory : IDesignTimeDbContextFactory<BookLoansContext>
{
	public BookLoansContext CreateDbContext(string[] args)
	{
		Directory.CreateDirectory(BookLoansContext.DbDirectory);

		var optionsBuilder = new DbContextOptionsBuilder<BookLoansContext>();
		optionsBuilder.UseSqlite($"Data Source={BookLoansContext.DbPath}");

		return new BookLoansContext(optionsBuilder.Options);
	}
}
