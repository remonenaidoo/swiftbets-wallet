INSERT INTO wallet.Reservations (ReservationId, AccountId, Amount, Currency, Reference, State, Purpose, CreatedAt, UpdatedAt)
VALUES (@ReservationId, @AccountId, @Amount, @Currency, @Reference, @State, @Purpose, @Now, @Now);
