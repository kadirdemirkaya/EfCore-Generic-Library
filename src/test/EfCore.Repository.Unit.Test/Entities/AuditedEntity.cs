namespace EfCore.Repository.Unit.Test.Entities
{
    public abstract class AuditedEntity : ITestEntity
    {
        public DateTime CreatedAt { get; set; }
    }
}
