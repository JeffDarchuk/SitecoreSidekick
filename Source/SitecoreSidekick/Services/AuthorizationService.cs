using System.Collections.Generic;
using Sidekick.Core.Pipelines.HttpRequestBegin;
using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.Services
{
	public class AuthorizationService : IAuthorizationService
	{
		public bool IsCurrentUserAdmin => IsAdmin.CurrentUserAdmin();

		public bool IsCurrentUserInRole(IEnumerable<string> roles) =>
			IsAdmin.CurrentUserInRoleList(new List<string>(roles));		
	}
}
