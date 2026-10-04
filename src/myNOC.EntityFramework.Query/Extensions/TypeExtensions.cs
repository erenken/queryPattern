namespace myNOC.EntityFramework.Query.Extensions
{
	public static class TypeExtensions
	{
		public static List<TypeInterfaces> CanImplement(this IEnumerable<Type> types, Type interfaceType)
		{
			var typeInterfaces = types.Where(x => !x.IsAbstract
				&& !x.IsInterface
				&& !x.ContainsGenericParameters
				&& x.IsAssignableTo(interfaceType))
				.Select(x => new TypeInterfaces(x)).ToList();

			foreach(var ti in typeInterfaces)
				GetServiceInterfaces(ti, interfaceType);

			return typeInterfaces;
		}

		private static void GetServiceInterfaces(TypeInterfaces typeInterfaces, Type interfaceType)
		{
			foreach(var it in GetServiceInterfaces(typeInterfaces.Type))
			{
				if (it.IsAssignableTo(interfaceType))
					typeInterfaces.Interfaces.Add(it);
			}
		}

		private static IEnumerable<Type> GetServiceInterfaces(Type type)
		{
			foreach(var it in type.GetInterfaces())
			{
				if (it == typeof(IDisposable))
					continue;

				yield return it;
			}

		}
	}
}
