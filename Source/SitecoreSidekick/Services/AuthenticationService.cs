using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.Services
{
	public class AuthenticationService : IAuthenticationService
	{
		public string GetCurrentTicketId()
		{
			return Sitecore.Web.Authentication.TicketManager.GetCurrentTicketId();
		}

		public bool Relogin(string ticket)
		{
			return Sitecore.Web.Authentication.TicketManager.Relogin(ticket);
		}

		public bool IsAuthenticated => Sitecore.Context.User.IsAuthenticated;
	}
}
