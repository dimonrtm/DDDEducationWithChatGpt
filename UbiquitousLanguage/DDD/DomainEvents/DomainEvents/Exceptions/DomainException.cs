namespace Exceptions
{
    public sealed class DomainException(string message) : Exception(message);
}
