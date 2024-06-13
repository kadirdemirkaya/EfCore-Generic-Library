namespace EfCore.Repository
{
    public class Specification<TEntity> : SpecificationBase<TEntity>
        where TEntity : class
    {
        public int? Skip { get; set; }

        public int? Take { get; set; }
    }
}
