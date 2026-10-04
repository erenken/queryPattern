using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using myNOC.EntityFramework.Query.Extensions;
using QuerySample.Data;
using QuerySample.Entities;
using QuerySample.Queries;

IServiceCollection services = new ServiceCollection();
services.AddSingleton(_ =>
{
	var connection = new SqliteConnection("Data Source=:memory:");
	connection.Open();
	return connection;
});
services.AddDbContext<AddressBookDbContext>((provider, options) =>
	options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
services.AddQueryPattern();

using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
using var scope = provider.CreateScope();
await SeedSampleData(scope.ServiceProvider.GetRequiredService<AddressBookDbContext>());

var queryRepo = scope.ServiceProvider.GetRequiredService<IAddressBookContextRepository>();
var result = await queryRepo.Query(new ContactGetAll());
DisplayResults("Return All Contacts", result);

Console.WriteLine();

result = await queryRepo.Query(new ContactNameContains("a"));
DisplayResults("Contacts Where Name Contain 'a'", result);

Console.WriteLine();

var id = await queryRepo.Query(new ContactGetIdByName("Bob"));
Console.WriteLine($"Bob's Id is: {id}");

static async Task SeedSampleData(AddressBookDbContext addressBook)
{
	await addressBook.Database.EnsureCreatedAsync();
	addressBook.Add(new ContactEntity { Id = 1, Name = "Abby" });
	addressBook.Add(new ContactEntity { Id = 2, Name = "Bob" });
	addressBook.Add(new ContactEntity { Id = 3, Name = "Charlie" });
	addressBook.Add(new ContactEntity { Id = 4, Name = "David" });
	await addressBook.SaveChangesAsync();
}

static void DisplayResults(string title, IEnumerable<QuerySample.Models.ContactModel> result)
{
	Console.WriteLine(title);
	foreach (var item in result)
		Console.WriteLine(item.DisplayName);
}
