using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using myNOC.EntityFramework.Query;
using myNOC.EntityFramework.Query.Extensions;
using QuerySample.Data;
using QuerySample.Entities;
using QuerySample.Queries;

namespace myNOC.Tests.EntityFramework.Query
{
	[TestClass]
	public class MultipleContextTests
	{
		[TestMethod]
		public async Task AddQueryPattern_MultipleDbContexts_RoutesQueriesAndDisposesBothContexts()
		{
			var services = new ServiceCollection();
			services.AddDbContext<AddressBookDbContext>(options => options.UseSqlite("Data Source=:memory:"));
			services.AddDbContext<SecondDbContext>(options => options.UseSqlite("Data Source=:memory:"));
			services.AddQueryPattern(typeof(AddressBookContext).Assembly, typeof(MultipleContextTests).Assembly);
			using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
			AddressBookDbContext firstDbContext;
			SecondDbContext secondDbContext;

			using (var scope = provider.CreateScope())
			{
				firstDbContext = scope.ServiceProvider.GetRequiredService<AddressBookDbContext>();
				secondDbContext = scope.ServiceProvider.GetRequiredService<SecondDbContext>();
				await Seed(firstDbContext, "First database");
				await Seed(secondDbContext, "Second database");
				var firstRepository = scope.ServiceProvider.GetRequiredService<IAddressBookContextRepository>();
				var secondRepository = scope.ServiceProvider.GetRequiredService<ISecondRepository>();

				var firstResult = await firstRepository.Query(new ContactNameContains("database"));
				var secondResult = await secondRepository.Query(new ContactNameContains("database"));

				Assert.AreEqual("First database", firstResult.Single().Name);
				Assert.AreEqual("Second database", secondResult.Single().Name);
				Assert.AreSame(firstDbContext, scope.ServiceProvider.GetRequiredService<AddressBookDbContext>());
				Assert.AreSame(secondDbContext, scope.ServiceProvider.GetRequiredService<SecondDbContext>());
				Assert.AreSame(firstRepository, scope.ServiceProvider.GetRequiredService<IAddressBookContextRepository>());
			}

			Assert.Throws<ObjectDisposedException>(() => firstDbContext.Contacts.Count());
			Assert.Throws<ObjectDisposedException>(() => secondDbContext.Contacts.Count());

			using var nextScope = provider.CreateScope();
			Assert.AreNotSame(firstDbContext, nextScope.ServiceProvider.GetRequiredService<AddressBookDbContext>());
			Assert.AreNotSame(secondDbContext, nextScope.ServiceProvider.GetRequiredService<SecondDbContext>());
		}

		private static async Task Seed(DbContext context, string name)
		{
			await context.Database.OpenConnectionAsync();
			await context.Database.EnsureCreatedAsync();
			context.Set<ContactEntity>().Add(new ContactEntity { Id = 1, Name = name });
			await context.SaveChangesAsync();
		}

		public interface ISecondContext : IQueryContext { }
		public interface ISecondRepository : IQueryRepository { }

		public class SecondDbContext : DbContext
		{
			public SecondDbContext(DbContextOptions<SecondDbContext> options) : base(options) { }

			public DbSet<ContactEntity> Contacts { get; set; } = default!;
		}

		public class SecondQueryContext : QueryContext, ISecondContext
		{
			private readonly SecondDbContext _context;

			public SecondQueryContext(SecondDbContext context)
			{
				_context = context;
			}

			public override DbContext GetContext()
			{
				return _context;
			}
		}

		public class SecondQueryRepository : QueryRepository, ISecondRepository
		{
			public SecondQueryRepository(ISecondContext context) : base(context) { }
		}
	}
}
