public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}

public sealed record Layer(Guid Id, Crs Crs);
public sealed record LayerVersion(Guid Id, Guid LayerId, Crs Crs);
public sealed record PublicationAttempt(Layer Layer, LayerVersion Version);

public sealed class SameCrsSpec : ISpecification<PublicationAttempt>
{
    public bool IsSatisfiedBy(PublicationAttempt attempt)
    {
        // TODO: верните true, если CRS совпадают; иначе false
        return attempt.Layer.Crs == attempt.Version.Crs;
    }
}