-- Rolls back 0008_bonus_and_withdrawals. Refuses while a withdrawal is held or any bonus money exists.
IF EXISTS (SELECT 1 FROM wallet.Reservations WHERE Purpose = 2 AND State = 1) OR EXISTS (SELECT 1 FROM wallet.Accounts WHERE Bonus <> 0)
    THROW 50001, 'Held withdrawals or bonus balances exist; settle them before rolling back.', 1;
ALTER TABLE wallet.Reservations DROP CONSTRAINT IF EXISTS DF_Reservations_Purpose;
ALTER TABLE wallet.Reservations DROP COLUMN IF EXISTS Purpose;
ALTER TABLE wallet.Accounts DROP CONSTRAINT IF EXISTS CK_Accounts_PunterBonusNotNegative;
ALTER TABLE wallet.Accounts DROP CONSTRAINT IF EXISTS DF_Accounts_Bonus;
ALTER TABLE wallet.Accounts DROP COLUMN IF EXISTS Bonus;
