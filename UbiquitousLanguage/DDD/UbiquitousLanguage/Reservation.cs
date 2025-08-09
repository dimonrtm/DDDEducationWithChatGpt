using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace UbiquitousLanguage
{
    public record ReservationId(Guid Value);
    public record ReaderId(Guid Value);

    public record BookCopyId(Guid Value);

    public enum ReservationStatus { Created, Active, ActiveInReadingRoom, Fulfilled, Cancelled, Expired }

    public enum ReservationPriority
    {
        Staff = 0,        // самый высокий приоритет
        Researcher = 1,
        Regular = 2
    }
    public enum BookAccessType
    {
        RegularLoan,       // можно на руки
        ReadingRoomOnly    // только в зале
    }

    public class Reservation
    {
        public ReservationId Id { get; }
        public BookCopyId BookCopyId { get; }
        public ReaderId ReaderId { get; }
        public ReservationPriority Priority { get; }
        public int Position { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public ReservationStatus Status { get; private set; }

        private Reservation(
            ReservationId id,
            BookCopyId bookCopyId,
            ReaderId readerId,
            ReservationPriority priority,
            DateTime createdAt,
            int position)
        {
            Id = id;
            BookCopyId = bookCopyId;
            ReaderId = readerId;
            Priority = priority;
            CreatedAt = createdAt;
            Status = ReservationStatus.Created;
        }

        internal void SetPosition(int position) => Position = position;

        public void Activate(BookAccessType accessType) {
            if (Status != ReservationStatus.Created)
                throw new InvalidOperationException();
            Status = accessType == BookAccessType.ReadingRoomOnly
            ? ReservationStatus.ActiveInReadingRoom
            : ReservationStatus.Active;
        }
        public void Fulfill() { 
            if(Status != ReservationStatus.Active)
                throw new InvalidOperationException();
            Status = ReservationStatus.Fulfilled;
        }
        public void Cancel(string reason)
        {
            if (Status is ReservationStatus.Fulfilled or ReservationStatus.Cancelled)
                throw new InvalidOperationException();
            Status = ReservationStatus.Cancelled;
        }
    }
}
