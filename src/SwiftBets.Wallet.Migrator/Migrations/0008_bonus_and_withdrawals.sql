-- A bonus bucket (stakeable, never withdrawable) and the purpose of a reservation: a stake (captured into the house)
-- or a withdrawal (paid out to the funding account).
ALTER TABLE wallet.Accounts ADD Bonus bigint NOT NULL CONSTRAINT DF_Accounts_Bonus DEFAULT (0);
GO
ALTER TABLE wallet.Accounts ADD CONSTRAINT CK_Accounts_PunterBonusNotNegative CHECK (Kind <> 1 OR Bonus >= 0);
ALTER TABLE wallet.Reservations ADD Purpose tinyint NOT NULL CONSTRAINT DF_Reservations_Purpose DEFAULT (1);
