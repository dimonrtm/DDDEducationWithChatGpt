using Carshering.Booking.Domain.Exceptions;

namespace Carshering.Booking.Domain.ValueObjects
{
    public readonly record struct Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            if (amount <= 0) throw new DomainException("Deposit must be positive.");
            if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");
            Amount = amount;
            Currency = currency;
        }

        public override string ToString() => $"{Amount:0.00} {Currency}";
    }
}
