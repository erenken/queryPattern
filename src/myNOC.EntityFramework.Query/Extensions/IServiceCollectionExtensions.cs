using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace myNOC.EntityFramework.Query.Extensions
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddQueryPattern(this IServiceCollection services)
		{
			return services.AddQueryPattern(AppDomain.CurrentDomain.GetAssemblies());
		}

		public static IServiceCollection AddQueryPattern(this IServiceCollection services, params Assembly[] assemblies)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(assemblies);

			var types = assemblies.Distinct().SelectMany(assembly => assembly.GetTypes()).ToArray();
			var implementations = types.CanImplement(typeof(IQueryContext))
				.Concat(types.CanImplement(typeof(IQueryRepository)));

			foreach (var implementation in implementations)
				foreach (var serviceInterface in implementation.Interfaces)
					services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceInterface, implementation.Type));

			return services;
		}
	}
}
