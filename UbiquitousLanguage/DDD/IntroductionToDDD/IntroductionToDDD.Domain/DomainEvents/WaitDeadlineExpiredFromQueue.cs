namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record WaitDeadlineExpiredFromQueue(Guid CopyId, Guid ReservationId, Guid ReaderId, DateTimeOffset WaitDeadline, DateTimeOffset OccurredAt, int Version);
}
