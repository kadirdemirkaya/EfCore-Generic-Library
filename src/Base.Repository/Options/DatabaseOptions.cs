namespace Base.Repository.Options
{
    public class DatabaseOptions
    {
        public int RetryCount { get; set; } = 5;
        public bool IsRetry { get; set; } = true;

        public string? ConnectionUrl { get; set; }
        public object? Connection { get; set; }

    }
}
