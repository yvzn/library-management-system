addEventListener('load', toggleDarkMode)

function toggleDarkMode() {
	const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)').matches
	const bootstrapTheme = prefersDarkScheme ? 'dark' : 'light'
	document.documentElement.dataset.bsTheme = bootstrapTheme
}
