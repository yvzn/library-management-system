using library_management_system.Infrastructure;
using library_management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace library_management_system.Controllers;

public class LoanMoviesController(BookLoansContext dbContext) : Controller
{
	public async Task<IActionResult> Create(LoanMovie loanMovie)
	{
		if (!ModelState.IsValid)
		{
			TempData["ValidationErrorMessage"] = string.Join(" ", ModelState
				.Values
				.SelectMany(value => value.Errors)
				.Select(error => error.ErrorMessage)
				.Distinct());

			return RedirectToAction("SearchResults", "Movies", new
			{
				loanId = loanMovie.LoanID,
				title = loanMovie.Movie?.Title,
				director = loanMovie.Movie?.Director,
				releaseYear = loanMovie.Movie?.ReleaseYear,
				EAN = loanMovie.Movie?.EAN,
			});
		}

		var newlyCreatedMovie = await dbContext.Movies.AddAsync(loanMovie.Movie!, HttpContext.RequestAborted);

		loanMovie.MovieID = newlyCreatedMovie.Entity.ID;
		var newlyCreatedLoanMovie = await dbContext.LoanMovies.AddAsync(loanMovie, HttpContext.RequestAborted);

		await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

		return RedirectToAction("Details", "Loans", new { id = loanMovie.LoanID, previous = "AddMovie", relationshipId = newlyCreatedLoanMovie.Entity.ID });
	}
}
