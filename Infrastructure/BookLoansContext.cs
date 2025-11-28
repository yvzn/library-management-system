using Microsoft.EntityFrameworkCore;
using library_management_system.Models;

namespace library_management_system.Infrastructure;

public class BookLoansContext(DbContextOptions options) : DbContext(options)
{
	public DbSet<Loan> Loans { get; set; }
	public DbSet<LoanBook> LoanBooks { get; set; }
	public DbSet<Book> Books { get; set; }
	public DbSet<Movie> Movies { get; set; }
	public DbSet<LoanMovie> LoanMovies { get; set; }
	public DbSet<MusicDisc> MusicDiscs { get; set; }
	public DbSet<LoanMusicDisc> LoanMusicDiscs { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Loan>()
			.ToTable(nameof(Loan));

		modelBuilder.Entity<LoanBook>()
			.ToTable(nameof(LoanBook));

		modelBuilder.Entity<Book>()
			.ToTable(nameof(Book));

		modelBuilder.Entity<Movie>()
			.ToTable(nameof(Movie));

		modelBuilder.Entity<LoanMovie>()
			.ToTable(nameof(LoanMovie));

		modelBuilder.Entity<MusicDisc>()
			.ToTable(nameof(MusicDisc));

		modelBuilder.Entity<LoanMusicDisc>()
			.ToTable(nameof(LoanMusicDisc));
	}

	public static string DbPath
		=> Path.Join(DbDirectory, "BookLoans.db");

	public static string DbDirectory
		=> Path.Join(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"library-management-system");
}
