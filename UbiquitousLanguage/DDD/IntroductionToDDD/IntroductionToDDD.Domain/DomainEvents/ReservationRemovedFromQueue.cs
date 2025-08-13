namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record ReservationRemovedFromQueue(Guid CopyId, Guid ReservationId, Guid ReaderId, string Reason, string CanceledBy, string? Note, DateTimeOffset OccurredAt, int Version);
}
