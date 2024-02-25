namespace Sample.Abstractions.Services
{
    public class Package
    {
        public string Name { get; init; }
        public string Source { get; init; }
        public string Version { get; init; }
        public string[] Licenses { get; init; }
    }
}
