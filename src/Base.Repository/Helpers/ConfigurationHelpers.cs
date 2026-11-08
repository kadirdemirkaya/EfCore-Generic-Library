using Microsoft.Extensions.Configuration;

namespace Base.Repository.Helpers
{
    /// <summary>
    /// Binds configuration sections to option models.
    /// </summary>
    public static class ConfigurationHelpers
    {
        /// <summary>
        /// Creates a <typeparamref name="TModel"/> and binds the configuration section <paramref name="section"/> to it.
        /// </summary>
        /// <typeparam name="TModel">The option model.</typeparam>
        /// <param name="configuration">The configuration to read from.</param>
        /// <param name="section">The section key.</param>
        /// <returns>The bound model; its defaults are kept when the section is missing.</returns>
        public static TModel GetOptions<TModel>(this IConfiguration configuration, string section) where TModel : new()
        {
            var model = new TModel();
            configuration.GetSection(section).Bind(model);
            return model;
        }
    }
}
