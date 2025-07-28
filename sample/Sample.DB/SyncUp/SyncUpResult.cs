namespace Sample.DB.SyncUp;

public class SyncUpResult
{
    public int Inserted { get; internal set; }
    public int Updated { get; internal set; }
    public int Deleted { get; internal set; }
    public int Total { get; internal set; }
}