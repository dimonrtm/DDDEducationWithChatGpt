using DomainServices.PublishVersionHandler;

namespace DomainServices.Layer
{
    public sealed class DomainException : Exception
    {
        public string Code { get; }
        public DomainException(string code, string message) : base(message) => Code = code;
    }

    // ваш агрегат (можно оставить record, но тогда храните поле через mutable-поле)
    public sealed class Layer
    {
        public Guid Id { get; }
        public Crs Crs { get; }
        private Guid? _publishedVersionId;
        public Guid? PublishedVersionId => _publishedVersionId;

        public Layer(Guid id, Crs crs) { Id = id; Crs = crs; }

        public void Publish(Guid versionId)
        {
            // TODO: если _publishedVersionId есть и не равен versionId — бросить DomainException
            // иначе присвоить _publishedVersionId = versionId
            if (_publishedVersionId.HasValue && _publishedVersionId.Value != versionId)
            {
                throw new DomainException("AlreadyPublished", "НЕльзя опубликовать версию, потому что у слоя уже существует опубликованная версия");
            }
            _publishedVersionId = versionId;
        }
    }
}
