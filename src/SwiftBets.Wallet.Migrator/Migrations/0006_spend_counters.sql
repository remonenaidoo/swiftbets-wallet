-- Responsible-gambling totals per punter and period (ADR 0006). Period: 0 day, 1 week, 2 month; PeriodStart is the local
-- (UTC+2) date the period begins. Written only under the account lock, in the same transaction as the money movement.
CREATE TABLE wallet.SpendCounters
(
    AccountId   uniqueidentifier NOT NULL CONSTRAINT FK_SpendCounters_Accounts REFERENCES wallet.Accounts (AccountId),
    Period      tinyint          NOT NULL,
    PeriodStart date             NOT NULL,
    Staked      bigint           NOT NULL,
    Won         bigint           NOT NULL,
    Deposited   bigint           NOT NULL,
    CONSTRAINT PK_SpendCounters PRIMARY KEY (AccountId, Period, PeriodStart)
);
