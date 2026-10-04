using Microsoft.EntityFrameworkCore;

namespace myNOC.EntityFramework.Query
{
	public interface IQueryRepository
	{
		public Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query) where TModel : class;
		public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query);

		public Task<IEnumerable<TModel>> Query<TModel>(IQueryList<TModel> query, CancellationToken cancellationToken) where TModel : class
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Query(query);
		}

		public Task<TReturn?> Query<TReturn>(IQueryScalar<TReturn> query, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Query(query);
		}
	}
}
