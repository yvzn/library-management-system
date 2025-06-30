namespace library_management_system.Models;

public class GoogleBooksApiResponse
{
	public List<Item> Items { get; set; } = [];
}

public class Item
{
	public VolumeInfo VolumeInfo { get; set; } = new ();
}

public class VolumeInfo
{
	public string? Title { get; set; }

	public List<string> Authors { get; set; } = [];

	public List<IndustryIdentifier> IndustryIdentifiers { get; set; } = [];

	public ImageLinks ImageLinks { get; set; } = new ();
}

public class IndustryIdentifier
{
	public string? Type { get; set; }

	public string? Identifier { get; set; }
}

public class ImageLinks
{
	public string? SmallThumbnail { get; set; }

	public string? Thumbnail { get; set; }
}
