using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using myNOC.EntityFramework.Query;
using QuerySample.Data;
using QuerySample.Entities;
using QuerySample.Queries;

namespace myNOC.Tests.EntityFramework.Query
{
	[TestClass]
	public class SampleQueryTests
	{
		private SqliteConnection _connection = default!;
		private AddressBookDbContext _dbContext = default!;
		private IQueryRepository _repository = default!;

		[TestInitialize]
		public async Task Initialize()
		{
			_connection = new SqliteConnection("Data Source=:memory:");
			await _connection.OpenAsync();
			var options = new DbContextOptionsBuilder<AddressBookDbContext>().UseSqlite(_connection).Options;
			_dbContext = new AddressBookDbContext(options);
			await _dbContext.Database.EnsureCreatedAsync();
			_dbContext.Contacts.AddRange(
				new ContactEntity { Id = 1, Name = "Abby" },
				new ContactEntity { Id = 2, Name = "Bob" },
				new ContactEntity { Id = 3, Name = "Charlie" },
				new ContactEntity { Id = 4, Name = "David" });
			await _dbContext.SaveChangesAsync();
			_repository = new AddressBookContextRepository(new AddressBookContext(_dbContext));
		}

		[TestCleanup]
		public void Cleanup()
		{
			_dbContext?.Dispose();
			_connection?.Dispose();
		}

		[TestMethod]
		[DataRow("a")]
		[DataRow("A")]
		public async Task ContactNameContains_TranslatesToSqlAndMatchesIgnoringAsciiCase(string namePart)
		{
			var query = new ContactNameContains(namePart);
			var sql = query.Query(new AddressBookContext(_dbContext)).ToQueryString();
			var result = (await _repository.Query(query)).ToList();

			StringAssert.Contains(sql, "WHERE");
			CollectionAssert.AreEquivalent(new[] { "Abby", "Charlie", "David" }, result.Select(contact => contact.Name).ToArray());
			Assert.IsTrue(result.All(contact => contact.DisplayName == $"{contact.Id} - {contact.Name}"));
		}

		[TestMethod]
		public async Task ContactNameContains_StringComparisonOverload_IsRejectedBySqlite()
		{
			var query = _dbContext.Contacts.Where(contact =>
				contact.Name.Contains("a", StringComparison.InvariantCultureIgnoreCase));

			await Assert.ThrowsAsync<InvalidOperationException>(() => query.ToListAsync());
		}

		[TestMethod]
		public async Task ContactNameContains_NoMatches_ReturnsEmptyList()
		{
			var result = await _repository.Query(new ContactNameContains("missing"));

			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task ContactGetIdByName_TranslatesToSqlAndMatchesIgnoringAsciiCase()
		{
			using var cancellation = new CancellationTokenSource();

			var result = await _repository.Query(new ContactGetIdByName("bOB"), cancellation.Token);

			Assert.AreEqual(2, result);
		}

		[TestMethod]
		public async Task ContactGetIdByName_NoMatches_ReturnsDefaultId()
		{
			var result = await _repository.Query(new ContactGetIdByName("missing"));

			Assert.AreEqual(0, result);
		}
	}
}
