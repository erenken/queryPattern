# myNOC.EntityFramework.Query

## Installation and support

```shell
dotnet add package myNOC.EntityFramework.Query
```

Register the EF Core provider used by your application separately. To follow the
SQLite examples, also install `Microsoft.EntityFrameworkCore.Sqlite`, using an
EF Core version matching your target framework and other EF packages.

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

A small implementation of the **query-object pattern** for Entity Framework Core.

Each concrete query object owns a read operation for a specific use case, including
its criteria and projection. This is not a classic specification containing only
reusable selection criteria. Package names and APIs such as `AddQueryPattern`
remain unchanged.

This keeps repositories focused on executing read operations rather than growing
a method for every screen or combining unrelated data requirements. A query
object can join multiple entities in its selected database context; for example,
an employee-security-roles query can own that specific projection without
expanding an existing employee-list query.

Query objects are also easy to test independently. Use a relational provider to
verify SQL translation as well as results. The sample and tests use SQLite
in-memory databases; EF Core's InMemory provider cannot validate SQL translation.
SQLite is supplementary coverage, not a substitute for testing against your
production database provider.

Instead you would have a `EmployeeSecurityRoles` concrete class that inherits from `IQueryList<>`.

## Sample Application

[QuerySample](https://github.com/erenken/queryPattern/tree/main/sample/QuerySample)

## Setup and Configuration

To use `myNOC.EntityFramework.Query` you will need to add it to your `IServiceCollection`.

```csharp
services.AddQueryPattern();
```

The default scans **all assemblies currently loaded in the `AppDomain`** and
registers concrete, closed classes implementing `IQueryContext` or
`IQueryRepository`, including inherited/custom query interfaces, as scoped services.
It is not restricted to the calling assembly or a single database. Repeated calls
do not duplicate the same service-interface/implementation pair, and multiple
implementations of an interface are retained for `IEnumerable<T>` resolution.

For explicit discovery boundaries, pass any number of assemblies:

```csharp
services.AddQueryPattern(typeof(AddressBookContext).Assembly,
                         typeof(SecurityContext).Assembly);
```

Assemblies are inspected once per call. Unloaded assemblies are not discovered
by the default overload; load plugins before calling it, or call the explicit
overload after loading each plugin. Reflection/type-load errors are not silently
ignored, so a missing dependency cannot quietly hide registrations.

This registers contexts and repositories, **not** `IQueryList<T>` or
`IQueryScalar<T>` objects: their constructor arguments are runtime query criteria,
so callers create them when executing a query. Neither overload registers a
`DbContext`; register each concrete database context separately using `AddDbContext`.

## Setting Up `QueryContext`

Set up a `QueryContext` for each `DbContext` used by the query-object pattern.
The context exposes entities for composing queries, while DI owns and disposes
the underlying scoped database context.

I recommend you first create an interface for your context.

```csharp
public interface IAddressBookContext : IQueryContext { }
```

Then you need to create your context `AddressBookContext` that inherits from the abstract class `QueryContext`.

```csharp
public class AddressBookContext : QueryContext, IAddressBookContext
{
    private readonly AddressBookDbContext _dbContext;

    public AddressBookContext(AddressBookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override DbContext GetContext()
    {
        return _dbContext;
    }
}
```

Inject the concrete context type, not a shared `DbContext` registration. Define
its typed options constructor like this:

```csharp
public class AddressBookDbContext : DbContext
{
    public AddressBookDbContext(DbContextOptions<AddressBookDbContext> options)
        : base(options) { }

    public DbSet<ContactEntity> Contacts { get; set; } = default!;
}
```

Register it with the provider appropriate to your app:

```csharp
services.AddDbContext<AddressBookDbContext>(options =>
    options.UseSqlite("Data Source=addressbook.db"));
services.AddQueryPattern();
```

Do not manually create or dispose this context inside `QueryContext`. Resolve
repositories inside a request scope, or an explicit scope in a console app.

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

## Multiple database contexts

Keep a distinct custom context interface and repository interface for each
database, and inject that custom interface into its repository. For example:

```csharp
public interface ISecurityContext : IQueryContext { }
public interface ISecurityRepository : IQueryRepository { }

public class SecurityContext : QueryContext, ISecurityContext
{
    private readonly SecurityDbContext _context;

    public SecurityContext(SecurityDbContext context)
    {
        _context = context;
    }

    public override DbContext GetContext() => _context;
}

public class SecurityRepository : QueryRepository, ISecurityRepository
{
    public SecurityRepository(ISecurityContext context) : base(context) { }
}

services.AddDbContext<AddressBookDbContext>(options =>
    options.UseSqlite("Data Source=addressbook.db"));
services.AddDbContext<SecurityDbContext>(options =>
    options.UseSqlite("Data Source=security.db"));
services.AddQueryPattern();
```

`IAddressBookContextRepository` uses `AddressBookDbContext`; `ISecurityRepository`
uses `SecurityDbContext`, even in the same scope. `SecurityDbContext` should accept
`DbContextOptions<SecurityDbContext>`, just as the address-book context accepts its
own typed options. DI disposes both contexts at scope end.

When multiple implementations exist, do not resolve bare `IQueryContext` or
`IQueryRepository` to choose a database: normal DI resolution selects the last
registration. Use the custom interfaces instead. Query objects execute against
the selected repository's context; one query does not perform a cross-database join.

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
        _namePart = namePart.ToUpperInvariant();
    }

    public IQueryable<ContactModel> Query(IQueryContext context)
    {
        var persons = context.Set<ContactEntity>();
        var query = from p in persons
                    where p.Name.ToUpper().Contains(_namePart)
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
where p.Name.ToUpper().Contains(_namePart)
```

### `IQueryScalar<>`

```csharp
public class ContactGetIdByName : IQueryScalar<int>
{
    private readonly string _name;

    public ContactGetIdByName(string name)
    {
        _name = name.ToUpperInvariant();
    }

    public Task<int> GetScalar(IQueryContext context)
    {
        return GetScalar(context, CancellationToken.None);
    }

    public async Task<int> GetScalar(IQueryContext context, CancellationToken cancellationToken)
    {
        var persons = context.Set<ContactEntity>();
        var query = from p in persons
                    where p.Name.ToUpper().Contains(_name)
                    select p.Id;

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
```

Just like `IQueryList<>` you pass in your criteria to the constructor.

## Running Queries

```csharp
IServiceCollection services = new ServiceCollection();
services.AddDbContext<AddressBookDbContext>(options =>
    options.UseSqlite("Data Source=addressbook.db"));
services.AddQueryPattern();

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true
});
using var scope = provider.CreateScope();

var queryRepo = scope.ServiceProvider.GetRequiredService<IAddressBookContextRepository>();
```

This example uses a SQLite file that must already have its schema and data
initialized. The sample's in-memory configuration and seeding are shown in the
testing section. Include `Microsoft.EntityFrameworkCore`,
`Microsoft.Extensions.DependencyInjection`, and
`myNOC.EntityFramework.Query.Extensions` namespaces for setup; the in-memory
example also uses `Microsoft.Data.Sqlite`.

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

The sample uses a SQLite in-memory database, with an open connection keeping the
database alive. DI creates and disposes the connection and scoped context:

```csharp
services.AddSingleton(_ =>
{
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    return connection;
});
services.AddDbContext<AddressBookDbContext>((provider, options) =>
    options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
services.AddQueryPattern();
```

and seeded my data like so.

```csharp
static async Task SeedSampleData(AddressBookDbContext addressBook)
{
	await addressBook.Database.EnsureCreatedAsync();
	addressBook.Add(new ContactEntity { Id = 1, Name = "Abby" });
	addressBook.Add(new ContactEntity { Id = 2, Name = "Bob" });
	addressBook.Add(new ContactEntity { Id = 3, Name = "Charlie" });
	addressBook.Add(new ContactEntity { Id = 4, Name = "David" });
	await addressBook.SaveChangesAsync();
}
```

Resolve the context from the scope and pass it into `SeedSampleData` before running
queries. Each test uses its own open SQLite connection to isolate database state.
Tests cover sample query translation/results, cancellation forwarding, and two
independent typed contexts with scope-owned disposal.

The sample normalizes search text with `ToUpperInvariant` and uses SQL `UPPER`
with `Contains`, avoiding the unsupported `StringComparison` overload. This
demonstrates case-insensitive ASCII matching in SQLite, not identical .NET
invariant-culture or Unicode behavior across database engines. For production,
choose collation/normalization appropriate to your provider and test it there;
applying `UPPER` to a column can also affect index usage.

## Cancellation and compatibility

Existing `Query(query)` and `GetScalar(context)` signatures remain available.
Pass a token to either repository overload for cancellation:

```csharp
var contacts = await queryRepo.Query(new ContactNameContains("a"), cancellationToken);
var id = await queryRepo.Query(new ContactGetIdByName("Bob"), cancellationToken);
```

`QueryRepository` rejects an already-canceled token before constructing or
executing a query. List execution forwards the token to `ToListAsync`; scalar
execution forwards it to `GetScalar(context, cancellationToken)`. Scalar query
implementations should override that overload and pass the token to every async
EF operation, as shown above.

Default interface implementations preserve compatibility for existing scalar
queries and custom `IQueryRepository` implementations. Those legacy fallbacks
only check cancellation before execution; they cannot cancel an in-flight
operation unless you implement the new overload and forward its token. Actual
in-flight cancellation support also depends on the database provider.
