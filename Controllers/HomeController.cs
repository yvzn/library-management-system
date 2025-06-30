using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using library_management_system.Models;
using library_management_system.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class HomeController(BookLoansContext dbContext) : Controller
{
	public IActionResult Index()
	{
		var currentLoans = dbContext.Loans
			.Where(loan => loan.ReturnDate == null)
			.Include(loan => loan.LoanBooks)
			.AsNoTracking()
			.ToList();

		return View(currentLoans);
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
