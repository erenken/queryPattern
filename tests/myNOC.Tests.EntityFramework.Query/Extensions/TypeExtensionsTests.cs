using myNOC.EntityFramework.Query.Extensions;
using System.Reflection;

namespace myNOC.Tests.EntityFramework.Query.Extensions
{
    [TestClass]
    public class TypeExtensionTests 
    {
		private IEnumerable<Type> _types = default!;

		[TestInitialize]
		public void Initialize()
		{
			_types = Assembly.GetExecutingAssembly().GetTypes();
		}

		[TestMethod]
		public void CanImplement_Types_ImplementsInterface_DirectInterface_ReturnsTypesThatInheritFromITestInterface2()
		{
			//	Arrange
			var implementedInterface = typeof(ITestInterface2);

			//	Act
			var results = _types.CanImplement(implementedInterface);

			//	Assert
			Assert.IsNotNull(results);
			Assert.AreEqual(1, results.Count());
			Assert.AreEqual(1, results[0].Interfaces.Count());
			Assert.AreEqual(implementedInterface, results[0].Interfaces[0]);
			Assert.AreEqual(typeof(TestClass3), results[0].Type);
			Assert.IsNull(results.FirstOrDefault(x => x.Type == typeof(TestClass4)));
		}

		[TestMethod]
        public void CanImplement_Types_ImplementsInterface_SupportsNestedInterfaces_ReturnsTypesThatInheritFromITestInterface()
        {
			//	Arrange
			var implementedInterface = typeof(ITestInterface);

			//	Act
			var results = _types.CanImplement(implementedInterface);

			//	Assert
			Assert.IsNotNull(results);
			Assert.AreEqual(3, results.Count());
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type == typeof(TestClass1)));
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type == typeof(TestClass2)));
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type == typeof(TestClass5)));
			Assert.IsNull(results.FirstOrDefault(x => x.Type == typeof(TestClass4)));
		}

		[TestMethod]
		public void CanImplement_Types_ImplementsInterface_NestedClassesRecursion()
		{
			//	Arrange
			var implementedInterface = typeof(ITestInterface3);

			//	Act
			var results = _types.CanImplement(implementedInterface);

			//	Assert
			Assert.IsNotNull(results);
			Assert.AreEqual(2, results.Count());
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type == typeof(TestClass6)));
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type == typeof(TestClass8)));
			Assert.IsTrue(results.All(result => result.Interfaces.Count == result.Interfaces.Distinct().Count()));
			Assert.IsNull(results.FirstOrDefault(x => x.Type == typeof(TestClass4)));
		}

		[TestMethod]
		public void CanImplement_Types_ImplementsInterface_SkipIDisposableInterfaceInGetInterfaces()
		{
			//	Arrange
			var implementedInterface = typeof(ITestInterface5);

			//	Act
			var results = _types.CanImplement(implementedInterface);

			//	Assert
			Assert.IsNotNull(results);
			Assert.AreEqual(1, results.Count());
			Assert.IsNotNull(results.FirstOrDefault(x => x.Type.Equals(typeof(TestClass7))));
			Assert.IsNull(results.FirstOrDefault(x => x.Type == typeof(TestClass4)));
		}

		internal interface ITestInterface { }
		internal interface ITestInterface2 { }
		internal interface ITestInterface3 { }
		internal interface ITestInterface4 : ITestInterface { }
		internal interface ITestInterface5 : IDisposable { }
		internal interface ITestInterface7 : ITestInterface3 { }

		internal class TestClass1 : ITestInterface { }
		internal class TestClass2 : ITestInterface { }
		internal class TestClass3 : ITestInterface2 { }
		internal class TestClass4 { }
		internal class TestClass5 : ITestInterface4 { }
		internal class TestClass6 : TestClass4, ITestInterface7 { }
		internal class TestClass7 : ITestInterface5
		{
			public void Dispose()
			{
				throw new NotImplementedException();
			}
		}

		internal class TestClass8 : TestClass6 { }
	}
}
