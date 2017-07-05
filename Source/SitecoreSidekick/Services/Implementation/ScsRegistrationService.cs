using SitecoreSidekick.Core;
using System;
using System.Collections.Concurrent;

namespace SitecoreSidekick.Services.Implementation
{
	public class ScsRegistrationService : IScsRegistrationService
	{
		private readonly ConcurrentDictionary<Type, IScsRegistration> _registrations = new ConcurrentDictionary<Type, IScsRegistration>();

		/// <summary>
		/// Gets the ScsRegistration for the provided type.
		/// </summary>
		/// <typeparam name="T">The type for which to get the registration</typeparam>
		/// <returns>The ScsRegistration for the provided type.</returns>
		public T GetScsRegistration<T>() where T : class, IScsRegistration
		{
			IScsRegistration registration;
			if (_registrations.TryGetValue(typeof(T), out registration))
			{
				return registration as T;
			}

			return null;
		}

		/// <summary>
		/// Gets the ScsRegistration for the provided type.
		/// </summary>
		/// <param name="t">The type for which to get the registration</param>
		/// <returns>The ScsRegistration for the provided type.</returns>
		public IScsRegistration GetScsRegistration(Type t)
		{
			IScsRegistration registration;
			if (_registrations.TryGetValue(t, out registration))
			{
				return registration;
			}

			return null;
		}

		/// <summary>
		/// Adds the provided ScsRegistration for the given type.
		/// </summary>
		/// <param name="t">The type associated with the ScsRegistration</param>
		/// <param name="registration">The registration to add</param>
		public void AddRegistration(Type t, IScsRegistration registration)
		{
			_registrations.AddOrUpdate(t, registration, (k, v) => registration);
		}

		/// <summary>
		/// Adds the provided ScsRegistration for the given type.
		/// </summary>
		/// <typeparam name="TType">The type associated with the ScsRegistration</typeparam>
		/// <param name="registration">The registration to add</param>
		public void AddRegistration<TType>(IScsRegistration registration) where TType : class
		{
			_registrations.AddOrUpdate(typeof(TType), registration, (k, v) => registration);
		}
	}
}
