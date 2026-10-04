using Microsoft.EntityFrameworkCore;

namespace myNOC.EntityFramework.Query
{
	public abstract class QueryRepository : IQueryRepository
	{
		private readonly IQueryContext _context;

		public QueryRepository(IQueryContext context)
		{
			_context = context;
		}

		public Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query) where TModel : class
		{
			return Query(query, CancellationToken.None);
		}

		public async Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query, CancellationToken cancellationToken) where TModel : class
		{
			cancellationToken.ThrowIfCancellationRequested();
			var result = query.Query(_context);
			return await result.ToListAsync(cancellationToken);
		}

		public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query)
		{
			return Query(query, CancellationToken.None);
		}

		public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return query.GetScalar(_context, cancellationToken);
		}
	}
}
