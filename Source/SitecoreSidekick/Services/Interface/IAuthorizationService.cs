using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitecoreSidekick.Services.Interface
{
	public interface IAuthorizationService
	{
		bool IsCurrentUserAdmin { get; }
		bool IsCurrentUserInRole(IEnumerable<string> roles);
	}
}
