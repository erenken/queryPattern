using Microsoft.EntityFrameworkCore;
using myNOC.EntityFramework.Query;
using QuerySample.Entities;

namespace QuerySample.Queries
{
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
}
