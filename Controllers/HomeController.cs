using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace library_management_system.Controllers;

public class HomeController(BookLoansContext dbContext) : Controller
{
	public async Task<IActionResult> Index()
	{
		var currentLoans = await dbContext.Loans
			.Where(loan => loan.ReturnDate == null)
			.Include(loan => loan.LoanBooks)
			.Include(loan => loan.LoanMovies)
			.OrderByDescending(loan => loan.DueDate)
			.Take(5)
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync(HttpContext.RequestAborted);

		return View(currentLoans);
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
