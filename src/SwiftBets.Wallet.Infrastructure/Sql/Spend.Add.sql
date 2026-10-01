-- The account row is already locked, so no other writer can race this upsert for the same account.
MERGE wallet.SpendCounters AS target
USING (VALUES (0, @Day), (1, @Week), (2, @Month)) AS source (Period, PeriodStart)
    ON target.AccountId = @AccountId AND target.Period = source.Period AND target.PeriodStart = source.PeriodStart
WHEN MATCHED THEN
    UPDATE SET Staked = target.Staked + @Staked, Won = target.Won + @Won, Deposited = target.Deposited + @Deposited
WHEN NOT MATCHED THEN
    INSERT (AccountId, Period, PeriodStart, Staked, Won, Deposited) VALUES (@AccountId, source.Period, source.PeriodStart, @Staked, @Won, @Deposited);
