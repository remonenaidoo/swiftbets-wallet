using System.Security.Claims;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Contracts.Errors;
using SwiftBets.Wallet.Application.Ledger;
using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;

namespace SwiftBets.Wallet.Api.Endpoints;

/// <summary>The signed-in customer's balances and statement, through the gateway's /api/me/wallet routes.</summary>
public static class CustomerEndpoints
{
    public const int MaxStatementLines = 100;

    public static IEndpointRouteBuilder MapCustomerWallet(this IEndpointRouteBuilder endpoints)
    {
        var me = endpoints.MapGroup("/me/wallet").RequireAuthorization();

        me.MapGet("/accounts", async (ClaimsPrincipal user, HttpContext context, AccountsHandler accounts) =>
            UserId(user) is { } userId
                ? Results.Ok((await accounts.ListAsync(userId, context.RequestAborted)).Select(View))
                : Unauthenticated(context));

        me.MapGet("/statement", async (string? currency, long? before, int? limit, ClaimsPrincipal user, HttpContext context, AccountsHandler accounts, IWalletStore store) =>
        {
            if (UserId(user) is not { } userId)
            {
                return Unauthenticated(context);
            }

            var account = (await accounts.ListAsync(userId, context.RequestAborted)).FirstOrDefault(a => currency is null || a.Currency == currency);
            if (account is null)
            {
                return Error.NotFound("account_not_found", "You have no account in that currency.").ToHttpResult(context);
            }

            var lines = await store.StatementAsync(account.AccountId, before, Math.Clamp(limit ?? 50, 1, MaxStatementLines), context.RequestAborted);
            return Results.Ok(new
            {
                account = View(account),
                lines = lines.Select(l => new { l.Sequence, l.PostingId, kind = Describe(l.Kind), l.Amount, l.AvailableAfter, l.Reference, l.PostedAt }),
                next = lines.Count == 0 ? null : (long?)lines[^1].Sequence,
            });
        });

        return endpoints;
    }

    private static Guid? UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // A token without a subject is a service token, not a customer.
    private static IResult Unauthenticated(HttpContext context) => Results.Unauthorized();

    private static object View(Account a) => new { a.AccountId, a.Currency, a.Available, a.Reserved, a.Bonus };

    private static string Describe(PostingKind kind) => kind switch
    {
        PostingKind.Reserve => "bet",
        PostingKind.Release => "betReturned",
        PostingKind.Credit => "winnings",
        PostingKind.Debit => "correction",
        PostingKind.TopUp => "topUp",
        PostingKind.Deposit => "deposit",
        PostingKind.WithdrawalHold => "withdrawal",
        PostingKind.WithdrawalReturned => "withdrawalReturned",
        _ => kind.ToString(),
    };
}
