using EfCore.Repository;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Test.Data;
using EfCore.Test.Dtos;
using EfCore.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace EfCore.Test
{
    [TestFixture]
    public class EfCoreExtensionTest
    {
        private readonly IUnitOfWork<Person> _unitOfWork;
        private ServiceCollection _services;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {

        }

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();

            InjectExtension(_services, ServiceLifetime.Scoped);
        }

        public void InjectExtension(ServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=genericrepocontext;Trusted_Connection=True;TrustServerCertificate=True"));

            var _sp2 = BuildServiceProvider();
            var context2 = _sp2.GetRequiredService<AppDbContext>();
            _services.EfCoreRepositoryServiceRegistration<IBaseEntity, AppDbContext>(ServiceLifetime.Scoped, AssemblyReference.Assembly);
        }

        public IServiceProvider BuildServiceProvider()
             => _services.BuildServiceProvider();

        public IUnitOfWork<TEntity> GetUnitOfWork<TEntity>(IServiceProvider sp)
            where TEntity : class, new()
            => sp.GetRequiredService<IUnitOfWork<TEntity>>();


        [Test]
        public async Task unitofwork_add_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetWriteRepository();

            await _writeRepo.AddAsync(new Person() { Age = 12, Name = "REF TEST2" });
            bool res = await _writeRepo.SaveChangesAsync();

            if (res)
                Assert.Pass();
            Assert.Fail();
        }

        [Test]
        public async Task unitofwork_get_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            var res = await _readRepo.GetListAsync();

            if (res != null)
                Assert.Pass();
            Assert.Fail();
        }

    }
}
