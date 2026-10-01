SELECT Period, Staked, Won, Deposited
FROM wallet.SpendCounters
WHERE AccountId = @AccountId
  AND ((Period = 0 AND PeriodStart = @Day) OR (Period = 1 AND PeriodStart = @Week) OR (Period = 2 AND PeriodStart = @Month));
