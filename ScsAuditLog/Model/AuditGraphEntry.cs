using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Support;
using Microsoft.SqlServer.Server;

namespace ScsAuditLog.Model
{
	public class AuditGraphEntry
	{
		public List<AuditGraphCoordinates> Coordinates { get; set; } = new EquatableList<AuditGraphCoordinates>();
		public string Color;
	}
}
