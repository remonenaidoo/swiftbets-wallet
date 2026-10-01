SELECT AccountId, Kind, Currency, Available, Reserved, IsBlacklisted, UserId, Bonus
FROM wallet.Accounts
WHERE AccountId = @AccountId;
