namespace EfCore.Repository
{
    public class PaginatedList<TEntity>
      where TEntity : class
    {
        public PaginatedList(List<TEntity> items, long totalItems, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            TotalItems = totalItems;
            Items = new List<TEntity>(pageSize);
            Items.AddRange(items);
        }

        private PaginatedList()
        {
        }

        public int PageIndex { get; }

        public int PageSize { get; }

        public int TotalPages { get; }

        public long TotalItems { get; }

        public List<TEntity> Items { get; }
    }
}
