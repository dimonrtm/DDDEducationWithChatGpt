using IntroductionToDDD.Domain.Enums;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public record LoanActivated(Guid reservationId, Guid readerId, Guid copyId, ReservationStatus status, DateTimeOffset? occuredAt);
   
}
