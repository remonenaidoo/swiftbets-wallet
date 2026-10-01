-- Wallet v2: a customer holds one punter account per currency, keyed by (UserId, Currency). Existing punter accounts
-- were opened with the user id as account id, so they map to (user, their currency) unchanged.
ALTER TABLE wallet.Accounts ADD UserId uniqueidentifier NULL;
GO
UPDATE wallet.Accounts SET UserId = AccountId WHERE Kind = 1;
ALTER TABLE wallet.Accounts ADD CONSTRAINT CK_Accounts_PunterHasUser CHECK (Kind <> 1 OR UserId IS NOT NULL);
CREATE UNIQUE INDEX UX_Accounts_UserCurrency ON wallet.Accounts (UserId, Currency) WHERE UserId IS NOT NULL;

-- House and funding accounts for each supported currency besides ZAR.
INSERT INTO wallet.Accounts (AccountId, Kind, Currency, CreatedAt)
SELECT v.AccountId, v.Kind, v.Currency, SYSUTCDATETIME()
FROM (VALUES ('00000000-0000-0000-0000-00000000b840', 2, 'USD'),
             ('00000000-0000-0000-0000-00000000f840', 3, 'USD')) v (AccountId, Kind, Currency)
WHERE NOT EXISTS (SELECT 1 FROM wallet.Accounts a WHERE a.AccountId = v.AccountId);
