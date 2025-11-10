namespace library_management_system.Models;

// Response models for OpenLibrary search API (https://openlibrary.org/search.json)
public class OpenLibrarySearchResponse
{
	public int NumFound { get; set; }
	public int Start { get; set; }
	public List<OpenLibraryDoc> Docs { get; set; } = [];
}

public class OpenLibraryDoc
{
	public string? Title { get; set; }
	public List<string> Author_name { get; set; } = [];
	public List<string> Isbn { get; set; } = [];
	public string? Key { get; set; }
	public int? First_publish_year { get; set; }
}

// Response model for OpenLibrary ISBN API (https://openlibrary.org/isbn/{isbn}.json)
public class OpenLibraryIsbnResponse
{
	public string? Title { get; set; }
	public List<OpenLibraryAuthorRef> Authors { get; set; } = [];
	public Dictionary<string, object>? Identifiers { get; set; }
	public string? Key { get; set; }
	public int? Number_of_pages { get; set; }
	public List<string> Publishers { get; set; } = [];
	public string? Publish_date { get; set; }
	public List<string> Isbn_10 { get; set; } = [];
	public List<string> Isbn_13 { get; set; } = [];
}

public class OpenLibraryAuthorRef
{
	public string? Key { get; set; }
}
