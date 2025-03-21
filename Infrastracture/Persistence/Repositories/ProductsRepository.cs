using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Options;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(DatabaseOptions dbOptions)
    {
        _connectionString = dbOptions.ConnectionString ?? throw new ArgumentNullException(nameof(dbOptions));
    }

    public async Task<long> CreateAsync(Product product)
    {
        const string sql = @"
            INSERT INTO products (product_name, product_price)
            VALUES (@ProductName, @ProductPrice)
            RETURNING product_id;";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(sql, product);
    }

    public async Task<IEnumerable<Product>> SearchAsync(IEnumerable<long> productIds, decimal? minPrice, decimal? maxPrice, string nameSubstring)
    {
        var sql = "SELECT product_id, product_name, product_price FROM products WHERE 1=1";
        var parameters = new DynamicParameters();

        if (productIds?.Any() == true)
        {
            sql += " AND product_id = ANY(@ProductIds)";
            parameters.Add("ProductIds", productIds);
        }
        if (minPrice.HasValue)
        {
            sql += " AND product_price >= @MinPrice";
            parameters.Add("MinPrice", minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            sql += " AND product_price <= @MaxPrice";
            parameters.Add("MaxPrice", maxPrice.Value);
        }
        if (!string.IsNullOrEmpty(nameSubstring))
        {
            sql += " AND product_name ILIKE @NameSubstring";
            parameters.Add("NameSubstring", $"%{nameSubstring}%");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Product>(sql, parameters);
    }
}