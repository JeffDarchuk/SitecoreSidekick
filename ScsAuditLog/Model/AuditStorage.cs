using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Core;
using ScsAuditLog.Model.Interface;

namespace ScsAuditLog.Model
{
	public class AuditStorage
	{
		public Dictionary<string, IAuditEntry> Documents = new Dictionary<string, IAuditEntry>();
	}
}
