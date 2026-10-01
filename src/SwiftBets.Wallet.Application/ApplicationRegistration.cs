using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftBets.Wallet.Application.Ledger;
using SwiftBets.Wallet.Application.Reconciliation;

namespace SwiftBets.Wallet.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddWalletApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PostingRunner>();
        services.AddScoped<ReserveFundsHandler>();
        services.AddScoped<SettleReservationHandler>();
        services.AddScoped<TransferHandler>();
        services.AddScoped<ReconcileLedgerHandler>();
        return services;
    }
}
