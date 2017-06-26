using System;
using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions.Common;

namespace SitecoreSidekick.Shared.IoC
{
	public static class Container
	{
		private static readonly ConcurrentDictionary<Type, RegistrationObject> Registrations = new ConcurrentDictionary<Type, RegistrationObject>();

		/// <summary>
		/// Registers the provided implementation for the provided interface in the container.
		/// </summary>
		/// <typeparam name="TInterface">The interface to register.</typeparam>
		/// <typeparam name="TImplementation">The class that implements the provided interface</typeparam>
		public static void Register<TInterface, TImplementation>()
			where TInterface : class
			where TImplementation : TInterface, new()
		{
			RegistrationObject registrationObject = new RegistrationObject(typeof(TImplementation).GetConstructor(new Type[] { }));
			Registrations.AddOrUpdate(typeof(TInterface), registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Retrieves the implementation that matches the provided interface.
		/// </summary>
		/// <typeparam name="T">The interface to resolve</typeparam>
		/// <returns>An object representing an implementation of the provided interface from the container.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when a registration for the provided interface is not found in the container</exception>
		/// <exception cref="NullReferenceException">Thrown when a registered object from the container cannot be initialized</exception>
		public static T Resolve<T>() where T : class
		{
			RegistrationObject registrationObject;
			if (!Registrations.TryGetValue(typeof(T), out registrationObject))
			{
				throw new ArgumentOutOfRangeException($"No registration for {typeof(T)} could be found. " +
													"Did you remember to register it with the container?");
			}
			try
			{
				if (registrationObject.RegisteredObject == null)
				{
					throw new NullReferenceException($"The registered object for {typeof(T)} could not be initialized. " +
													"This can happen if there was an error in the constructor of the implementation class.");
				}
			}
			catch (TargetInvocationException ex)
			{
				throw new NullReferenceException($"The registered object for {typeof(T)} could not be initialized. " +
												"The constructor threw an exception when the object was being initialized. " +
												"Check the inner exception for more information", ex);
			}
			catch (Exception ex)
			{
				throw new Exception($"The registered object for {typeof(T)} could not be initialized. " +
									"An unhandled exception occurred during object resolution. " +
									"Check the inner exception for more information", ex);
			}

			return (T) registrationObject.RegisteredObject;
		}

		/// <summary>
		/// Clears the container and disposes its registrations
		/// </summary>
		public static void Clear()
		{
			foreach (var registrationKey in Registrations.Keys)
			{
				RegistrationObject registrationObject;
				if (Registrations.TryRemove(registrationKey, out registrationObject))
				{
					registrationObject.Dispose();
				}
			}
		}
		
		#region RegistrationObject
		private class RegistrationObject : IDisposable
		{
			private object _registeredObject;

			/// <summary>
			///  The object will only be initialized the first time it accessed.
			/// </summary>
			public object RegisteredObject
			{
				get
				{
					if (_registeredObject != null) return _registeredObject;
					_registeredObject = _constructor.Invoke(null);
					return _registeredObject;
				}
			}

			private readonly ConstructorInfo _constructor;

			/// <summary>
			/// Creates a Registration Object to be used in the simple IoC container.
			/// </summary>
			/// <param name="constructor">The constructor (with no parameters) for the object</param>
			public RegistrationObject(ConstructorInfo constructor)
			{
				_constructor = constructor;
			}

			/// <summary>
			/// Invokes the dispose method of the registered object (if it implements IDisposable) and sets the registered
			/// object to null.
			/// </summary>
			public void Dispose()
			{
				try
				{
					if (_registeredObject == null) return;

					// If the registered object supports IDisposable, invoke it.
					if (_registeredObject.GetType().Implements(typeof(IDisposable)))
					{
						_registeredObject.GetType().GetMethod(nameof(IDisposable.Dispose)).Invoke(_registeredObject, null);
					}

					_registeredObject = null;
				}
				catch
				{
					// ignored
				}
			}
		}
		#endregion
	}
}
