using library_management_system.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace library_management_system.Controllers;

public class LoansController(BookLoansContext dbContext) : Controller
{
	public IActionResult Details(int id)
	{
		var loan = dbContext.Loans
			.Include(l => l.LoanBooks)
				.ThenInclude(lb => lb.Book)
			.FirstOrDefault(l => l.ID == id);

		if (loan == null)
		{
			return NotFound();
		}

		return View(loan);
	}
}
