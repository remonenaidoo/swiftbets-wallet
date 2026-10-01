-- Rolls back 0006_spend_counters. Limits judged after this start from zero for the current periods.
DROP TABLE IF EXISTS wallet.SpendCounters;
