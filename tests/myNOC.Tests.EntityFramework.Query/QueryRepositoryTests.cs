using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using myNOC.EntityFramework.Query;
using NSubstitute;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace myNOC.Tests.EntityFramework.Query
{
	[TestClass]
	public class QueryRepositoryTests
	{
		private QueryContext _queryContext = default!;
		private TestContext _testDbContext = default!;
		private QueryRepository _queryRepository = default!;
		private SqliteConnection _connection = default!;
		private CancellationInterceptor _interceptor = default!;

		[TestInitialize]
		public void Initialize()
		{
			_connection = new SqliteConnection("Data Source=:memory:");
			_connection.Open();
			_interceptor = new CancellationInterceptor();
			var options = new DbContextOptionsBuilder<TestContext>().UseSqlite(_connection).AddInterceptors(_interceptor).Options;
			_testDbContext = new TestContext(options);
			_testDbContext.Database.EnsureCreated();
			_testDbContext.TestEntities.Add(new TestEntity());
			_testDbContext.TestEntities.Add(new TestEntity());
			_testDbContext.TestEntities.Add(new TestEntity());
			_testDbContext.SaveChanges();

			_queryContext = Substitute.ForPartsOf<QueryContext>();
			_queryContext.GetContext().Returns(_testDbContext);

			_queryRepository = Substitute.ForPartsOf<QueryRepository>(_queryContext);
		}

		[TestCleanup]
		public void Cleanup()
		{
			_testDbContext?.Dispose();
			_connection?.Dispose();
		}

		[TestMethod]
		public async Task Query_RunAIQueryList_ReturnsIEnumerable()
		{
			//	Assemble
			var query = new TestEntitiesGetAll();

			//	Act
			var result = await _queryRepository.Query(query);

			//	Assert
			Assert.AreEqual(3, result.Count());
		}

		[TestMethod]
		public async Task Query_RunAIQueryScalar_ReturnsInt()
		{
			//	Assemble
			var query = new TestEntitiesCount();

			//	Act
			var result = await _queryRepository.Query(query);

			//	Assert
			Assert.AreEqual(3, result);
		}

		[TestMethod]
		public async Task Query_List_ForwardsCancellationTokenToDatabase()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await _queryRepository.Query(new TestEntitiesGetAll(), cancellation.Token);

			Assert.AreEqual(3, result.Count());
			Assert.AreEqual(cancellation.Token, _interceptor.LastToken);
		}

		[TestMethod]
		public async Task Query_Scalar_ForwardsCancellationTokenToDatabase()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await _queryRepository.Query(new TestEntitiesCount(), cancellation.Token);

			Assert.AreEqual(3, result);
			Assert.AreEqual(cancellation.Token, _interceptor.LastToken);
		}

		[TestMethod]
		public async Task Query_List_PreCanceledToken_DoesNotBuildQuery()
		{
			var query = Substitute.For<IQueryList<TestModel>>();
			var token = new CancellationToken(true);

			await Assert.ThrowsAsync<OperationCanceledException>(() => _queryRepository.Query(query, token));
			query.DidNotReceive().Query(Arg.Any<IQueryContext>());
		}

		[TestMethod]
		public async Task Query_Scalar_PreCanceledToken_DoesNotExecuteQuery()
		{
			var query = Substitute.For<IQueryScalar<int>>();
			var token = new CancellationToken(true);

			await Assert.ThrowsAsync<OperationCanceledException>(() => _queryRepository.Query(query, token));
			await query.DidNotReceive().GetScalar(Arg.Any<IQueryContext>(), Arg.Any<CancellationToken>());
		}

		[TestMethod]
		public async Task Query_List_CanceledDuringExecution_StopsDatabaseCommand()
		{
			using var cancellation = new CancellationTokenSource();
			_interceptor.CancelOnRead = cancellation;

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				_queryRepository.Query(new TestEntitiesGetAll(), cancellation.Token));
			Assert.AreEqual(cancellation.Token, _interceptor.LastToken);
		}

		[TestMethod]
		public async Task Query_Scalar_CanceledDuringExecution_StopsDatabaseCommand()
		{
			using var cancellation = new CancellationTokenSource();
			_interceptor.CancelOnRead = cancellation;

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				_queryRepository.Query(new TestEntitiesCount(), cancellation.Token));
			Assert.AreEqual(cancellation.Token, _interceptor.LastToken);
		}

		[TestMethod]
		public async Task Query_LegacyScalar_StillExecutesWithCancellationOverload()
		{
			using var cancellation = new CancellationTokenSource();

			var result = await _queryRepository.Query(new LegacyEntitiesCount(), cancellation.Token);

			Assert.AreEqual(3, result);
		}

		public class TestModel { public int Id { get; set; } }

		public class TestEntity
		{
			[Key]
			public int Id { get; set; }
		}

		public class TestContext : DbContext
		{
			public DbSet<TestEntity> TestEntities { get; set; } = default!;

			public TestContext(DbContextOptions options) : base(options) { }
		}

		internal class TestEntitiesGetAll : IQueryList<TestModel>
		{
			public IQueryable<TestModel> Query(IQueryContext context)
			{
				return from te in context.Set<TestEntity>()
					   select new TestModel { Id = te.Id };
			}
		}

		internal class TestEntitiesCount : IQueryScalar<int>
		{
			public Task<int> GetScalar(IQueryContext context)
			{
				return GetScalar(context, CancellationToken.None);
			}

			public Task<int> GetScalar(IQueryContext context, CancellationToken cancellationToken)
			{
				return context.Set<TestEntity>().CountAsync(cancellationToken);
			}
		}

		internal class LegacyEntitiesCount : IQueryScalar<int>
		{
			public Task<int> GetScalar(IQueryContext context)
			{
				return context.Set<TestEntity>().CountAsync();
			}
		}

		private class CancellationInterceptor : DbCommandInterceptor
		{
			public CancellationToken LastToken { get; private set; }
			public CancellationTokenSource? CancelOnRead { get; set; }

			public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
				DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
				CancellationToken cancellationToken = default)
			{
				LastToken = cancellationToken;
				CancelOnRead?.Cancel();
				cancellationToken.ThrowIfCancellationRequested();
				return new ValueTask<InterceptionResult<DbDataReader>>(result);
			}
		}
	}
}
