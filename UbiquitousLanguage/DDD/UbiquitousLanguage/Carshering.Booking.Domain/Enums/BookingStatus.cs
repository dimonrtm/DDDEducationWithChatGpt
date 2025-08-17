namespace Carshering.Booking.Domain.Enums
{
    public enum BookingStatus
    {
        Placed,            // заявка создана
        DepositAuthorized, // залог авторизован
        Active,            // аренда началась
        Completed,
        Cancelled
    }
}
