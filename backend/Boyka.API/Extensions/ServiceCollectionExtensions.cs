using Boyka.API.Middleware;
using Boyka.Application.Interfaces;
using Boyka.Application.Services;
using Boyka.Infrastructure.Data;
using Boyka.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Boyka.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBoykaServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BoykaDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("BoykaDb")));

        services.AddScoped<IBoykaDbContext>(sp => sp.GetRequiredService<BoykaDbContext>());

        services.AddScoped<ITradingAccountService, TradingAccountService>();
        services.AddScoped<ITradeSignalService, TradeSignalService>();
        services.AddScoped<ITradeOrderService, TradeOrderService>();
        services.AddScoped<IRiskProfileService, RiskProfileService>();
        services.AddScoped<IMockTradeExecutionService, MockTradeExecutionService>();
        services.AddScoped<IEaRegistrationService, EaRegistrationService>();
        services.AddScoped<IMT5CommandService, MT5CommandService>();
        services.AddScoped<EaApiKeyMiddleware>();

        return services;
    }
}
