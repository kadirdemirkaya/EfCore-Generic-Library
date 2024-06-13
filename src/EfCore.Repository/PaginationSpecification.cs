namespace EfCore.Repository
{
    public class PaginationSpecification<TEntity> : SpecificationBase<TEntity>
       where TEntity : class
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }
    }
}
