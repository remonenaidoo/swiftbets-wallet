SELECT AccountId, Kind, Currency, Available, Reserved, IsBlacklisted, UserId, Bonus
FROM wallet.Accounts WITH (UPDLOCK, ROWLOCK)
WHERE AccountId = @AccountId;
