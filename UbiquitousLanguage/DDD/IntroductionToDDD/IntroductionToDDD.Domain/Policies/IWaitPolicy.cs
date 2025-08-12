namespace IntroductionToDDD.Domain.Policies
{
    public interface IWaitPolicy
    {
        TimeSpan WaitDuration(); // N дней
    }
}
