SELECT ReservationId, AccountId, Amount, Currency, Reference, State, Purpose
FROM wallet.Reservations WITH (UPDLOCK, ROWLOCK)
WHERE ReservationId = @ReservationId;
