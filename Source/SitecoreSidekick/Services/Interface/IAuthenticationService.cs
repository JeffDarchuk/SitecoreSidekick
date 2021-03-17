namespace Sidekick.Core.Services.Interface
{
	public interface IAuthenticationService
	{
		string GetCurrentTicketId();
		bool Relogin(string ticket);
		bool IsAuthenticated { get; }
	}
}
