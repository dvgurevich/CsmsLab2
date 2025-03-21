using System.Threading.Tasks;

public interface IProductService
{
    Task<long> CreateProductAsync(string productName, decimal productPrice);
}