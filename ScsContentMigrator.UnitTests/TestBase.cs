using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FluentAssertions;
using NSubstitute;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.ContentMigrator.UnitTests
{
	public static class TestLocker
	{
		private static readonly SemaphoreSlim TestLock = new SemaphoreSlim(1);
		public static void Wait() => TestLock.Wait();
		public static void Release() => TestLock.Release();
	}

	public class TestBase : IDisposable
	{
		/// <summary>
		/// The IoC container
		/// </summary>
		protected Container Container { get; }

		protected TestBase()
		{
			TestLocker.Wait();
			Container = new Container();
		}

		/// <summary>
		/// Creates an instance of the provided type using default substitutions for anything that might need to be pulled from the IoC container.
		/// </summary>
		/// <typeparam name="T">The type of instance to create</typeparam>
		/// <returns>A new instance of the provided type</returns>
		protected T CreateInstance<T>(params object[] args)
		{
			RegisterDependencies(typeof(T));
			Bootstrap.SetContainer(Container);
			if (args.Any())
				return (T)Activator.CreateInstance(typeof(T), args);

			return Activator.CreateInstance<T>();
		}

		/// <summary>
		/// Creates an instance of the provided type with all constructor parameters defaulted.
		/// </summary>
		/// <typeparam name="T">The type of instance to create</typeparam>
		/// <returns>An instance of the provided type with all constructor parameters defaulted.</returns>
		protected T CreateDefaultedInstance<T>()
		{			
			var ctor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public).OrderBy(c => c.GetParameters().Length).FirstOrDefault();
			if (ctor == null)
				throw new NullReferenceException("Could not find a constructor");

			object[] parameters = ctor.GetParameters()
				.Select(p => 
					typeof(TestBase).GetMethod("Default", BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(p.ParameterType).Invoke(null, new object[] { })).ToArray();

			return CreateInstance<T>(parameters);
		}

		/// <summary>
		/// Recursively registers all dependencies (private readonly Interfaces) required to create the provided type.
		/// </summary>
		/// <param name="t">The type to register dependencies for</param>
		private void RegisterDependencies(Type t)
		{
			if (t.BaseType != null) RegisterDependencies(t.BaseType);
			IEnumerable<FieldInfo> fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.FieldType.IsInterface);
			foreach (FieldInfo field in fields)
			{
				if (Container.ContainsRegistration(field.FieldType)) continue;
				RegisterDependencies(field.FieldType);
				object fieldImplementation = Substitute.For(new[] { field.FieldType }, new object[] { });
				Container.Register(field.FieldType, fieldImplementation);
			}
		}

		/// <summary>
		/// Creates a substitute for the provided interface and registers it with the container
		/// </summary>
		/// <typeparam name="T">The type of interface to register with the container</typeparam>
		/// <returns>The substitute that was registered to the container</returns>
		protected T CreateSubstitute<T>() where T : class
		{
			T substitute = Substitute.For<T>();
			Container.Register<T>(substitute);
			return substitute;
		}

		/// <summary>
		/// Fetches the provided interface as a substitute from the container.  If the interface does not exist in the container, it is created.
		/// </summary>
		/// <typeparam name="T">The type of interface to retrieve from the container</typeparam>
		/// <returns>The Substitute representing the provided interface from the container</returns>
		protected T GetSubstitute<T>() where T : class
		{
			return !Container.ContainsRegistration<T>() ? CreateSubstitute<T>() : Container.Resolve<T>();
		}

		/// <summary>
		/// Creates a substitute for the provided type and registers all of its dependencies in the container
		/// </summary>
		/// <typeparam name="T">The type of object to create a substitute for</typeparam>
		/// <param name="args">[Optional] arguments to provide to the substitute for creation</param>
		/// <returns>A substitute for the provided type</returns>
		protected T CreateSubstituteFor<T>(params object[] args) where T : class
		{
			RegisterDependencies(typeof(T));
			return Substitute.For<T>(args);
		}

		/// <summary>
		/// Gets a substitute for the ControllerContext
		/// </summary>
		/// <returns>A substitute for the ControllerContext</returns>
		protected ControllerContext ContextSubstitute()
		{
			HttpRequestBase request = Substitute.For<HttpRequestBase>();
			HttpContextBase httpContext = Substitute.For<HttpContextBase>();
			httpContext.Request.Returns(request);
			ControllerContext controllerContext = Substitute.For<ControllerContext>();
			controllerContext.HttpContext.Returns(httpContext);
			return controllerContext;
		}

		/// <summary>
		/// Casts the provided object to the supplied type.
		/// </summary>
		/// <typeparam name="T">The type to cast to</typeparam>
		/// <param name="obj">The object to cast</param>
		/// <returns>An object of the supplied type</returns>
		private static T Default<T>()
		{
			return default(T);
		}

		public void Dispose()
		{
			TestLocker.Release();
		}
	}
}
