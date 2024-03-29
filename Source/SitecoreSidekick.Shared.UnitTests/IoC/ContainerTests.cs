﻿using FluentAssertions;
using Sidekick.Core.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sidekick.Core.Shared.UnitTests.IoC
{
	public class containerTests
	{
		[Fact]
		public void Register_ValidRegistrationViaTypes_ResolvesNewInstance()
		{
			Container container = new Container();
			container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = container.Resolve<IMyClass>();

			myClassInstance.IsCreated.Should().BeTrue();
		}

		[Fact]
		public void Register_ValidRegistrationWithTypeAndConcreteImplementation_ResolvesInstance()
		{
			int expectedCounter = 42;
			Container container = new Container();
			MyClass myConcreteClass = new MyClass { Counter = expectedCounter };

			container.Register(typeof(IMyClass), myConcreteClass);

			IMyClass myResolvedClass = container.Resolve<IMyClass>();

			myResolvedClass.Counter.Should().Be(expectedCounter);
		}

		[Fact]
		public void Register_ValidRegistrationWithConcreteImplementation_ResolvesInstance()
		{
			int expectedCounter = 42;
			Container container = new Container();
			MyClass myConcreteClass = new MyClass { Counter = expectedCounter };

			container.Register<IMyClass>(myConcreteClass);

			IMyClass myResolvedClass = container.Resolve<IMyClass>();

			myResolvedClass.Counter.Should().Be(expectedCounter);
		}

		[Fact]
		public void Register_MultipleRegistrations_ResolvesLastRegistration()
		{
			Container container = new Container();
			container.Register<IMyClass, MyClass>();
			container.Register<IMyClass, MyOtherClass>();

			IMyClass myClassInstance = container.Resolve<IMyClass>();

			myClassInstance.GetType().ShouldBeEquivalentTo(typeof(MyOtherClass));
		}

		[Fact]
		public void RegisterFactory_ResolvesDifferentInstances()
		{
			int instanceCount = 5;
			Container container = new Container();

			container.RegisterFactory<IMyClass>(args => new MyClass());

			List<IMyClass> myClassInstances = new List<IMyClass>();

			for (int idx = 0; idx < instanceCount; idx++)
			{
				IMyClass myClassInstance = container.Resolve<IMyClass>();
				myClassInstance.Counter = idx;
				myClassInstances.Add(myClassInstance);
			}

			myClassInstances.Count.Should().Be(instanceCount);

			for (int idx = 0; idx < instanceCount; idx++)
			{
				myClassInstances.Any(mci => mci.Counter == idx).Should().BeTrue();
			}
		}

		[Fact]
		public void Resolve_MultipleResolutions_ReturnsSameInstance()
		{
			Container container = new Container();
			int expectedCounter = 10;
			container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = container.Resolve<IMyClass>();
			myClassInstance.Counter = expectedCounter;

			IMyClass myOtherClassInstance = container.Resolve<IMyClass>();

			myOtherClassInstance.Counter.ShouldBeEquivalentTo(expectedCounter);
		}

		[Fact]
		public void Resolve_MultipleResolutions_SameObject()
		{
			Container container = new Container();
			int expectedCounter = 10;
			container.Register<IMyClass, MyClass>();

			IMyClass myClassInstance = container.Resolve<IMyClass>();
			IMyClass myOtherClassInstance = container.Resolve<IMyClass>();
			
			myClassInstance.Counter = expectedCounter;

			myOtherClassInstance.Counter.ShouldBeEquivalentTo(expectedCounter);
		}

		[Fact]
		public void Resolve_DependencyOnAnotherClass_Resolves()
		{			
			IMyClass myClassInstance = Bootstrap.Container.Resolve<IMyClass>();
			myClassInstance.Counter++;
			IMyDependentClass myDependentClassInstance = Bootstrap.Container.Resolve<IMyDependentClass>();

			myDependentClassInstance.MyDoubler.ShouldBeEquivalentTo(2);
		}

		[Fact]
		public void Resolve_ClassNotRegistered_ThrowsException()
		{
			Container container = new Container();
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				container.Resolve<IMyClass>();
			});
		}

		[Fact]
		public void Resolve_ClassFailsToInitialize_ThrowsException()
		{
			Container container = new Container();
			Assert.Throws<NullReferenceException>(() =>
			{
				container.Register<IMyBrokenClass, MyBrokenClass>();
				container.Resolve<IMyBrokenClass>();
			});
		}

		[Fact]
		public void Clear_RegisteredClassHasDispose_InvokesDispose()
		{
			Container container = new Container();
			bool didDispose = false;
			container.Register<IMyDisposableClass, MyDisposableClass>();
			IMyDisposableClass myDisposableClassInstance = container.Resolve<IMyDisposableClass>();
			myDisposableClassInstance.OnDispose = () =>
			{
				didDispose = true;
			};

			container.Clear();

			didDispose.Should().BeTrue();
		}

		[Fact]
		public void ContainsRegistration_ContainsRegistration_ReturnsTrue()
		{
			Container container = new Container();
			container.Register<IMyClass, MyClass>();

			var result = container.ContainsRegistration<IMyClass>();

			result.Should().Be(true);
		}

		[Fact]
		public void ContainsRegistration_ContainsRegistrationViaType_ReturnsTrue()
		{
			Container container = new Container();
			container.Register<IMyClass, MyClass>();

			var result = container.ContainsRegistration(typeof(IMyClass));

			result.Should().Be(true);
		}

		[Fact]
		public void ContainsRegistration_DoesNotContainsRegistration_ReturnsFalse()
		{
			Container container = new Container();

			var result = container.ContainsRegistration<IMyClass>();

			result.Should().Be(false);
		}

		[Fact]
		public void ContainsRegistration_DoesNotContainsRegistrationViaType_ReturnsFalse()
		{
			Container container = new Container();

			var result = container.ContainsRegistration(typeof(IMyClass));

			result.Should().Be(false);
		}

		#region Test Interfaces and Classes

		public class Bootstrap
		{
			internal static readonly object BootstrapLock = new object();
			/// <summary>
			/// Sets the container to use to an existing container
			/// </summary>
			/// <param name="container">The container to use</param>
			public static void SetContainer(Container container)
			{
				_container = container;
			}

			private static Container _container;
			public static Container Container
			{
				get
				{
					lock (BootstrapLock)
					{
						if (_container != null) return _container;
						_container = InitializeContainer();
						return _container;
					}
				}
			}

			private static Container InitializeContainer()
			{
				Container container = new Container();

				// Register components here				
				container.Register<IMyClass, MyClass>();
				container.Register<IMyDependentClass, MyDependentClass>();

				return container;
			}
		}

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
				_myClass = Bootstrap.Container.Resolve<IMyClass>();
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
