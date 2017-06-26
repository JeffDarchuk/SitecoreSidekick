using System;
using FluentAssertions;
using SitecoreSidekick.Shared.IoC;
using Xunit;

namespace SitecoreSidekick.Shared.UnitTests.IoC
{
	public class ContainerTests
	{
		public ContainerTests()
		{
			Container.Clear();
		}

		[Fact]
		public void Register_ValidRegistration_ResolvesNewInstance()
		{
			Container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = Container.Resolve<IMyClass>();

			myClassInstance.IsCreated.Should().BeTrue();
		}

		[Fact]
		public void Register_MultipleRegistrations_ResolvesLastRegistration()
		{
			Container.Register<IMyClass, MyClass>();
			Container.Register<IMyClass, MyOtherClass>();

			IMyClass myClassInstance = Container.Resolve<IMyClass>();

			myClassInstance.GetType().ShouldBeEquivalentTo(typeof(MyOtherClass));
		}

		[Fact]
		public void Resolve_MultipleResolutions_ReturnsSameInstance()
		{
			int expectedCounter = 10;
			Container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = Container.Resolve<IMyClass>();
			myClassInstance.Counter = expectedCounter;

			IMyClass myOtherClassInstance = Container.Resolve<IMyClass>();

			myOtherClassInstance.Counter.ShouldBeEquivalentTo(expectedCounter);
		}

		[Fact]
		public void Resolve_MultipleResolutions_SameObject()
		{
			int expectedCounter = 10;
			Container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = Container.Resolve<IMyClass>();
			IMyClass myOtherClassInstance = Container.Resolve<IMyClass>();
			
			myClassInstance.Counter = expectedCounter;
			
			myOtherClassInstance.Counter.ShouldBeEquivalentTo(expectedCounter);
		}

		[Fact]
		public void Resolve_DependencyOnAnotherClass_Resolves()
		{
			Container.Register<IMyClass, MyClass>();
			Container.Register<IMyDependentClass, MyDependentClass>();
			
			IMyClass myClassInstance = Container.Resolve<IMyClass>();
			myClassInstance.Counter++;
			IMyDependentClass myDependentClassInstance = Container.Resolve<IMyDependentClass>();

			myDependentClassInstance.MyDoubler.ShouldBeEquivalentTo(2);
		}

		[Fact]
		public void Resolve_ClassNotRegistered_ThrowsException()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				Container.Resolve<IMyClass>();
			});
		}

		[Fact]
		public void Resolve_ClassFailsToInitialize_ThrowsException()
		{
			Assert.Throws<NullReferenceException>(() =>
			{
				Container.Register<IMyBrokenClass, MyBrokenClass>();
				Container.Resolve<IMyBrokenClass>();
			});
		}

		[Fact]
		public void Clear_RegisteredClassHasDispose_InvokesDispose()
		{
			bool didDispose = false;
			Container.Register<IMyDisposableClass, MyDisposableClass>();
			IMyDisposableClass myDisposableClassInstance = Container.Resolve<IMyDisposableClass>();
			myDisposableClassInstance.OnDispose = () =>
			{
				didDispose = true;
			};

			Container.Clear();

			didDispose.Should().BeTrue();
		}

		#region Test Interfaces and Classes

		public interface IMyClass
		{
			bool IsCreated { get; }
			int Counter { get; set; }
		}

		public class MyClass : IMyClass
		{
			public bool IsCreated { get; }
			public int Counter { get; set; }

			public MyClass()
			{
				IsCreated = true;
			}
		}

		public class MyOtherClass : IMyClass
		{
			public bool IsCreated { get; }
			public int Counter { get; set; }

			public MyOtherClass()
			{
				IsCreated = true;
				Counter = 10;
			}
		}

		public interface IMyDependentClass
		{
			int MyDoubler { get; }
		}

		public class MyDependentClass : IMyDependentClass
		{
			private readonly IMyClass _myClass;
			public MyDependentClass()
			{
				_myClass = Container.Resolve<IMyClass>();
			}

			public int MyDoubler => _myClass.Counter * 2;
		}

		public interface IMyBrokenClass
		{
		}

		public class MyBrokenClass : IMyBrokenClass
		{
			public MyBrokenClass()
			{
				throw new Exception("I am a bad constructor");
			}
		}

		public interface IMyDisposableClass
		{
			Action OnDispose { get; set; }
		}

		public class MyDisposableClass : IMyDisposableClass, IDisposable
		{
			public Action OnDispose { get; set; }

			public void Dispose()
			{
				OnDispose?.Invoke();
			}
		}

		#endregion
	}
}
