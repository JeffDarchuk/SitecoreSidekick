using System.Collections.Generic;
using ScsAuditLog.Model.Interface;

namespace ScsAuditLog.Model
{
	public class AuditStorage
	{
		public Dictionary<string, IAuditEntry> Documents = new Dictionary<string, IAuditEntry>();
	}
}
