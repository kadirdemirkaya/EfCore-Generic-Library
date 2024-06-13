using Base.Repository.Options;
using EfCore.Repository;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Repository.Factory;
using EfCore.Test.Data;
using EfCore.Test.Dtos;
using EfCore.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Client;
using System.Linq.Expressions;

namespace EfCore.Test
{
    [TestFixture]
    public class EfCoreTest
    {
        private readonly IUnitOfWork<Person> _unitOfWork;
        private ServiceCollection _services;

        [OneTimeSetUp]// S�n�f genelinde yap�lacak kurulum i�lemleri
        public void OneTimeSetUp()
        {

        }

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


        [Test]
        public async Task unitofwork_get_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();
            //var _readRepo = _unitOfWork.GetDbReadRepository(true);
            //var _readRepo = _unitOfWork.GetDbReadRepository(true, true);
            //var _readRepo = _unitOfWork.GetBaseReadRepository(true, true);
            //var _readRepo = _unitOfWork.GetBaseReadRepository(true);
            //var _readRepo = _unitOfWork.GetBaseReadRepository();

            var res = await _readRepo.GetListAsync();

            //var res1 = await _readRepo.GetAsync(p => p.Age < 30, false);

            //var res2 = await _readRepo.GetAsync(p => p.Age > 40, b => b.Include(ba => ba.Basket).ThenInclude(p => p.Products));

            Specification<Person> specification = new() { Skip = 0, Take = 5 };
            //specification.Conditions.Add(p => p.Age < 100);
            specification.Conditions.Add(p => p.Age < 100 && p.Id == 4);
            specification.Includes = query => query.Include(p => p.Basket).ThenInclude(b => b.Products);
            specification.OrderBy = query => query.OrderByDescending(p => p.Age);
            //var res3 = await _readRepo.GetAsync(specification);

            Expression<Func<Person, PersonDto>> selectExpression = person => new PersonDto
            {
                Name = person.Name,
                Id = person.Id,
                Age = person.Age,
                BasketDtos = person.Basket.Select(basket => new BasketDto
                {
                    Id = basket.Id,
                    Description = basket.Description,
                    PersonId = basket.PersonId,
                    ProductDtos = basket.Products.Select(product => new ProductDto
                    {
                        Id = product.Id,
                        BasketId = product.BasketId,
                        ProductDescription = product.ProductDescription
                    }).ToList()
                }).ToList()
            };

            //PersonDto products = await _readRepo.GetAsync<PersonDto>(specification, selectExpression);

            //if (products != null)
            //    Assert.Pass();
            //Assert.Fail();
        }


        [Test]
        public async Task unitofwork_add_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetWriteRepository();
            //var _writeRepo = _unitOfWork.GetWriteRepository(true);
            //var _writeRepo = _unitOfWork.GetWriteRepository(true,true);
            //var _writeRepo = _unitOfWork.GetBaseWriteRepository(true,true);
            //var _writeRepo = _unitOfWork.GetBaseWriteRepository(true);
            //var _writeRepo = _unitOfWork.GetBaseWriteRepository();
            //var _writeRepo = _unitOfWork.GetDbWriteRepository();
            //var _writeRepo = _unitOfWork.GetDbWriteRepository(true);
            //var _writeRepo = _unitOfWork.GetDbWriteRepository(true,true);

            await _writeRepo.AddAsync(new Person() { Age = 12, Name = "REF TEST2" });
            bool res = await _writeRepo.SaveChangesAsync();

