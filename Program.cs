using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repositories;
using Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<IStockRepository, StockRepository>();
        services.AddSingleton<IStockClosingPriceSyncService, StockClosingPriceSyncService>();
        services.AddSingleton(context.Configuration);
    })
    .Build();

using var scope = host.Services.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IStockClosingPriceSyncService>();
await service.SyncAsync();
