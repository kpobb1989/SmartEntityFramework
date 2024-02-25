namespace Sample.Abstractions.Services
{
    public class PagedList<T>
    {
        public IEnumerable<T> Data { get; init; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
