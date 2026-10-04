# myNOC.EntityFramework.Query

## Installation and support

```shell
dotnet add package myNOC.EntityFramework.Query
```

Supports **.NET 8.0 and .NET 10.0** (LTS), using EF Core 8 and EF Core 10
respectively. Match your application's Entity Framework packages to its target.
See the [repository build instructions](https://github.com/erenken/queryPattern#local-build-and-validation)
to build the library/sample and run tests against both frameworks.

## Versions and publishing

Stable packages are released from `main` only after all targets build without
warnings/errors, tests pass, and packaged Source Link checks succeed. GitVersion
automatically stamps package, assembly, file and informational versions. Work
branches use preview versions for validation, not public publishing. Maintainers
request minor/major releases using `+semver: minor` / `+semver: major` in commit
or merge messages. Each release has a matching GitHub `v<version>` tag.
Publishing uses GitHub OIDC through `NuGet/login` and a package-scoped NuGet
Trusted Publishing policy, not a permanent API key. See the
[release setup](https://github.com/erenken/queryPattern#ci-and-trusted-publishing).

## Source Link debugging

The `.snupkg` includes portable PDBs for .NET 8 and .NET 10. Source Link maps
to the exact build commit and CI verifies matching source downloads/checksums.
In Visual Studio, add `https://symbols.nuget.org/download/symbols` under
**Tools > Options > Debugging > Symbols**, enable **Source Link support** and
disable **Just My Code** to step into library methods. Symbols become available
after NuGet finishes indexing the symbol package.

## Overview

A library used to create EntityFramework queries.

The concept is to create a concrete class that contains query written for a specific job.  

This helps you keep your repositories focused on one entity.  You no longer need to think if you are violating the SOLID pattern by having a function `SecurityRoles` in your `Employee` repository that is using an `Employee` and `Security` entity.

The other big advantage is the ability to write unit test against your queries.  When I have written queries and written unit tests against them they would fully pass the unit tests, but them I would get runtime errors when it failed to convert to SQL.  Using this method you can easily use EntityFramework's `InMemoryDatabase` and mock up test data.  You can verify your complex queries will do what you expect them to do.

Instead you would have a `EmployeeSecurityRoles` concrete class that inherits from `IQueryList<>`.

## Sample Application

[QuerySample](https://github.com/erenken/queryPattern/tree/main/sample/QuerySample)

## Setup and Configuration

To use `myNOC.EntityFramework.Query` you will need to add it to your `IServiceCollection`.

```csharp
services.AddQueryPattern();
```

This will register any classes in the `AppDomain` that implements `IQueryContext` or `IQueryRepository`

## Setting Up `QueryContext`

You will need to setup a `QueryContext` for any `DbContext` you want the query pattern to use.  The query pattern needs the `DbContext` because your queries may need access to multiple entities.

I recommend you first create an interface for your context.

```csharp
public interface IAddressBookContext : IQueryContext { }
```

Then you need to create your context `AddressBookContext` that inherits from the abstract class `QueryContext`.

```csharp
public class AddressBookContext : QueryContext, IAddressBookContext
{
    private DbContext? _dbContext = null;
    public override DbContext GetContext()
    {
        if (_dbContext == null)
            _dbContext = new AddressBookDbContext();

        return _dbContext;
    }
}
```

This allows you to use any `DbContext` you want.  You could inject a `DbContext` you are already using in your application and return it in `GetContext()`.

## Setting Up `QueryRepository`

You will need a `QueryRepository` for your `DbContext`.  Again I recommend your first create an interface for your repository.  

```csharp
public interface IAddressBookContextRepository : IQueryRepository { }
```

Then you need to create your repository `AddressBookContextRepository` that inherits from the abstract class `QueryRepository`.

```csharp
public class AddressBookContextRepository : QueryRepository, IAddressBookContextRepository
{
    public AddressBookContextRepository(IAddressBookContext context) : base(context) { }
}
```

## Create Queries

There are two types of queries.  

* Lists
* Scalar

They return exactly what they say.  One returns a list and the other returns a scalar value.

### `IQueryList<>`

```csharp
public class ContactNameContains : IQueryList<ContactModel>
{
    private readonly string _namePart;

    public ContactNameContains(string namePart)
    {
        _namePart = namePart;
    }

    public IQueryable<ContactModel> Query(IQueryContext context)
    {
        var persons = context.Set<ContactEntity>();
        var query = from p in persons
                    where p.Name.Contains(_namePart, StringComparison.InvariantCultureIgnoreCase)
                    select new ContactModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = $"{p.Id} - {p.Name}"
                    };

        return query;
    }
}
```

Your criteria is passed into the constructor.

```csharp
public ContactNameContains(string namePart)
```

In the `Query` method you have access to your `IQueryContext` and in turn all of its entities.

```csharp
var persons = context.Set<ContactEntity>();
```

You can then use `persons` in your query.  You could just use the `context.Set<ContactEntity>()` in your query, but I find this cleaner.

You can then use the criteria that was passed into the constructor in your query.  
 
```csharp
where p.Name.Contains(_namePart, StringComparison.InvariantCultureIgnoreCase)
```

### `IQueryScalar<>`

```csharp
internal class ContactGetIdByName : IQueryScalar<int>
{
    private readonly string _name;

    public ContactGetIdByName(string name)
    {
        _name = name;
    }

    public async Task<int> GetScalar(IQueryContext context)
    {
        var persons = context.Set<ContactEntity>();
        var query = from p in persons
                    where p.Name.Contains(_name, StringComparison.InvariantCultureIgnoreCase)
                    select p.Id;

        return await query.FirstOrDefaultAsync();
    }
}
```

Just like `IQueryList<>` you pass in your criteria to the constructor.

## Running Queries

```csharp
IServiceCollection services = new ServiceCollection();
services.AddQueryPattern();

var provider = services.BuildServiceProvider();

var queryRepo = provider.GetRequiredService<IAddressBookContextRepository>();
```

You need to get an instance of your query repository `IAddressBookContextRepository`.

Once you have your `queryRepo` you can execute the query.

```csharp
var result = await queryRepo.Query(new ContactNameContains("a"));
```

`ContactNameContains` inherits from `IQueryList<ContactModel>` so the `Query` method will return `IEnumerable<ContactModel>` where the name contains an `a`.  

You run a scalar query the same way.

```csharp
var id = await queryRepo.Query(new ContactGetIdByName("Bob"));
```

`ContactGetIdByName` inherits from `IQueryScalar<int>` so the `Query` method will return an `int`.

## Testing

For testing in the sample application I used an `InMemoryDatabase`.

```csharp
public class AddressBookDbContext : DbContext
{
    public DbSet<ContactEntity> Contacts { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("AddressBook");
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
```

and seeded my data like so.

```csharp
static async Task SeedSampleData()
{
	var addressBook = new AddressBookDbContext();
	addressBook.Add(new ContactEntity { Id = 1, Name = "Abby" });
	addressBook.Add(new ContactEntity { Id = 2, Name = "Bob" });
	addressBook.Add(new ContactEntity { Id = 3, Name = "Charlie" });
	addressBook.Add(new ContactEntity { Id = 4, Name = "David" });
	await addressBook.SaveChangesAsync();
}
```
