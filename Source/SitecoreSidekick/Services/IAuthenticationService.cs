namespace SitecoreSidekick.Services
{
	public interface IAuthenticationService
	{
		string GetCurrentTicketId();
		bool Relogin(string ticket);
		bool IsAuthenticated { get; }
	}
}
