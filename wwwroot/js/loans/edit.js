document.addEventListener('DOMContentLoaded', () => {
	const dueDateInput = document.getElementById('DueDate');
	const loanDateInput = document.getElementById('LoanDate');

	loanDateInput.addEventListener('change', () => {
		const loanDate = new Date(loanDateInput.value);
		if (!isNaN(loanDate.getTime())) {
			const dueDate = new Date(loanDate);
			dueDate.setDate(dueDate.getDate() + defaultLoanDuration);
			dueDateInput.valueAsDate = dueDate;
		}
	});

	const clearReturnDateButton = document.getElementById('ClearReturnDate');
	if (clearReturnDateButton) {
		const returnDateInput = document.getElementById('ReturnDate');
		clearReturnDateButton.addEventListener('click', () => {
			returnDateInput.value = '';
		});
	}
});
