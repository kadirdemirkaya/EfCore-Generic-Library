using EfCore.Repository;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Test.Data;
using EfCore.Test.Dtos;
using EfCore.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            //_services.AddAutoMapper(AssemblyReference.Assembly);

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

        [Test]
        public async Task update_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetWriteRepository();
            var _readRepo = _unitOfWork.GetReadRepository();

            List<Basket> baskets = new();
            Basket basket = new()
            {
                PersonId = 34,
                Description = "Sample Description" // Provide a value for the required property
            };

            basket.Products.Add(new()
            {
                ProductDescription = "asdasdasd"
            });

            baskets.Add(basket);

            //var person = new Person() { Age = 12, Name = "REF TEST2", Basket = baskets };

            //await _writeRepo.AddAsync(person);
            //bool res = await _writeRepo.SaveChangesAsync();

            //if (res is true)
            //{s
            //Person existPerson = await _readRepo.GetAsync(p => p.Id == person.Id);

            //Person existPerson = await _readRepo.GetAsync(p => p.Id == 4, false);

            var updatePerson = new Person()
            {
                Id = 4,
                Name = "Updated Nameeeeeeeeeee",
                Age = 40
            };

            bool updateResponse = await _writeRepo.UpdateAsync(updatePerson);

            Console.WriteLine(updateResponse);
            //}
        }
    }
}
