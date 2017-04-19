using System.Collections.Generic;
using ScsAuditLog.Model.Interface;

namespace ScsAuditLog.Model
{
	public class AuditGraph
	{
		public string XMax { get; set; }
		public string XMin { get; set; }
		public string YMax { get; set; }
		public string YMin { get; set; }
		public Dictionary<string, AuditGraphEntry> GraphEntries { get; set; } 
		public ISet<IAuditEntry> LogEntries { get; set; } 
	}
}
