// Порты слоя Application/Domain
public interface ILayerRepository
{
    Task<Layer?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(Layer layer, CancellationToken ct);
}
public interface ILayerVersionRepository
{
    Task<LayerVersion?> GetAsync(Guid id, CancellationToken ct);
}
public interface IPublicationPolicy
{
    PublicationCheckResult CanPublish(Layer layer, LayerVersion version, PublicationContext ctx);
}
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}
public interface IEventBus
{
    Task PublishAsync(IDomainEvent evt, CancellationToken ct);
}

// Доменные типы (упрощённо)
public sealed record Crs(string Code);
public sealed record Layer(Guid Id, Crs Crs)
{
    // Инвариант «ровно одна Published» соблюдается здесь
    public void Publish(Guid versionId) { /* изменить состояние, поднять событие внутри, если хотите */ }
}
public sealed record LayerVersion(Guid Id, Guid LayerId, Crs Crs);
public sealed record PublicationContext(DateTimeOffset Now);
public sealed record PublicationCheckResult(bool IsAllowed, string? Reason);
public interface IDomainEvent { }
public sealed record LayerVersionPublished(Guid LayerId, Guid VersionId, DateTimeOffset At) : IDomainEvent;

public sealed record PublishResult(bool Ok, string? Error);

// Прикладной сервис (ваша реализация внутри метода)
public sealed class PublishLayerVersionHandler
{
    private readonly ILayerRepository _layers;
    private readonly ILayerVersionRepository _versions;
    private readonly IPublicationPolicy _policy;
    private readonly IUnitOfWork _uow;
    private readonly IEventBus _bus;

    public PublishLayerVersionHandler(
        ILayerRepository layers,
        ILayerVersionRepository versions,
        IPublicationPolicy policy,
        IUnitOfWork uow,
        IEventBus bus)
    { _layers = layers; _versions = versions; _policy = policy; _uow = uow; _bus = bus; }

    public async Task<PublishResult> PublishAsync(Guid layerId, Guid versionId, CancellationToken ct)
    {
        // 1) Загрузить Layer и LayerVersion
        // 2) Если чего-то нет — вернуть NotFound (ошибка в результирующей модели)
        // 3) Вызвать политику _policy.CanPublish(...)
        // 4) Если запрещено — вернуть причину отказа
        // 5) Вызвать агрегат: layer.Publish(versionId)
        // 6) Сохранить через репозиторий и зафиксировать UnitOfWork
        // 7) Опубликовать событие LayerVersionPublished
        // 8) Вернуть успех
        throw new NotImplementedException();
    }
}