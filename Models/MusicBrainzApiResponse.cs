namespace library_management_system.Models;

public class MusicBrainzApiResponse
{
	public Release[] Releases { get; set; } = [];
}

public class Release
{
	public string? Title { get; set; }
	public ArtistCredit[] ArtistCredit { get; set; } = [];
	public string? Date { get; set; }
	public string? Country { get; set; }
	public string? BarCode { get; set; }
	public LabelInfo[] LabelInfo { get; set; } = [];
	public int TrackCount { get; set; }
	public Medium[] Media { get; set; } = [];
	public string? Asin { get; set; }
	public string? Disambiguation { get; set; }
	public string Version => string.Join(" - ",
		new string?[] { Disambiguation, Country, Date, string.Join(" ", LabelInfo.Select(l => l.Label.Name)) }
		.Where(s => !string.IsNullOrEmpty(s)));
}

public class ArtistCredit
{
	public string? Name { get; set; }
}

public class LabelInfo
{
	public Label Label { get; set; } = new();
}

public class Label
{
	public string? Name { get; set; }
}

public class Medium
{
	public string? Format { get; set; }
	public int DiscCount { get; set; }
	public int TrackCount { get; set; }
}
