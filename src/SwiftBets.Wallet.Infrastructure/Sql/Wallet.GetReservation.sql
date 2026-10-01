SELECT ReservationId, AccountId, Amount, Currency, Reference, State, Purpose
FROM wallet.Reservations
WHERE ReservationId = @ReservationId;
