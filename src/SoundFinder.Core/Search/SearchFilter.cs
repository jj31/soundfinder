namespace SoundFinder.Core.Search;

public sealed class SearchFilter
{
    public string Query { get; set; } = string.Empty;
    public long? LibraryId { get; set; }
    public double? MinDurationSeconds { get; set; }
    public double? MaxDurationSeconds { get; set; }
    public string? Format { get; set; }
    public string? TagContains { get; set; }
    public double? MinBpm { get; set; }
    public double? MaxBpm { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = 200;
}

public sealed class SearchResult
{
    public List<Models.SampleFile> Items { get; set; } = new();
    public int Total { get; set; }
}
