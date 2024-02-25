namespace Sample.Abstractions.Services.Interfaces
{
    public interface IPackageService
    {
        PagedList<Package> GetPackages(int pageNumber, int pageSize);
    }
}
