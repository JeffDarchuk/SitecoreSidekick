using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Pipelines;
using Sitecore.Data;

namespace ScsAuditLog.Model.Interface
{
	public interface IAuditEntry
	{
		string Uid { get; set; }
		string User { get; set; }
		List<string> Role { get; set; }
		ID Id { get; set; }
		string Database { get; set; }
		string Path { get; set; }
		DateTime TimeStamp { get; set; }
		string EventId { get; set; }
		string Note { get; set; }
		string Label { get; set; }
		string Color { get; set; }
	}
}
