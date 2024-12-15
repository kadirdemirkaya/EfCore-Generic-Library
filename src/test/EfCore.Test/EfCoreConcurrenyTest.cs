using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Repository.Factory;
using EfCore.Test.Data;
using EfCore.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EfCore.Test
{
    [TestFixture]
    public class EfCoreConcurrenyTest
    {
        // Same reference 
        // Update process same time 
        // Add process same time
        // get process same time

        private readonly IUnitOfWork<Person> _unitOfWork;
        private ServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();

            #region NUnit inject
            //InjectDbContext<Person, AppDbContext>(_services);
            #endregion

            #region Extension
            InjectExtension(_services, ServiceLifetime.Scoped);
            #endregion
        }

        public void InjectDbContext<TEntity, TDbContext>(ServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
           where TDbContext : DbContext
           where TEntity : class, new()
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            //services.AddDbContext<AppDbContext>(options =>
            //     options.UseSqlite("Data Source=MyDatabase.db"));

            services.AddDbContext<AppDbContext>(options =>
                 options.UseSqlServer("Server=DESKTOP-KCT444U\\SQLEXPRESS;Database=genericrepocontext;Trusted_Connection=True;TrustServerCertificate=True"));

            //services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Server=localhost;port=5432;Database=AppDbContext;User Id=postgresql;Password=123"));

            //services.AddDbContext<AppDbContext>(options =>
            //    options.UseMySql(
            //        "Server=localhost;Port=3306;Database=mydatabase;User Id=user;Password=userpassword;",
            //        new MySqlServerVersion(new Version(8, 0, 21))
            //    ));


            services.Add(new ServiceDescriptor(
                typeof(IUnitOfWork<TEntity>),
                sp =>
                {
                    TDbContext dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext));
                    return RepositoryFactory<TDbContext>.CreateUnitOfWork<TEntity>(dbContext, sp);
                },
                serviceLifetime
            ));
        }

        public void InjectExtension(ServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=DESKTOP-KCT444U\\SQLEXPRESS;Database=genericrepocontext;Trusted_Connection=True;TrustServerCertificate=True"));

            #region with options and service provider
            //var _sp = BuildServiceProvider();
            //var context = _sp.GetRequiredService<AppDbContext>();
            //_services.EfCoreRepositoryServiceRegistration<Person, AppDbContext>(ServiceLifetime.Scoped, new Base.Repository.Options.DatabaseOptions() { Connection = context, IsRetry = true, RetryCount = 5 });
            #endregion

            #region dbcontext and service provider
            var _sp2 = BuildServiceProvider();
            var context2 = _sp2.GetRequiredService<AppDbContext>();
            _services.EfCoreRepositoryServiceRegistration<Person, AppDbContext>(ServiceLifetime.Scoped);
            #endregion
        }

        public IServiceProvider BuildServiceProvider()
           => _services.BuildServiceProvider();

        public IUnitOfWork<TEntity> GetUnitOfWork<TEntity>(IServiceProvider sp)
            where TEntity : class, new()
            => sp.GetRequiredService<IUnitOfWork<TEntity>>();
    }
}
