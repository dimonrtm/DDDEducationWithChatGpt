public sealed record Crs(string Code);
public sealed record GeoShape(object Native, Crs Crs); // Native будет адаптирован в инфраструктуре
public sealed record Feature(string Id, GeoShape Shape, IReadOnlyDictionary<string, object>? Attrs);
public sealed record FeatureArea(string Id, GeoShape Shape, double Area);
public sealed record IntersectionResult(IReadOnlyList<FeatureArea> Items);

// Порт для геометрических операций (реализация в инфраструктуре)
public interface IGeometryOps
{
    GeoShape? Intersect(GeoShape a, GeoShape b);
    double Area(GeoShape shape);
    bool IsEmpty(GeoShape shape);
}

// Расчётная доменная служба
public sealed class IntersectionCalculator
{
    private readonly IGeometryOps _ops;
    public IntersectionCalculator(IGeometryOps ops) => _ops = ops;

    // TODO: реализуйте тело
    public IntersectionResult Calculate(IReadOnlyList<Feature> left, IReadOnlyList<Feature> right)
    {
        // 1) Переберите пары (простая версия)
        // 2) Получите пересечение через _ops.Intersect(...)
        // 3) Пропустите пустые (null или _ops.IsEmpty(...))
        // 4) Посчитайте площадь через _ops.Area(...)
        // 5) Верните IntersectionResult с коллекцией FeatureArea
        throw new NotImplementedException();
    }
}