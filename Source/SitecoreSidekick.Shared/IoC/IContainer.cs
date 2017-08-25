using System;
using System.Collections.Generic;
using System.Text;

namespace SitecoreSidekick.Shared.IoC
{
	public interface IContainer
	{
		/// <summary>
		/// Indicates if the container has a registration for the provided interface type
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>True if the registration exists, otherwise false</returns>
		bool ContainsRegistration<T>();

		/// <summary>
		/// Indicates if the container has a registration for the provided interface type
		/// </summary>
		/// <param name="t">The type to check</param>
		/// <returns>True if the registration exists, otherwise false</returns>
		bool ContainsRegistration(Type t);

		/// <summary>
		/// Retrieves the implementation that matches the provided interface.
		/// </summary>
		/// <typeparam name="T">The interface to resolve</typeparam>
		/// <param name="args">[Optional] arguments to send to the instance factory (if the resolved object is not a singleton)</param>
		/// <returns>An object representing an implementation of the provided interface from the container.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when a registration for the provided interface is not found in the container</exception>
		/// <exception cref="NullReferenceException">Thrown when a registered object from the container cannot be initialized</exception>
		T Resolve<T>(params object[] args) where T : class;
	}
}
