SELECT AccountId, Kind, Currency, Available, Reserved, IsBlacklisted, UserId, Bonus
FROM wallet.Accounts
WHERE UserId = @UserId AND Kind = 1
-- The first account (id = user id) comes first; ties on time fall back to currency so the order is stable.
ORDER BY CASE WHEN AccountId = UserId THEN 0 ELSE 1 END, CreatedAt, Currency;
