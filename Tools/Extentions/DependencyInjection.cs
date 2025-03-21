using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FluentMigrator.Runner;
using Microsoft.Extensions.Options;
using System;
using Npgsql;
public static class DependencyInjection
{
    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("DatabaseOptions:ConnectionString").Value;

        var databaseOptions = new DatabaseOptions
        {
            ConnectionString = connectionString
        };

        // Регистрируем DatabaseOptions как Singleton
        services.AddSingleton(databaseOptions);

        NpgsqlConnection.GlobalTypeMapper.MapEnum<OrderState>();

        // Регистрируем репозитории и сервисы
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IOrderHistoryRepository, OrderHistoryRepository>();

        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();

        // Регистрация сервисов для работы с миграциями
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(databaseOptions.ConnectionString)
                .ScanIn(typeof(CreateOrderManagementTables).Assembly).For.Migrations());
    }
}