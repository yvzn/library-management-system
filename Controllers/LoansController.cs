using library_management_system.Infrastructure;
using library_management_system.Models;
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

	public IActionResult New()
	{
		var loan = new Loan
		{
			LoanDate = DateTime.Now,
			DueDate = DateTime.Now.AddDays(30),
		};

		return View(loan);
	}

	public IActionResult Create(Loan loan)
	{
		if (!ModelState.IsValid)
		{
			return View(nameof(New), loan);
		}

		var created = dbContext.Loans.Add(loan);
		dbContext.SaveChanges();

		return RedirectToAction("Details", "Loans", new { id = created.Entity.ID });
	}
}
