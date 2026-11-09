namespace EfCore.Repository
{
    /// <summary>
    /// One page of a keyset (cursor) paginated read: the rows of the page, whether more rows follow, and the cursor to
    /// pass to the next <c>GetPageAfterAsync</c> call.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the key the rows are ordered and paged by.</typeparam>
    public sealed class KeysetPage<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>
        /// Creates a page from its rows and navigation values.
        /// </summary>
        /// <param name="items">The rows of the page, in page order. The sequence is copied.</param>
        /// <param name="pageSize">The maximum number of rows a page holds.</param>
        /// <param name="hasNextPage"><see langword="true"/> when more rows follow this page.</param>
        /// <param name="nextCursor">The key of the last row of the page, or <see langword="default"/> when the page is empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
        public KeysetPage(IEnumerable<TEntity> items, int pageSize, bool hasNextPage, TKey? nextCursor)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            Items = new List<TEntity>(items);
            PageSize = pageSize;
            HasNextPage = hasNextPage;
            NextCursor = nextCursor;
        }

        /// <summary>
        /// The rows of the page, in the order of the key (ascending or descending as requested).
        /// </summary>
        public IReadOnlyList<TEntity> Items { get; }

        /// <summary>
        /// The maximum number of rows a page holds.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// <see langword="true"/> when at least one more row follows this page at the time it was read.
        /// </summary>
        public bool HasNextPage { get; }

        /// <summary>
        /// The key of the last row of the page: pass it as the <c>after</c> argument to read the next page. It is
        /// <see langword="default"/> (<see langword="null"/>, or zero for numeric keys) when the page is empty, so check
        /// <see cref="HasNextPage"/> before asking for another page.
        /// </summary>
        public TKey? NextCursor { get; }
    }
}
