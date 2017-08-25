using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace SitecoreSidekick.Shared.IoC
{
	public class Container : IContainer
	{
		private readonly ConcurrentDictionary<Type, RegistrationObject> _registrations = new ConcurrentDictionary<Type, RegistrationObject>();

		/// <summary>
		/// Registers the provided implementation for the provided interface in the container.
		/// </summary>
		/// <typeparam name="TInterface">The interface to register.</typeparam>
		/// <typeparam name="TImplementation">The class that implements the provided interface</typeparam>
		public void Register<TInterface, TImplementation>()
			where TInterface : class
			where TImplementation : TInterface, new()
		{
			RegistrationObject registrationObject = new RegistrationObject(typeof(TImplementation).GetConstructor(new Type[] { }));
			_registrations.AddOrUpdate(typeof(TInterface), registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Registers the provided implementaiton for the provided interface in the container.
		/// </summary>
		/// <typeparam name="TInterface">The interface to register.</typeparam>
		/// <param name="implementation">The implemented object to use in the container</param>
		public void Register<TInterface>(object implementation)
		{
			RegistrationObject registrationObject = new RegistrationObject(implementation);
			_registrations.AddOrUpdate(typeof(TInterface), registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Registers the provided implementation for the provided interface in the container
		/// </summary>
		/// <param name="interfaceType">The type of the interface to register</param>
		/// <param name="implementationType">The type of the class that implements the provided interface</param>
		public void Register(Type interfaceType, Type implementationType)
		{
			RegistrationObject registrationObject = new RegistrationObject(implementationType.GetConstructor(new Type[] { }));
			_registrations.AddOrUpdate(interfaceType, registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Registers the provided implemented object for the provided interface in the container
		/// </summary>
		/// <param name="interfaceType">The type of the interface to register</param>
		/// <param name="implementation">The implementated object to use in the container</param>
		public void Register(Type interfaceType, object implementation)
		{
			RegistrationObject registrationObject = new RegistrationObject(implementation);
			_registrations.AddOrUpdate(interfaceType, registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Registers the provided instance factory for the provided interface in the container
		/// </summary>
		/// <param name="interfaceType">The type of interface to register</param>
		/// <param name="factory">The factory to generate new instances of the provided instance type</param>
		public void RegisterFactory(Type interfaceType, Func<object[], object> factory)
		{
			RegistrationObject registrationObject = new RegistrationObject(factory);
			_registrations.AddOrUpdate(interfaceType, registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Registers the provided instance factory for the provided interface in the container
		/// </summary>
		/// <typeparam name="TInterface">The type of interface to register</typeparam>
		/// <param name="factory">The factory to generate new instances of the provided instance type</param>
		public void RegisterFactory<TInterface>(Func<object[], object> factory)
		{
			RegistrationObject registrationObject = new RegistrationObject(factory);
			_registrations.AddOrUpdate(typeof(TInterface), registrationObject, (k, v) => registrationObject);
		}

		/// <summary>
		/// Indicates if the container has a registration for the provided interface type
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>True if the registration exists, otherwise false</returns>
		public bool ContainsRegistration<T>()
		{
			return _registrations.ContainsKey(typeof(T));
		}

		/// <summary>
		/// Indicates if the container has a registration for the provided interface type
		/// </summary>
		/// <param name="t">The type to check</param>
		/// <returns>True if the registration exists, otherwise false</returns>
		public bool ContainsRegistration(Type t)
		{
			return _registrations.ContainsKey(t);
		}

		/// <summary>
		/// Retrieves the implementation that matches the provided interface.
		/// </summary>
		/// <typeparam name="T">The interface to resolve</typeparam>
		/// <param name="args">[Optional] arguments to send to the instance factory (if the resolved object is not a singleton)</param>
		/// <returns>An object representing an implementation of the provided interface from the container.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when a registration for the provided interface is not found in the container</exception>
		/// <exception cref="NullReferenceException">Thrown when a registered object from the container cannot be initialized</exception>
		public T Resolve<T>(params object[] args) where T : class
		{
			RegistrationObject registration;
			object registrationObject;
			if (!_registrations.TryGetValue(typeof(T), out registration))
			{
				throw new ArgumentOutOfRangeException($"No registration for {typeof(T)} could be found. " +
													"Did you remember to register it with the container?");
			}
			try
			{
				registrationObject = registration.IsSingleton
					? registration.RegisteredObject
					: registration.RegisteredObjectInstance(args);

				if (registrationObject == null)
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

			return (T)registrationObject;
		}

		/// <summary>
		/// Clears the container and disposes its registrations
		/// </summary>
		public void Clear()
		{
			foreach (var registrationKey in _registrations.Keys)
			{
				RegistrationObject registrationObject;
				if (_registrations.TryRemove(registrationKey, out registrationObject))
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

			/// <summary>
			/// Creates an instance of the registered type with any provided arguments
			/// </summary>
			/// <param name="args">[Optional] arguments to provide to the instance factory</param>
			/// <returns>An instance of the registered type</returns>
			public object RegisteredObjectInstance(params object[] args)
			{
				return _factory?.Invoke(args);
			}

			private readonly ConstructorInfo _constructor;

			private readonly Func<object[], object> _factory;

			/// <summary>
			/// Indicates whether or not the registered object should be a singleton
			/// </summary>
			public bool IsSingleton => _factory == null;

			/// <summary>
			/// Creates a Registration Object to be used in the simple IoC container.
			/// </summary>
			/// <param name="constructor">The constructor (with no parameters) for the object</param>
			public RegistrationObject(ConstructorInfo constructor)
			{
				_constructor = constructor;
			}

			/// <summary>
			/// Creates an initialized Registration Object to be used int he simple IoC container.
			/// </summary>
			/// <param name="registeredObject">The object to use for registration</param>
			public RegistrationObject(object registeredObject)
			{
				_registeredObject = registeredObject;
			}

			/// <summary>
			/// Creates a Registration Object to be used in the simple IoC container
			/// </summary>
			/// <param name="factory">The function to use to generate new instances of the registered type</param>
			public RegistrationObject(Func<object[], object> factory)
			{
				_factory = factory;
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
					if (_registeredObject.GetType().GetInterfaces().Any(x => x == typeof(IDisposable)))
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
