using SitecoreSidekick.Core;
using System;

namespace SitecoreSidekick.Services
{
	public interface IScsRegistrationService
	{
		/// <summary>
		/// Gets the ScsRegistration for the provided type.
		/// </summary>
		/// <typeparam name="T">The type for which to get the registration</typeparam>
		/// <returns>The ScsRegistration for the provided type.</returns>
		T GetScsRegistration<T>() where T : class, IScsRegistration;

		/// <summary>
		/// Gets the ScsRegistration for the provided type.
		/// </summary>
		/// <param name="t">The type for which to get the registration</param>
		/// <returns>The ScsRegistration for the provided type.</returns>
		IScsRegistration GetScsRegistration(Type t);

		/// <summary>
		/// Adds the provided ScsRegistration for the given type.
		/// </summary>
		/// <param name="t">The type associated with the ScsRegistration</param>
		/// <param name="registration">The registration to add</param>
		void AddRegistration(Type t, IScsRegistration registration);

		/// <summary>
		/// Adds the provided ScsRegistration for the given type.
		/// </summary>
		/// <typeparam name="TType">The type associated with the ScsRegistration</typeparam>
		/// <param name="registration">The registration to add</param>
		void AddRegistration<TType>(IScsRegistration registration) where TType : class;
	}
}
