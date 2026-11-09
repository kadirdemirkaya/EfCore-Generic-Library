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
            Items = new List<TEntity>(items);
        }

        private PaginatedList()
        {
            Items = null!;
        }

        public int PageIndex { get; }

        public int PageSize { get; }

        public int TotalPages { get; }

        public long TotalItems { get; }

        public IReadOnlyList<TEntity> Items { get; private set; }

        /// <summary>
        /// <see langword="true"/> when a page precedes this one (<see cref="PageIndex"/> is greater than 1).
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// <see langword="true"/> when a page follows this one (<see cref="PageIndex"/> is less than <see cref="TotalPages"/>).
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
