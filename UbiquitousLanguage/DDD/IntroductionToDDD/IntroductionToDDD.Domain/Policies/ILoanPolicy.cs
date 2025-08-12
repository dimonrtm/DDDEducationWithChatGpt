namespace IntroductionToDDD.Domain.Policies
{
    public interface ILoanPolicy
    {
        bool HasActiveLoan(Guid copyId);
    }
}
