-- Rolls back 0007_currency_accounts. Refuses while any customer holds more than one account, since those would be orphaned.
IF EXISTS (SELECT 1 FROM wallet.Accounts WHERE Kind = 1 AND UserId <> AccountId)
    THROW 50001, 'Customers hold accounts in more than one currency; move or close them before rolling back.', 1;
DELETE FROM wallet.Accounts
WHERE AccountId IN ('00000000-0000-0000-0000-00000000b840', '00000000-0000-0000-0000-00000000f840')
  AND NOT EXISTS (SELECT 1 FROM wallet.LedgerEntries e WHERE e.AccountId = wallet.Accounts.AccountId);
DROP INDEX IF EXISTS UX_Accounts_UserCurrency ON wallet.Accounts;
ALTER TABLE wallet.Accounts DROP CONSTRAINT IF EXISTS CK_Accounts_PunterHasUser;
ALTER TABLE wallet.Accounts DROP COLUMN IF EXISTS UserId;
