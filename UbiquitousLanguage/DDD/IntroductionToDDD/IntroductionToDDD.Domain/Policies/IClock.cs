namespace IntroductionToDDD.Domain.Policies
{
    public interface IClock
    {
        DateTimeOffset Now();
    }
}
