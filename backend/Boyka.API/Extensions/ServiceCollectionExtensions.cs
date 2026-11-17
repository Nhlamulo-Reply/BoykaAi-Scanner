using Boyka.Application.Interfaces;
using Boyka.Application.Services;
using Boyka.Infrastructure.Data;
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

        return services;
    }
}
