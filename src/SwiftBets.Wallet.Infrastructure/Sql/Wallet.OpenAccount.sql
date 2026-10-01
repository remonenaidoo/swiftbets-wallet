-- A duplicate id or (user, currency) pair fails the insert; the caller re-reads and retries.
INSERT INTO wallet.Accounts (AccountId, Kind, Currency, UserId, CreatedAt)
VALUES (@AccountId, 1, @Currency, @UserId, @Now);
