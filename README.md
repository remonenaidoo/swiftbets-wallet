# swiftbets-wallet

The SwiftBets wallet: the double-entry ledger, punter balances, reservations for bets, and the daily reconciliation.

| Part | What it does |
|---|---|
| `SwiftBets.Wallet.Api` | gRPC for reserve, commit, release, credit and debit; HTTP for balances and reconciliation runs |
| `SwiftBets.Wallet.Application` | Posting rules: every posting balances to zero, punters never go negative, idempotency keys make retries safe |
| `SwiftBets.Wallet.Infrastructure` | Dapper over SQL Server (`SbWallet`), outbox to Kafka, the reconciliation worker and its metrics |
| `SwiftBets.Wallet.Migrator` | DbUp migrations with rollbacks; `Migrator:SeedDemo=true` funds the demo punters |

Images: `ghcr.io/remonenaidoo/swiftbets-wallet` and `swiftbets-wallet-migrator`.

## Build and test

```bash
../swiftbets-platform/scripts/fetch-shared-packages.sh .
dotnet test
```

Integration tests start SQL Server and Redpanda in Docker (Testcontainers).

Moved out of `swiftbets-placement` with its full history (platform `scripts/split-repo.sh`).
