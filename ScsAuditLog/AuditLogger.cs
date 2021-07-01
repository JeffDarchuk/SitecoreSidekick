namespace Sidekick.AuditLog
{
	public class AuditLogger
	{
		internal static Core.IAuditLog Log;
		public static Core.IAuditLog Current => Log;
	}
}
