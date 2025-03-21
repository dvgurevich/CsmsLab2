using System.Collections.Generic;
using System.Threading.Tasks;

public interface IProductRepository
{
    Task<long> CreateAsync(Product product);
    Task<IEnumerable<Product>> SearchAsync(IEnumerable<long> productIds, decimal? minPrice, decimal? maxPrice, string nameSubstring);
}
