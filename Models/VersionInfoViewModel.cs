namespace library_management_system.Models;

public class VersionInfoViewModel
{
	public string? CurrentVersion { get; set; }
	public bool IsNewVersionAvailable { get; set; }
	public Uri? NewVersionUri { get; set; }
}
