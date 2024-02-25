namespace Sample.Abstractions.Services
{
    public class PagedList<T>
    {
        public IEnumerable<T> Data { get; init; } = new List<T>();
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
