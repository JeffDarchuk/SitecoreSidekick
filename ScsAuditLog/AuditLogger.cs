using Sidekick.AuditLog.Core;

namespace Sidekick.AuditLog
{
	public class AuditLogger
	{
		internal static Core.AuditLog Log;
		public static Core.AuditLog Current => Log;
	}
}
