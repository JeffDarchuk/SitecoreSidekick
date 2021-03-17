using System.Collections.Generic;
using Lucene.Net.Support;

namespace Sidekick.AuditLog.Model
{
	public class AuditGraphEntry
	{
		public List<AuditGraphCoordinates> Coordinates { get; set; } = new EquatableList<AuditGraphCoordinates>();
		public string Color;
	}
}
