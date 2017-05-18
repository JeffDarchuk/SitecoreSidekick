using ScsAuditLog.Core;

namespace ScsAuditLog
{
	public class AuditLogger
	{
		internal static AuditLog Log;
		public static AuditLog Current => Log;
	}
}
