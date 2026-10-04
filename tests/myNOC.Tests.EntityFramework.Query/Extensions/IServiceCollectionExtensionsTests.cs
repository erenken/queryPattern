using Microsoft.Extensions.DependencyInjection;
using myNOC.EntityFramework.Query;
using myNOC.EntityFramework.Query.Extensions;

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
