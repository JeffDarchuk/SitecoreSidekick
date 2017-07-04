using System;
using System.Collections.Generic;
using SitecoreSidekick.Core;

namespace SitecoreSidekick.Services.Interface
{
	public interface IScsRegistrationService
	{
		void RegisterSidekick(IScsRegistration sidekick);
		void RegisterSidekick(Type t, IScsRegistration sidekick);
		T GetScsRegistration<T>() where T : class, IScsRegistration;
		IScsRegistration GetScsRegistration(Type t);
		IEnumerable<IScsRegistration> GetAllSidekicks();
		string Js { get; }
		string Css { get; }
	}
}
