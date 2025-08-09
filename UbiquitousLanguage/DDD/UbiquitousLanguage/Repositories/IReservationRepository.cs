namespace UbiquitousLanguage.Repositories
{
    public interface IReservationRepository
    {
        Task<IReadOnlyList<Reservation>> GetActiveByCopyAsync(BookCopyId bookCopyId);
        Task AddAsync (Reservation reservation);
        Task UpdateAsync (Reservation reservation);
        Task UpsertRangeAsync(List<Reservation> reservations);
    }
}
