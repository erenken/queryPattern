using Microsoft.Extensions.DependencyInjection;
using myNOC.EntityFramework.Query;
using myNOC.EntityFramework.Query.Extensions;
using QuerySample.Data;

namespace myNOC.Tests.EntityFramework.Query.Extensions
{
	[TestClass]
	public class IServiceCollectionExtensionsTests
	{
		[TestMethod]
		public void AddQueryPattern_ScanAppDomainForClassesThatInheritFromIQueryContextOrIQueryRepository_ScopedAndRegister()
		{
			//	Assemble
			var services = new ServiceCollection();

			//	Act
			var results = services.AddQueryPattern();

			//	Assert
			Assert.IsNotNull(results);
			Assert.IsInstanceOfType<IServiceCollection>(results);

			Assert.IsNotNull(results.FirstOrDefault(x => x.ImplementationType == typeof(TestQueryContext)
				&& x.ServiceType == typeof(IQueryContext)
				&& x.Lifetime == ServiceLifetime.Scoped));

			Assert.IsNotNull(results.FirstOrDefault(x => x.ImplementationType == typeof(TestQueryRepository)
				&& x.ServiceType == typeof(IQueryRepository)
				&& x.Lifetime == ServiceLifetime.Scoped));
		}

		[TestMethod]
		public void AddQueryPattern_ExplicitAssembly_DiscoversInheritedInterfacesOnlyInSelectedAssembly()
		{
			var services = new ServiceCollection();

			services.AddQueryPattern(typeof(IServiceCollectionExtensionsTests).Assembly);

			Assert.IsTrue(services.Any(service => service.ServiceType == typeof(IDerivedQueryContext)
				&& service.ImplementationType == typeof(DerivedQueryContext)));
			Assert.IsFalse(services.Any(service => service.ImplementationType == typeof(AddressBookContext)));
		}

		[TestMethod]
		public void AddQueryPattern_Default_DiscoversImplementationsAcrossLoadedAssemblies()
		{
			_ = typeof(AddressBookContext).Assembly;
			var services = new ServiceCollection();

			services.AddQueryPattern();

			Assert.IsTrue(services.Any(service => service.ImplementationType == typeof(AddressBookContext)));
			Assert.IsTrue(services.Any(service => service.ImplementationType == typeof(TestQueryContext)));
		}

		[TestMethod]
		public void AddQueryPattern_RepeatedCallsAndAssemblies_DoNotDuplicateRegistrations()
		{
			var services = new ServiceCollection();
			var assembly = typeof(IServiceCollectionExtensionsTests).Assembly;

			services.AddQueryPattern(assembly, assembly);
			var count = services.Count;
			services.AddQueryPattern(assembly);

			Assert.AreEqual(count, services.Count);
			Assert.AreEqual(1, services.Count(service => service.ServiceType == typeof(IQueryContext)
				&& service.ImplementationType == typeof(DerivedQueryContext)));
		}

		[TestMethod]
		public void AddQueryPattern_OpenGenericImplementations_AreNotRegistered()
		{
			var services = new ServiceCollection();

			services.AddQueryPattern(typeof(IServiceCollectionExtensionsTests).Assembly);

			Assert.IsFalse(services.Any(service => service.ImplementationType == typeof(GenericQueryContext<>)));
		}

		[TestMethod]
		public async Task LegacyRepository_DefaultCancellationOverloads_DelegateToExistingMethods()
		{
			IQueryRepository repository = new LegacyRepository();
			using var cancellation = new CancellationTokenSource();

			var scalar = await repository.Query<int>(null!, cancellation.Token);
			var list = await repository.Query((IQueryList<object>)null!, cancellation.Token);

			Assert.AreEqual(42, scalar);
			Assert.AreEqual(0, list.Count());
		}

		[TestMethod]
		public async Task LegacyRepository_DefaultCancellationOverloads_RemainCompatible()
		{
			IQueryRepository repository = new TestQueryRepository();
			var token = new CancellationToken(true);

			await Assert.ThrowsAsync<OperationCanceledException>(() => repository.Query<int>(null!, token));
			await Assert.ThrowsAsync<OperationCanceledException>(() => repository.Query((IQueryList<object>)null!, token));
		}

		internal interface IDerivedQueryContext : IQueryContext { }
		internal class DerivedQueryContext : TestQueryContext, IDerivedQueryContext { }
		internal class GenericQueryContext<TEntity> : TestQueryContext { }

		internal class LegacyRepository : IQueryRepository
		{
			public Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query) where TModel : class
			{
				return Task.FromResult<IEnumerable<TModel>>(Array.Empty<TModel>());
			}

			public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query)
			{
				return Task.FromResult((TReturn?)(object)42);
			}
		}

		internal class TestQueryContext : IQueryContext
		{
			public IQueryable<TEntity> Set<TEntity>() where TEntity : class
			{
				throw new NotImplementedException();
			}
		}

		internal class TestQueryRepository : IQueryRepository
		{
			public Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query) where TModel : class
			{
				throw new NotImplementedException();
			}

			public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query)
			{
				throw new NotImplementedException();
			}
		}
	}
}