            if (res)
                Assert.Pass();
            Assert.Fail();
        }


        [Test]
        public async Task unitofwork_add_test2()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);

            #region
            await _unitOfWork.GetDbWriteRepository().AddAsync(new Person() { Age = 31, Name = "NUNT TEST" });

            List<Person> persons = new();
            persons.Add(new() { Age = 22, Name = "mehmet" });
            persons.Add(new() { Age = 23, Name = "omer" });
            await _unitOfWork.GetDbWriteRepository().AddAsync(persons);
            #endregion

            #region
            Person person = new() { Age = 45, Name = "test" };
            person.Basket = new()
            {
                new Basket(){ Description = "basket1",
                    Products = new()
                    {
                        new Product(){ProductDescription = "product1"},
                        new Product(){ProductDescription = "product2"},
                    }
                }
            };
            await _unitOfWork.GetDbWriteRepository().AddAsync(person);
            #endregion

            bool kres = await _unitOfWork.SaveChangesAsync();

            if (kres)
                Assert.Pass();
            Assert.Fail();
        }

        [Test]
        public async Task unitofwork_delete_transaction_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetDbWriteRepository();
            var _readRepo = _unitOfWork.GetDbReadRepository();

            using (var transaction = await _writeRepo.BeginTransactionAsync())
            {
                await _writeRepo.AddAsync(new Person() { Age = 47, Name = "Can" });
                await _writeRepo.SaveChangesAsync();

                Person person = await _readRepo.GetAsync(p => p.Age == 47 && p.Name == "Can");

                bool res = await _writeRepo.DeleteAsync(person);

                if (res)
                {
                    await transaction.RollbackAsync();
                    return;
                }
                await transaction.CommitAsync();
            }
        }

        [Test]
        public async Task unitofwork_executecommand_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetDbWriteRepository();

            var id = await _writeRepo.InsertAsync(new Person() { Age = 47, Name = "FOR DELETE" });

            string query = @"DELETE FROM Persons WHERE Id = {0}";
            //string query = "DELETE FROM \"Person\" WHERE \"Id\" = @p0";

            bool res = await _writeRepo.ExecuteCommandAsync(query, id);

        }

        [Test]
        public async Task unitofwork_insert_test()
        {

            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _writeRepo = _unitOfWork.GetWriteRepository();
            //var _writeRepo = _unitOfWork.GetWriteRepository(true, true);
            //var _writeRepo = _unitOfWork.GetWriteRepository(true);

            Person p1 = new()
            {
                Age = 99,
                Name = "memati",
                Basket = new()
                {
                    new Basket()
                    {
                        Description = "computer",
                        Products = new()
                        {
                            new Product()
                            {
                                ProductDescription = "pc",
                            },
                            new Product()
                            {
                                ProductDescription = "laptop",
                            }
                        }
                    }
                }
            };

            var p1Id = await _writeRepo.InsertAsync(p1);

            List<Person> persons = new()
            {
                new()
                {
                    Age = 68,
                    Name = "mehmet",
                    Basket = new()
                    {
                        new Basket()
                        {
                            Description = "food",
                            Products = new()
                            {
                                new Product()
                                {
                                    ProductDescription = "kebab",
                                },
                                new Product()
                                {
                                    ProductDescription = "lahmacun",
                                }
                            }
                        }
                    }
                },
                new()
                {
                    Age = 78,
                    Name = "omer",
                    Basket = new()
                    {
                        new Basket()
                        {
                            Description = "Book",
                            Products = new()
                            {
                                new Product()
                                {
                                    ProductDescription = "Portakal",
                                },
                                new Product()
                                {
                                    ProductDescription = "Aci Bamem",
                                }
                            }
                        }
                    }
                },
            };

            var personsId = await _writeRepo.InsertAsync(persons);

            if (personsId.Count() > 0)
                Assert.Pass();
            Assert.Fail();
        }

        [Test]
        public async Task unitofwork_getlist_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetReadRepository();


            var persons1 = await _readRepo.GetListAsync(false);



            var persons2 = await _readRepo.GetListAsync(p => p.Age < 50, p => p.Include(b => b.Basket).ThenInclude(p => p.Products), false);



            Specification<Person> specification = new() { Skip = 0, Take = 5 };
            specification.Conditions.Add(p => p.Age < 100);
            specification.Includes = query => query.Include(p => p.Basket).ThenInclude(b => b.Products);
            specification.OrderBy = query => query.OrderByDescending(p => p.Age);
            var res3 = await _readRepo.GetAsync(specification);
            Expression<Func<Person, PersonDto>> selectExpression = person => new PersonDto
            {
                Name = person.Name,
                Id = person.Id,
                Age = person.Age,
                BasketDtos = person.Basket.Select(basket => new BasketDto
                {
                    Id = basket.Id,
                    Description = basket.Description,
                    PersonId = basket.PersonId,
                    ProductDtos = basket.Products.Select(product => new ProductDto
                    {
                        Id = product.Id,
                        BasketId = product.BasketId,
                        ProductDescription = product.ProductDescription
                    }).ToList()
                }).ToList()
            };
            var persons3 = await _readRepo.GetListAsync(specification, selectExpression);



            PaginationSpecification<Person> paginationSpecification = new()
            {
                Includes = query => query.Include(p => p.Basket).ThenInclude(b => b.Products),
                OrderBy = query => query.OrderByDescending(p => p.Age),
                PageIndex = 2,
                PageSize = 5
            };
            paginationSpecification.Conditions.Add(p => p.Age < 100);
            Expression<Func<Person, PersonDto>> selectExpression2 = person => new PersonDto
            {
                Name = person.Name,
                Id = person.Id,
                Age = person.Age,
                BasketDtos = person.Basket.Select(basket => new BasketDto
                {
                    Id = basket.Id,
                    Description = basket.Description,
                    PersonId = basket.PersonId,
                    ProductDtos = basket.Products.Select(product => new ProductDto
                    {
                        Id = product.Id,
                        BasketId = product.BasketId,
                        ProductDescription = product.ProductDescription
                    }).ToList()
                }).ToList()
            };
            var persons4 = await _readRepo.GetListAsync(paginationSpecification, selectExpression);
        }

        [Test]
        public async Task unitofwork_any_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            bool any1 = await _readRepo.AnyAsync(p => p.Age < 50, true);

            List<Person> persons1 = new();
            bool any2 = _readRepo.Any(out persons1, p => p.Age < 100, p => p.Include(b => b.Basket).ThenInclude(p => p.Products), true);


            Specification<Person> specification = new() { Skip = 0, Take = 5 };
            specification.Conditions.Add(p => p.Age < 100);
            specification.Includes = query => query.Include(p => p.Basket).ThenInclude(b => b.Products);
            specification.OrderBy = query => query.OrderByDescending(p => p.Age);
            specification.AsNoTracking = true;
            Expression<Func<Person, PersonDto>> selectExpression = person => new PersonDto
            {
                Name = person.Name,
                Id = person.Id,
                Age = person.Age,
                BasketDtos = person.Basket.Select(basket => new BasketDto
                {
                    Id = basket.Id,
                    Description = basket.Description,
                    PersonId = basket.PersonId,
                    ProductDtos = basket.Products.Select(product => new ProductDto
                    {
                        Id = product.Id,
                        BasketId = product.BasketId,
                        ProductDescription = product.ProductDescription
                    }).ToList()
                }).ToList()
            };
            List<PersonDto> personDto2 = new();
            bool any3 = _readRepo.Any(out personDto2, specification, selectExpression);

        }

        [Test]
        public async Task unitofwork_count_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            int count1 = await _readRepo.CountAsync();

            List<Expression<Func<Person, bool>>> filter = new();
            filter.Add(p => p.Age > 50);
            filter.Add(p => p.Age < 150);
            filter.Add(p => p.Name != null);

            int count2 = await _readRepo.CountAsync(filter);
        }

        [Test]
        public async Task unitofwork_table_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            var persons = await _readRepo.Table.Set<Person>().ToListAsync();
        }

        [Test]
        public async Task unitofwork_getbyid_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            Person person1 = await _readRepo.GetByIdAsync(7);


            Expression<Func<Person, PersonDto>> selectExpression = person => new PersonDto
            {
                Name = person.Name,
                Id = person.Id,
                Age = person.Age,
                BasketDtos = person.Basket.Select(basket => new BasketDto
                {
                    Id = basket.Id,
                    Description = basket.Description,
                    PersonId = basket.PersonId,
                    ProductDtos = basket.Products.Select(product => new ProductDto
                    {
                        Id = product.Id,
                        BasketId = product.BasketId,
                        ProductDescription = product.ProductDescription
                    }).ToList()
                }).ToList()
            };

            PersonDto person3 = await _readRepo.GetByIdAsync<PersonDto>(8, selectExpression, true);
        }

        [Test]
        public async Task unitofwork_executequery_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            string query1 = @"
                    SELECT * FROM Persons";

            //string query1 = @"SELECT * FROM ""Person""";

            var person1 = await _readRepo.ExecuteQueryAsync(query1);

            string query2 = @"
                    SELECT p.Id, p.Name, p.Age
                    FROM Persons p
                    WHERE p.Age BETWEEN {0} AND {1}";

            //string query2 = @"
            //    SELECT p.""Id"", p.""Name"", p.""Age""
            //    FROM ""Person"" p
            //    WHERE p.""Age"" BETWEEN @p0 AND @p1";


            int minAge = 1;
            int maxAge = 50;

            var person2 = await _readRepo.ExecuteQueryAsync(query2, minAge, maxAge);
        }

        [Test]
        public async Task unitofwork_getqueryable_test()
        {
            var _sp = BuildServiceProvider();
            var _unitOfWork = GetUnitOfWork<Person>(_sp);
            var _readRepo = _unitOfWork.GetDbReadRepository();

            var persons = _readRepo.GetQueryable();
            var persons2 = await persons.ToListAsync();
        }

    }
}