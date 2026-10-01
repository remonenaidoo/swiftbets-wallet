INSERT INTO wallet.Accounts (AccountId, Kind, Currency, UserId, CreatedAt)
SELECT @AccountId, 1, @Currency, @AccountId, @Now
WHERE NOT EXISTS (SELECT 1 FROM wallet.Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @AccountId);
