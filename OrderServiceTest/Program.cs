using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Настройка конфигурации
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Настройка DI-контейнера
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        DependencyInjection.RegisterServices(serviceCollection, configuration);
        serviceCollection.AddLogging(builder => builder.AddConsole());

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Получаем экземпляры сервисов
        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        var productService = serviceProvider.GetRequiredService<IProductService>();

        serviceProvider.RunMigrations();
        Console.WriteLine("Миграции успешно применены.");

        var productId1 = await productService.CreateProductAsync("Product A", 100.00m);
        Console.WriteLine($"Создан товар с ID: {productId1}");

        var productId2 = await productService.CreateProductAsync("Product B", 150.00m);
        Console.WriteLine($"Создан товар с ID: {productId2}");

        // Создание заказа
        var order = new Order { OrderState = OrderState.created, OrderCreatedBy = "TestUser" };
        var orderId = await orderService.CreateOrderAsync(order);
        Console.WriteLine($"Создан заказ с ID: {orderId}");

        // Добавление товара в заказ
        var orderItem = new OrderItem { OrderId = orderId, ProductId = 1, OrderItemDeleted = false };
        var orderAnotherItem = new OrderItem { OrderId = orderId, ProductId = 2, OrderItemDeleted = false };
        await orderService.AddOrderItemAsync(orderId, orderItem);
        Console.WriteLine("Товар добавлен в заказ");

        // Удаление товара
        await orderService.RemoveOrderItemAsync(orderItem.OrderId);
        Console.WriteLine("Товар удален из заказа");

        // Перевод заказа в работу
        await orderService.ChangeOrderStatusAsync(orderId, OrderState.processing);
        Console.WriteLine("Заказ переведен в работу");

        // Завершение заказа
        await orderService.ChangeOrderStatusAsync(orderId, OrderState.completed);
        Console.WriteLine("Заказ выполнен");

        // История заказа
        var history = await orderService.GetOrderHistoryAsync(orderId);
        Console.WriteLine("История заказа:");
        foreach (var record in history)
        {
            Console.WriteLine($"Тип события: {record.HistoryType}, Данные: {record.Payload}");
        }
    }
}