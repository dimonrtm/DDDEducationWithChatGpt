namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record QueueHeadChanged(Guid CopyId, Guid HeadReservationId, DateTimeOffset OccurredAt, int Version);
}
