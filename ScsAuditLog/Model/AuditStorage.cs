using System.Collections.Generic;
using Sidekick.AuditLog.Model.Interface;

namespace Sidekick.AuditLog.Model
{
	public class AuditStorage
	{
		public Dictionary<string, IAuditEntry> Documents = new Dictionary<string, IAuditEntry>();
	}
}
