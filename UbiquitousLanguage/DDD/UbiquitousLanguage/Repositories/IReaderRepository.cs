namespace UbiquitousLanguage.Repositories
{
    public interface IReaderRepository
    {
        Task<int> CountActiveReservationsAsync(ReaderId readerId);
    }
}
