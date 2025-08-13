namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record ActiveLoanCleared(Guid CopyId, Guid ReservationId, DateTimeOffset OccurredAt, int Version);
}
