using myNOC.EntityFramework.Query;
using QuerySample.Entities;
using QuerySample.Models;

namespace QuerySample.Queries
{
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
}
