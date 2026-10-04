namespace myNOC.EntityFramework.Query
{
	public interface IQueryScalar<TReturn>
	{
		Task<TReturn?> GetScalar(IQueryContext context);

		Task<TReturn?> GetScalar(IQueryContext context, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return GetScalar(context);
		}
	}
}
