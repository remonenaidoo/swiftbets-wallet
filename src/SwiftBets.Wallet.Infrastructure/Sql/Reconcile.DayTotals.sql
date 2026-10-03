-- One UTC day of money movement, split the way the reporting warehouse counts it. Casino postings carry casino: keys.
SELECT
    COALESCE(SUM(CASE WHEN Kind = 3 AND IdempotencyKey NOT LIKE 'casino:%' THEN Amount END), 0) AS SportsStakes,
    COALESCE(SUM(CASE WHEN Kind = 5 AND IdempotencyKey NOT LIKE 'casino:%' THEN Amount END), 0) AS SportsCredits,
    COALESCE(SUM(CASE WHEN Kind = 6 AND IdempotencyKey NOT LIKE 'casino:%' THEN Amount END), 0) AS SportsDebits,
    COALESCE(SUM(CASE WHEN IdempotencyKey LIKE 'casino:%' AND Kind IN (3, 6) THEN Amount END), 0) AS CasinoStaked,
    COALESCE(SUM(CASE WHEN IdempotencyKey LIKE 'casino:%' AND Kind IN (4, 5) THEN Amount END), 0) AS CasinoReturned
FROM wallet.Postings
WHERE PostedAt >= @From AND PostedAt < @To;
