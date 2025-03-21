using System.Threading.Tasks;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<long> CreateProductAsync(string productName, decimal productPrice)
    {
        var product = new Product
        {
            ProductName = productName,
            ProductPrice = productPrice
        };

        return await _productRepository.CreateAsync(product);
    }
}