using System.Collections.Generic;
using Lucene.Net.Support;

namespace ScsAuditLog.Model
{
	public class AuditGraphEntry
	{
		public List<AuditGraphCoordinates> Coordinates { get; set; } = new EquatableList<AuditGraphCoordinates>();
		public string Color;
	}
}
