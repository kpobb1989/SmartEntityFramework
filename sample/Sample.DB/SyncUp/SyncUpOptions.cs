using Microsoft.Extensions.Logging;

namespace Sample.DB.Options;

public class SyncUpOptions
{
    public bool FullSync { get; init; } = true;
    public int BatchSize { get; init; } = 1000;
    public ILogger? Logger { get; set; }
}