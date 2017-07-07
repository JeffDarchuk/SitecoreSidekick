using System.Collections.Generic;
using SitecoreSidekick.Pipelines.HttpRequestBegin;
using SitecoreSidekick.Services.Interface;

namespace SitecoreSidekick.Services
{
	public class AuthorizationService : IAuthorizationService
	{
		public bool IsCurrentUserAdmin => IsAdmin.CurrentUserAdmin();

		public bool IsCurrentUserInRole(IEnumerable<string> roles) =>
			IsAdmin.CurrentUserInRoleList(new List<string>(roles));		
	}
}
