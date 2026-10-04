using Microsoft.EntityFrameworkCore;
using myNOC.EntityFramework.Query;

namespace QuerySample.Data
{
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
}
