document.addEventListener('DOMContentLoaded', toggleDarkMode);

function toggleDarkMode() {
	const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)').matches;
	const bootstrapTheme = prefersDarkScheme ? 'dark' : 'light';
	document.documentElement.dataset.bsTheme = bootstrapTheme;
}

document.addEventListener('DOMContentLoaded', initializeSignalR);

function initializeSignalR() {
	const connection = new signalR.HubConnectionBuilder()
		.withUrl('/userz')
		.withAutomaticReconnect()
		.build();

	connection.start().catch(err => console.error(err.toString()));
}
