using Base.Repository.Abstractions;
using Base.Repository.Helpers;
using Base.Repository.Options;
using EfCore.Repository.Concretes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class PackageSurfaceTests
    {
        private static readonly Dictionary<int, Version> PatchedEfCoreFloor = new()
        {
            [8] = new Version(8, 0, 33),
            [9] = new Version(9, 0, 22),
            [10] = new Version(10, 0, 14)
        };

        [Test]
        public void EfCoreDependency_MatchesRuntimeMajorVersion()
        {
            Version efCore = typeof(DbContext).Assembly.GetName().Version;

            Assert.That(efCore.Major, Is.EqualTo(Environment.Version.Major));
        }

        [Test]
        public void EfCoreDependency_IsAtLeastPatchedVersion()
        {
            string informational = typeof(DbContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Version efCore = Version.Parse(informational.Split('+', '-')[0]);

            Assert.That(efCore, Is.GreaterThanOrEqualTo(PatchedEfCoreFloor[Environment.Version.Major]));
        }

        [Test]
        public void BothPackages_ShareVersion2()
        {
            string repositoryPack = InformationalVersionOf(typeof(ReadRepository<>).Assembly);
            string baseRepositoryPack = InformationalVersionOf(typeof(IBaseReadRepository<>).Assembly);

            Assert.Multiple(() =>
            {
                Assert.That(repositoryPack, Is.EqualTo(baseRepositoryPack));
                Assert.That(Version.Parse(repositoryPack).Major, Is.GreaterThanOrEqualTo(2));
            });
        }

        [TestCase(typeof(BaseReadRepository<>))]
        [TestCase(typeof(BaseWriteRepository<>))]
        [TestCase(typeof(ReadRepository<>))]
        [TestCase(typeof(WriteRepository<>))]
        [TestCase(typeof(DbReadRepository<>))]
        [TestCase(typeof(DbWriteRepository<>))]
        [TestCase(typeof(Table))]
        [TestCase(typeof(UnitOfWork<>))]
        public void Context_IsNotExposedThroughPublicDbContextMember(Type type)
        {
            MemberInfo[] members = type.GetMember("_dbContext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            Assert.That(members, Is.Empty);
        }

        [Test]
        public void BaseReadRepository_HasNoUnparameterizedRawQueryOverload()
        {
            MethodInfo method = typeof(IBaseReadRepository<>).GetMethod("ExecuteQueryAsync", new[] { typeof(string) });

            Assert.That(method, Is.Null);
        }

        [Test]
        public void DatabaseOptions_HasNoRetrySettings()
        {
            Assert.Multiple(() =>
            {
                Assert.That(typeof(DatabaseOptions).GetProperty("RetryCount"), Is.Null);
                Assert.That(typeof(DatabaseOptions).GetProperty("IsRetry"), Is.Null);
            });
        }

        [Test]
        public void RepositoryPack_HasNoParameterValidationAttribute()
        {
            Type type = typeof(ReadRepository<>).Assembly.GetType("EfCore.Repository.Attributes.ParameterValidationAttribute");

            Assert.That(type, Is.Null);
        }

        [Test]
        public void ConfigurationHelpers_HasNoServiceCollectionOverload()
        {
            bool hasServiceCollectionOverload = typeof(ConfigurationHelpers)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(IServiceCollection));

            Assert.That(hasServiceCollectionOverload, Is.False);
        }

        private static string InformationalVersionOf(Assembly assembly)
            => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];
    }
}
