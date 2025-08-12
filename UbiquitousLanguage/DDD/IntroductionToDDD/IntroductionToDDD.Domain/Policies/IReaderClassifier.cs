using IntroductionToDDD.Domain.ValueObjects;

namespace IntroductionToDDD.Domain.Policies
{
    public interface IReaderClassifier
    {
        ReaderProfile Classify(Guid readerId); // Eligible, PriorityLevel
    }
}
