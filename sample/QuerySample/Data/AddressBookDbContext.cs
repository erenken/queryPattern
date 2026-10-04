using Microsoft.EntityFrameworkCore;
using QuerySample.Entities;

namespace QuerySample.Data
{
	public class AddressBookDbContext : DbContext
	{
		public DbSet<ContactEntity> Contacts { get; set; } = default!;

		public AddressBookDbContext(DbContextOptions<AddressBookDbContext> options) : base(options)
		{
		}
	}
}
