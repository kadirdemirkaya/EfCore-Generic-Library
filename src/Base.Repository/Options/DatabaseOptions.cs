namespace Base.Repository.Options
{
    /// <summary>
    /// Supplies the context used by the <see cref="DatabaseOptions"/>-based repository registrations.
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// A connection string kept for the application's own use; the repositories do not read it.
        /// </summary>
        public string? ConnectionUrl { get; set; }

        /// <summary>
        /// The <c>DbContext</c> instance the repositories and the unit of work operate on.
        /// </summary>
        public object? Connection { get; set; }
    }
}
